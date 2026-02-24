using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Infrastructure.Data;

namespace ZSR.Underwriting.Infrastructure.Services;

public partial class EmailIngestionService : IEmailIngestionService
{
    private readonly AppDbContext _db;
    private readonly IAuthorizedSenderService _authorizedSenderService;
    private readonly IDocumentUploadService _uploadService;
    private readonly IDocumentMatchingService _matchingService;

    public EmailIngestionService(
        AppDbContext db,
        IAuthorizedSenderService authorizedSenderService,
        IDocumentUploadService uploadService,
        IDocumentMatchingService matchingService)
    {
        _db = db;
        _authorizedSenderService = authorizedSenderService;
        _uploadService = uploadService;
        _matchingService = matchingService;
    }

    public async Task<EmailIngestionResult> ProcessInboundEmailAsync(InboundEmailDto email)
    {
        var senderEmail = email.From.Trim().ToLowerInvariant();

        // 1. Parse shortcode from To address (deal-{shortcode}@ingest.zsrunderwriting.com)
        var match = ShortCodeRegex().Match(email.To);
        if (!match.Success)
            return await RejectAsync(null, senderEmail, "Deal not found", email.Attachments.Count);

        var shortCode = match.Groups[1].Value;

        // 2. Look up deal by ShortCode
        var deal = await _db.Deals.FirstOrDefaultAsync(d => d.ShortCode == shortCode);
        if (deal is null)
            return await RejectAsync(null, senderEmail, "Deal not found", email.Attachments.Count);

        // 3. Check if sender is the deal owner
        var owner = await _db.Users.FirstOrDefaultAsync(u => u.Id == deal.UserId);
        var isOwner = owner?.Email is not null
            && string.Equals(owner.Email, senderEmail, StringComparison.OrdinalIgnoreCase);

        // 4. If not owner, check authorized senders
        if (!isOwner)
        {
            var isAuthorized = await _authorizedSenderService.IsAuthorizedAsync(deal.UserId, senderEmail);
            if (!isAuthorized)
                return await RejectAsync(deal.Id, senderEmail, "Sender is not authorized for this deal", email.Attachments.Count);
        }

        // 5. Process attachments
        int processed = 0;
        foreach (var attachment in email.Attachments)
        {
            var docType = InferDocumentType(attachment.FileName);
            await _uploadService.UploadDocumentAsync(
                deal.Id, attachment.Content, attachment.FileName, docType, deal.UserId);
            processed++;
        }

        // 6. Log acceptance
        var log = new EmailIngestionLog(deal.Id, senderEmail, EmailIngestionStatus.Accepted, "OK", processed);
        _db.EmailIngestionLogs.Add(log);
        await _db.SaveChangesAsync();

        return new EmailIngestionResult
        {
            Accepted = true,
            DealId = deal.Id,
            AttachmentsProcessed = processed
        };
    }

    private async Task<EmailIngestionResult> RejectAsync(Guid? dealId, string senderEmail, string reason, int attachmentCount)
    {
        var log = new EmailIngestionLog(dealId, senderEmail, EmailIngestionStatus.Rejected, reason, attachmentCount);
        _db.EmailIngestionLogs.Add(log);
        await _db.SaveChangesAsync();

        return new EmailIngestionResult
        {
            Accepted = false,
            Reason = reason,
            DealId = dealId
        };
    }

    private static DocumentType InferDocumentType(string fileName)
    {
        var lower = fileName.ToLowerInvariant();
        if (lower.Contains("rent") && lower.Contains("roll")) return DocumentType.RentRoll;
        if (lower.Contains("t12") || lower.Contains("trailing")) return DocumentType.T12PAndL;
        if (lower.Contains("appraisal")) return DocumentType.Appraisal;
        if (lower.Contains("offering") || lower.Contains("memorandum")) return DocumentType.OfferingMemorandum;
        if (lower.Contains("phase") || lower.Contains("pca")) return DocumentType.PhaseIPCA;
        if (lower.Contains("loan") || lower.Contains("term")) return DocumentType.LoanTermSheet;
        return DocumentType.RentRoll; // Default fallback
    }

    [GeneratedRegex(@"deal-([a-z0-9]+)@", RegexOptions.IgnoreCase)]
    private static partial Regex ShortCodeRegex();
}
