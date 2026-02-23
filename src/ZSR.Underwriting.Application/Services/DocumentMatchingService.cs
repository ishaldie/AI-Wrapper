using System.Text.RegularExpressions;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Application.Services;

public partial class DocumentMatchingService : IDocumentMatchingService
{
    private const double MinScoreThreshold = 0.25;
    private const int MinKeywordMatches = 2;

    private static readonly Dictionary<DocumentType, string[]> DocTypeKeywords = new()
    {
        [DocumentType.RentRoll] = ["rent", "roll"],
        [DocumentType.T12PAndL] = ["t12", "trailing", "operating", "statement", "p&l", "pnl"],
        [DocumentType.OfferingMemorandum] = ["offering", "memorandum", "om"],
        [DocumentType.Appraisal] = ["appraisal"],
        [DocumentType.PhaseIPCA] = ["phase", "pca", "environmental"],
        [DocumentType.LoanTermSheet] = ["loan", "term", "sheet"],
    };

    public DocumentMatchResult? FindBestMatch(
        string fileName,
        DocumentType documentType,
        IReadOnlyList<ChecklistMatchCandidate> candidates)
    {
        if (string.IsNullOrWhiteSpace(fileName) || candidates.Count == 0)
            return null;

        var docKeywords = ExtractKeywords(fileName, documentType);
        if (docKeywords.Count == 0)
            return null;

        DocumentMatchResult? best = null;

        foreach (var candidate in candidates)
        {
            var itemWords = TokenizeItemName(candidate.ItemName);
            if (itemWords.Count == 0)
                continue;

            int matches = 0;
            foreach (var keyword in docKeywords)
            {
                if (itemWords.Any(w => WordsMatch(keyword, w)))
                    matches++;
            }

            if (matches < MinKeywordMatches)
                continue;

            double ratio = (double)matches / itemWords.Count;
            if (ratio < MinScoreThreshold)
                continue;

            // Primary: match count (more overlapping keywords = better).
            // Secondary: ratio (tiebreaker favours tighter matches).
            double score = matches + ratio;

            if (best is null || score > best.Score)
                best = new DocumentMatchResult(candidate.ChecklistItemId, candidate.ItemName, score);
        }

        return best;
    }

    private static HashSet<string> ExtractKeywords(string fileName, DocumentType documentType)
    {
        // Remove file extension
        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);

        // Split on separators and camelCase boundaries
        var tokens = SplitTokens(nameWithoutExt);

        var keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var token in tokens)
        {
            var lower = token.ToLowerInvariant();
            if (lower.Length >= 2 && !IsNoiseWord(lower))
                keywords.Add(lower);
        }

        // Add document type keywords
        if (DocTypeKeywords.TryGetValue(documentType, out var typeKeywords))
        {
            foreach (var kw in typeKeywords)
                keywords.Add(kw);
        }

        return keywords;
    }

    private static HashSet<string> TokenizeItemName(string itemName)
    {
        var words = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in WordSplitRegex().Split(itemName))
        {
            var lower = part.ToLowerInvariant().Trim();
            if (lower.Length >= 2 && !IsNoiseWord(lower))
                words.Add(lower);
        }
        return words;
    }

    private static List<string> SplitTokens(string input)
    {
        // Split on underscores, hyphens, dots, spaces, and camelCase boundaries
        var parts = SeparatorRegex().Split(input);
        var result = new List<string>();
        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part))
                continue;
            // Further split on camelCase: "RentRoll" → ["Rent", "Roll"]
            var camelParts = CamelCaseRegex().Split(part);
            foreach (var cp in camelParts)
            {
                if (!string.IsNullOrWhiteSpace(cp))
                    result.Add(cp);
            }
        }
        return result;
    }

    private static bool IsNoiseWord(string word)
    {
        return word is "the" or "and" or "for" or "of" or "or" or "in" or "to" or "a"
            or "an" or "is" or "it" or "by" or "on" or "at" or "as" or "if"
            or "copy" or "final" or "draft" or "v1" or "v2" or "v3" or "new" or "old";
    }

    private static bool WordsMatch(string a, string b)
    {
        if (a == b) return true;
        // Handle plural and short suffix variations (e.g., tax/taxes, term/terms)
        if (a.Length >= 3 && b.Length >= 3)
        {
            var shorter = a.Length <= b.Length ? a : b;
            var longer = a.Length <= b.Length ? b : a;
            if (longer.StartsWith(shorter, StringComparison.Ordinal) && longer.Length - shorter.Length <= 3)
                return true;
        }
        return false;
    }

    [GeneratedRegex(@"[\s_\-\.]+")]
    private static partial Regex SeparatorRegex();

    [GeneratedRegex(@"(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])")]
    private static partial Regex CamelCaseRegex();

    [GeneratedRegex(@"[\s\(\)&/,]+")]
    private static partial Regex WordSplitRegex();
}
