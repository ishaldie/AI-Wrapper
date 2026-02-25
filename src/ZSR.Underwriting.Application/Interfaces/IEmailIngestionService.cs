using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Domain.Entities;

namespace ZSR.Underwriting.Application.Interfaces;

public class EmailIngestionResult
{
    public bool Accepted { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid? DealId { get; set; }
    public int AttachmentsProcessed { get; set; }
}

public interface IEmailIngestionService
{
    Task<EmailIngestionResult> ProcessInboundEmailAsync(InboundEmailDto email);
}
