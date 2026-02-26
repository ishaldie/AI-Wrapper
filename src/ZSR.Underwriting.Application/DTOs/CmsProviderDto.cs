namespace ZSR.Underwriting.Application.DTOs;

public class CmsProviderDto
{
    public string CcnNumber { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public int OverallRating { get; set; }
    public int HealthInspectionRating { get; set; }
    public int QualityMeasureRating { get; set; }
    public int StaffingRating { get; set; }
    public int CertifiedBeds { get; set; }
    public decimal AverageResidentsPerDay { get; set; }
    public string OwnershipType { get; set; } = string.Empty;
    public string? ChainName { get; set; }

    // Staffing
    public decimal TotalNurseHoursPerResidentDay { get; set; }
    public decimal RnHoursPerResidentDay { get; set; }
    public decimal NursingTurnoverPct { get; set; }

    // Compliance
    public int TotalDeficiencies { get; set; }
    public int NumberOfFines { get; set; }
    public decimal TotalFinesAmount { get; set; }
    public int TotalPenalties { get; set; }
    public bool AbuseFlag { get; set; }
    public string? SpecialFocusStatus { get; set; }

    // Inspection
    public DateTime? LastInspectionDate { get; set; }
    public int HealthDeficiencyScore { get; set; }

    // Deficiency + penalty details (from separate datasets)
    public List<CmsDeficiencyDto> Deficiencies { get; set; } = [];
    public List<CmsPenaltyDto> Penalties { get; set; } = [];

    public DateTime? DataAsOfDate { get; set; }
}

public class CmsDeficiencyDto
{
    public string DeficiencyTag { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime? SurveyDate { get; set; }
    public DateTime? CorrectionDate { get; set; }
}

public class CmsPenaltyDto
{
    public string PenaltyType { get; set; } = string.Empty;
    public decimal FineAmount { get; set; }
    public DateTime? PenaltyDate { get; set; }
    public string? Description { get; set; }
}
