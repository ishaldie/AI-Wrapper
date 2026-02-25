namespace ZSR.Underwriting.Application.DTOs;

public class InboundEmailDto
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public List<EmailAttachmentDto> Attachments { get; set; } = new();
}

public class EmailAttachmentDto
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public Stream Content { get; set; } = Stream.Null;
    public long Size { get; set; }
}
