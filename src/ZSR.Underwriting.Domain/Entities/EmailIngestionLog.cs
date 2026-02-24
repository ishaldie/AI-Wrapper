namespace ZSR.Underwriting.Domain.Entities;

public enum EmailIngestionStatus
{
    Accepted,
    Rejected
}

public class EmailIngestionLog
{
    public Guid Id { get; private set; }
    public Guid? DealId { get; private set; }
    public string SenderEmail { get; private set; }
    public EmailIngestionStatus Status { get; private set; }
    public string Reason { get; private set; }
    public int AttachmentCount { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Deal? Deal { get; set; }

    // EF Core parameterless constructor
    private EmailIngestionLog()
    {
        SenderEmail = string.Empty;
        Reason = string.Empty;
    }

    public EmailIngestionLog(Guid? dealId, string senderEmail, EmailIngestionStatus status, string reason, int attachmentCount)
    {
        if (string.IsNullOrWhiteSpace(senderEmail))
            throw new ArgumentException("Sender email cannot be empty.", nameof(senderEmail));

        Id = Guid.NewGuid();
        DealId = dealId;
        SenderEmail = senderEmail.Trim().ToLowerInvariant();
        Status = status;
        Reason = reason;
        AttachmentCount = attachmentCount;
        CreatedAt = DateTime.UtcNow;
    }
}
