using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Domain.Interfaces;
using ZSR.Underwriting.Infrastructure.Data;

namespace ZSR.Underwriting.Web.Endpoints;

public static class DocumentEndpoints
{
    public static void MapDocumentEndpoints(this WebApplication app)
    {
        app.MapGet("/api/documents/{id:guid}/download", HandleDownload)
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleDownload(
        Guid id,
        AppDbContext db,
        IFileStorageService fileStorage,
        IActivityTracker activityTracker,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();

        var document = await db.UploadedDocuments
            .Include(d => d.Deal)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        if (document is null)
            return Results.NotFound();

        // Ownership verification — only the deal owner can download
        if (document.Deal.UserId != userId)
            return Results.Forbid();

        var exists = await fileStorage.FileExistsAsync(document.StoredPath, ct);
        if (!exists)
            return Results.NotFound();

        var stream = await fileStorage.GetFileAsync(document.StoredPath, ct);
        var contentType = GetContentType(document.FileName);

        await activityTracker.TrackEventAsync(ActivityEventType.DocumentDownloaded, dealId: document.DealId, metadata: document.FileName);

        return Results.File(stream, contentType, document.FileName);
    }

    private static string GetContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".pdf" => "application/pdf",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".csv" => "text/csv",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };
    }
}
