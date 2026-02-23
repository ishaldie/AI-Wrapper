using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZSR.Underwriting.Domain.Interfaces;
using ZSR.Underwriting.Domain.Models;
using ZSR.Underwriting.Infrastructure.Configuration;

namespace ZSR.Underwriting.Infrastructure.Services;

/// <summary>
/// IClaudeClient implementation that shells out to the Claude Code CLI.
/// Uses your Claude Code subscription instead of API credits.
/// For local development/testing only — not suitable for multi-user production.
/// </summary>
public class ClaudeCliClient : IClaudeClient
{
    private readonly ClaudeOptions _options;
    private readonly ILogger<ClaudeCliClient> _logger;

    private readonly string _claudePath;

    public ClaudeCliClient(
        IOptions<ClaudeOptions> options,
        ILogger<ClaudeCliClient> logger)
    {
        _options = options.Value;
        _logger = logger;
        _claudePath = ResolveClaudePath();
        _logger.LogInformation("Claude CLI resolved path: {Path}", _claudePath);
    }

    private static string ResolveClaudePath()
    {
        // 1. Check if "claude" is directly on the PATH
        var pathDirs = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? [];
        foreach (var dir in pathDirs)
        {
            var candidate = Path.Combine(dir, "claude.exe");
            if (File.Exists(candidate)) return candidate;
            candidate = Path.Combine(dir, "claude");
            if (File.Exists(candidate)) return candidate;
        }

        // 2. Check common install locations
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string[] commonPaths =
        [
            Path.Combine(home, ".local", "bin", "claude.exe"),
            Path.Combine(home, ".local", "bin", "claude"),
            Path.Combine(home, "AppData", "Local", "Programs", "claude", "claude.exe"),
            Path.Combine(home, ".npm-global", "bin", "claude.cmd"),
            Path.Combine(home, ".npm-global", "bin", "claude"),
        ];

        foreach (var path in commonPaths)
        {
            if (File.Exists(path)) return path;
        }

        // 3. Fallback — let the OS try to resolve it
        return "claude";
    }

    public async Task<ClaudeResponse> SendMessageAsync(ClaudeRequest request, CancellationToken ct = default)
    {
        // Build stdin content (the user message / conversation)
        var stdinContent = new StringBuilder();
        if (request.ConversationHistory is { Count: > 0 })
        {
            foreach (var msg in request.ConversationHistory)
            {
                var prefix = msg.Role == "user" ? "User" : "Assistant";
                stdinContent.AppendLine($"[{prefix}]: {msg.Content}");
                stdinContent.AppendLine();
            }
        }
        else
        {
            stdinContent.AppendLine(request.UserMessage);
        }

        // Use ArgumentList for proper escaping (avoids Windows shell quoting issues)
        var psi = new ProcessStartInfo
        {
            FileName = _claudePath,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        psi.ArgumentList.Add("-p");
        psi.ArgumentList.Add("--output-format");
        psi.ArgumentList.Add("json");
        psi.ArgumentList.Add("--model");
        psi.ArgumentList.Add(_options.Model);
        psi.ArgumentList.Add("--max-turns");
        psi.ArgumentList.Add("1");
        // Disable all tools so the model generates text directly
        // (without this, it tries WebSearch which gets denied, burns the turn, and returns no text)
        psi.ArgumentList.Add("--tools");
        psi.ArgumentList.Add("");

        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
        {
            psi.ArgumentList.Add("--system-prompt");
            psi.ArgumentList.Add(request.SystemPrompt);
        }

        _logger.LogInformation("Claude CLI call: {Path} with {ArgCount} args (stdin: {StdinLen} chars)",
            _claudePath, psi.ArgumentList.Count, stdinContent.Length);

        // Clear env vars to allow running inside a Claude Code terminal session
        psi.Environment.Remove("CLAUDECODE");
        psi.Environment.Remove("CLAUDE_CODE_ENTRYPOINT");

        using var process = new Process { StartInfo = psi };
        process.Start();

        await process.StandardInput.WriteAsync(stdinContent.ToString());
        process.StandardInput.Close();

        var outputTask = process.StandardOutput.ReadToEndAsync(ct);
        var errorTask = process.StandardError.ReadToEndAsync(ct);

        // Wait for process with timeout
        var timeoutMs = _options.TimeoutSeconds * 1000;
        var completed = await Task.Run(() => process.WaitForExit(timeoutMs), ct);

        if (!completed)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException(
                $"Claude CLI timed out after {_options.TimeoutSeconds} seconds.");
        }

        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0)
        {
            _logger.LogError("Claude CLI failed (exit code {ExitCode}): {Error}",
                process.ExitCode, error);
            throw new InvalidOperationException(
                $"Claude CLI exited with code {process.ExitCode}: {error}");
        }

