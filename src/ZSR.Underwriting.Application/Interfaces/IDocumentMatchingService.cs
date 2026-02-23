using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Application.Interfaces;

public interface IDocumentMatchingService
{
    /// <summary>
    /// Finds the best checklist item match for a document based on filename keywords and document type.
    /// </summary>
    /// <param name="fileName">The uploaded file name (e.g., "rent_roll_jan2024.xlsx").</param>
    /// <param name="documentType">The document type enum value.</param>
    /// <param name="candidates">Checklist items to match against (caller should pre-filter to Outstanding items).</param>
    /// <returns>The best match, or null if no match exceeds the minimum threshold.</returns>
    DocumentMatchResult? FindBestMatch(
        string fileName,
        DocumentType documentType,
        IReadOnlyList<ChecklistMatchCandidate> candidates);
}
