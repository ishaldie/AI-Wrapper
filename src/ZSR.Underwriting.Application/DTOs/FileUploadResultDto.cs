namespace ZSR.Underwriting.Application.DTOs;

public class FileUploadResultDto
{
    public Guid DocumentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
}
