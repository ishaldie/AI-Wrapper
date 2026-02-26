namespace ZSR.Underwriting.Application.DTOs;

public class BulkImportResultDto
{
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public Guid? PortfolioId { get; set; }
    public string? PortfolioName { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<Guid> CreatedDealIds { get; set; } = new();
}
