namespace ZSR.Underwriting.Application.DTOs;

public class OverrideApplicationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<FieldOverrideDto> AppliedOverrides { get; set; } = new();
}

public class FieldOverrideDto
{
    public string FieldName { get; set; } = string.Empty;
    public string OriginalValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime AppliedAt { get; set; }
}