        return ParseJsonResponse(output);
    }

    private ClaudeResponse ParseJsonResponse(string output)
    {
        try
        {
            using var doc = JsonDocument.Parse(output);
            var root = doc.RootElement;

            // Log key field values for debugging
            var subtype = root.TryGetProperty("subtype", out var subtypeEl) ? subtypeEl.GetString() : "n/a";
            var isErrorVal = root.TryGetProperty("is_error", out var isErrEl) ? isErrEl.ToString() : "n/a";
            _logger.LogInformation("Claude CLI response: subtype={Subtype}, is_error={IsError}", subtype, isErrorVal);

            // Check for error response — CLI returns is_error=true with errors array,
            // but also check subtype for "error_*" since is_error can be false for error_max_turns
            var isError = root.TryGetProperty("is_error", out var isErrorProp) &&
                          isErrorProp.ValueKind == JsonValueKind.True;
            var hasErrorSubtype = subtype?.StartsWith("error_") == true;

            if (isError || hasErrorSubtype)
            {
                var errorMessages = new List<string>();
                if (root.TryGetProperty("errors", out var errorsProp) &&
                    errorsProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var err in errorsProp.EnumerateArray())
                    {
                        if (err.ValueKind == JsonValueKind.String)
                            errorMessages.Add(err.GetString() ?? "");
                    }
                }

                var errSubtype = root.TryGetProperty("subtype", out var subtypeProp)
                    ? subtypeProp.GetString() ?? "unknown"
                    : "unknown";

                var errText = errorMessages.Count > 0
                    ? string.Join("; ", errorMessages)
                    : $"CLI error (subtype: {errSubtype})";

                _logger.LogError("Claude CLI returned error: subtype={Subtype}, errors={Errors}",
                    errSubtype, errText);

                // If there's a result despite the error (e.g. partial), use it
                var partialText = ExtractText(root);
                if (!string.IsNullOrEmpty(partialText))
                {
                    _logger.LogWarning("Claude CLI error but found partial result ({Length} chars), using it",
                        partialText.Length);
                }
                else
                {
                    throw new InvalidOperationException($"Claude CLI error: {errText}");
                }
            }

            // Extract text from result field
            var text = ExtractText(root);
            _logger.LogInformation("Claude CLI extracted text length: {Length}", text.Length);

            // Extract metadata
            var model = root.TryGetProperty("model", out var modelProp)
                ? modelProp.GetString() ?? _options.Model
                : _options.Model;

            var stopReason = root.TryGetProperty("stop_reason", out var stopProp)
                ? stopProp.GetString() ?? "end_turn"
                : "end_turn";

            var inputTokens = 0;
            var outputTokens = 0;

            if (root.TryGetProperty("usage", out var usageProp))
            {
                if (usageProp.TryGetProperty("input_tokens", out var inTok))
                    inputTokens = inTok.GetInt32();
                if (usageProp.TryGetProperty("output_tokens", out var outTok))
                    outputTokens = outTok.GetInt32();
            }

            // Fallback: tokens at top level
            if (inputTokens == 0 && root.TryGetProperty("input_tokens", out var inTokTop))
                inputTokens = inTokTop.GetInt32();
            if (outputTokens == 0 && root.TryGetProperty("output_tokens", out var outTokTop))
                outputTokens = outTokTop.GetInt32();

            _logger.LogInformation(
                "Claude CLI done: model={Model}, input_tokens={InputTokens}, output_tokens={OutputTokens}, stop_reason={StopReason}, text_length={TextLength}",
                model, inputTokens, outputTokens, stopReason, text.Length);

            return new ClaudeResponse
            {
                Content = text.Trim(),
                Model = model,
                StopReason = stopReason,
                InputTokens = inputTokens,
                OutputTokens = outputTokens
            };
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse Claude CLI JSON output, falling back to raw text");
            return FallbackResponse(output);
        }
    }

    /// <summary>
    /// Extract the response text from the CLI JSON, handling multiple possible formats:
    /// - "result": "text..." (string)
    /// - "result": [{"type":"text","text":"..."}] (content blocks)
    /// - "content": [{"type":"text","text":"..."}] (content blocks)
    /// </summary>
    private string ExtractText(JsonElement root)
    {
        // Try "result" field first
        if (root.TryGetProperty("result", out var resultProp))
        {
            if (resultProp.ValueKind == JsonValueKind.String)
            {
                var s = resultProp.GetString();
                if (!string.IsNullOrEmpty(s))
                    return s;
            }
            else if (resultProp.ValueKind == JsonValueKind.Array)
            {
                return ExtractTextFromContentBlocks(resultProp);
            }
        }

        // Try "content" field (some CLI versions)
        if (root.TryGetProperty("content", out var contentProp))
        {
            if (contentProp.ValueKind == JsonValueKind.String)
            {
                var s = contentProp.GetString();
                if (!string.IsNullOrEmpty(s))
                    return s;
            }
            else if (contentProp.ValueKind == JsonValueKind.Array)
            {
                return ExtractTextFromContentBlocks(contentProp);
            }
        }

        // Try "message" → "content" (Messages API style)
        if (root.TryGetProperty("message", out var msgProp) &&
            msgProp.TryGetProperty("content", out var msgContentProp) &&
            msgContentProp.ValueKind == JsonValueKind.Array)
        {
            return ExtractTextFromContentBlocks(msgContentProp);
        }

        _logger.LogWarning("Claude CLI JSON: could not find response text in any known field");
        return string.Empty;
    }

    private static string ExtractTextFromContentBlocks(JsonElement array)
    {
        var parts = new List<string>();
        foreach (var block in array.EnumerateArray())
        {
            if (block.ValueKind == JsonValueKind.String)
            {
                parts.Add(block.GetString() ?? "");
            }
            else if (block.TryGetProperty("text", out var textProp))
            {
                parts.Add(textProp.GetString() ?? "");
            }
        }
        return string.Join("\n", parts);
    }

    private ClaudeResponse FallbackResponse(string output)
    {
        _logger.LogInformation("Claude CLI fallback: {Length} chars returned", output.Length);
        return new ClaudeResponse
        {
            Content = output.Trim(),
            Model = _options.Model,
            StopReason = "end_turn",
            InputTokens = 0,
            OutputTokens = 0
        };
    }

}
