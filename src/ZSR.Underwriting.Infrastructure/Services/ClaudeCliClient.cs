using System.Diagnostics;
using System.Text;
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

    public ClaudeCliClient(
        IOptions<ClaudeOptions> options,
        ILogger<ClaudeCliClient> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ClaudeResponse> SendMessageAsync(ClaudeRequest request, CancellationToken ct = default)
    {
        var args = new StringBuilder();
        args.Append("--print ");
        args.Append($"--model \"{_options.Model}\" ");

        if (request.MaxTokens.HasValue)
            args.Append($"--max-tokens {request.MaxTokens.Value} ");
        else
            args.Append($"--max-tokens {_options.MaxTokens} ");

        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
            args.Append($"--system-prompt \"{EscapeForShell(request.SystemPrompt)}\" ");

        _logger.LogInformation("Claude CLI call: claude {Args}", args.ToString().TrimEnd());

        var psi = new ProcessStartInfo
        {
            FileName = "claude",
            Arguments = args.ToString(),
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        // Send user message via stdin to avoid shell escaping issues with large prompts
        await process.StandardInput.WriteAsync(request.UserMessage);
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

        _logger.LogInformation(
            "Claude CLI success: {Length} chars returned", output.Length);

        return new ClaudeResponse
        {
            Content = output.Trim(),
            Model = _options.Model,
            StopReason = "end_turn",
            InputTokens = 0,  // CLI doesn't report token counts
            OutputTokens = 0
        };
    }

    private static string EscapeForShell(string input)
    {
        return input
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "");
    }
}
