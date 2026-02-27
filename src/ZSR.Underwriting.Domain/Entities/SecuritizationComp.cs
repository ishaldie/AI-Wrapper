using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Domain.Entities;

public class SecuritizationComp
{
    public Guid Id { get; private set; }
    public SecuritizationDataSource Source { get; private set; }

    public PropertyType? PropertyType { get; set; }
    public string? State { get; set; }
    public string? City { get; set; }
    public string? MSA { get; set; }
    public int? Units { get; set; }
    public decimal? LoanAmount { get; set; }
    public decimal? InterestRate { get; set; }
    public decimal? DSCR { get; set; }
    public decimal? LTV { get; set; }
    public decimal? NOI { get; set; }
    public decimal? Occupancy { get; set; }
    public decimal? CapRate { get; set; }
    public DateTime? MaturityDate { get; set; }
    public DateTime? OriginationDate { get; set; }
    public string? DealName { get; set; }
    public string? SecuritizationId { get; set; }

    // EF Core parameterless constructor
    private SecuritizationComp() { }

    public SecuritizationComp(SecuritizationDataSource source)
    {
        Id = Guid.NewGuid();
        Source = source;
    }
}
