using ZSR.Underwriting.Domain.Models;

namespace ZSR.Underwriting.Domain.Interfaces;

/// <summary>
/// Client for sending messages to the Claude API.
/// </summary>
public interface IClaudeClient
{
    Task<ClaudeResponse> SendMessageAsync(ClaudeRequest request, CancellationToken ct = default);
}
