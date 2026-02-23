namespace ZSR.Underwriting.Application.DTOs;

/// <summary>
/// Result of matching a document against checklist items.
/// </summary>
public record DocumentMatchResult(Guid ChecklistItemId, string ItemName, double Score);

/// <summary>
/// A checklist item candidate for document matching.
/// </summary>
public record ChecklistMatchCandidate(Guid ChecklistItemId, string ItemName);
