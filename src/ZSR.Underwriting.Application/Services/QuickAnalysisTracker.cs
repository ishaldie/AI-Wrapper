using System.Collections.Concurrent;
using ZSR.Underwriting.Application.DTOs;

namespace ZSR.Underwriting.Application.Services;

public static class QuickAnalysisTracker
{
    private static readonly ConcurrentDictionary<Guid, QuickAnalysisProgress> _progress = new();

    public static void Register(Guid dealId, QuickAnalysisProgress progress)
    {
        _progress[dealId] = progress;
    }

    public static QuickAnalysisProgress? GetProgress(Guid dealId)
    {
        _progress.TryGetValue(dealId, out var progress);
        return progress;
    }

    public static bool Remove(Guid dealId)
    {
        return _progress.TryRemove(dealId, out _);
    }
}
