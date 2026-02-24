using Microsoft.EntityFrameworkCore;
using Xunit;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Infrastructure.Data;
using ZSR.Underwriting.Infrastructure.Services;

namespace ZSR.Underwriting.Tests.Services;

public class EmailIngestionServiceTests : IAsyncLifetime
{
    private readonly AppDbContext _db;
    private readonly EmailIngestionService _svc;
    private readonly AuthorizedSenderService _authSenderSvc;
    private readonly Deal _deal;
    private const string OwnerEmail = "owner@example.com";
    private const string OwnerId = "owner-user-id";

    public EmailIngestionServiceTests()
    {
        var dbName = $"EmailIngestionTests_{Guid.NewGuid()}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        _db = new AppDbContext(options);
        _authSenderSvc = new AuthorizedSenderService(_db);

        var stubUploadService = new StubUploadService();
        var matchingService = new ZSR.Underwriting.Application.Services.DocumentMatchingService();

        _svc = new EmailIngestionService(_db, _authSenderSvc, stubUploadService, matchingService);

        // Seed a deal with an owner who has an email
        _deal = new Deal("Test Property", OwnerId);
        _db.Deals.Add(_deal);

        // Seed owner user with email
        var owner = new ApplicationUser { Id = OwnerId, Email = OwnerEmail, UserName = OwnerEmail };
        _db.Users.Add(owner);
        _db.SaveChanges();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await _db.DisposeAsync();

    private InboundEmailDto MakeEmail(string from, string? toShortCode = null, List<EmailAttachmentDto>? attachments = null)
    {
        var shortCode = toShortCode ?? _deal.ShortCode;
        return new InboundEmailDto
        {
            From = from,
            To = $"deal-{shortCode}@ingest.zsrunderwriting.com",
            Subject = "Documents for review",
            Attachments = attachments ?? new List<EmailAttachmentDto>()
        };
    }

    [Fact]
    public async Task OwnerEmail_IsAccepted()
    {
        var email = MakeEmail(OwnerEmail);
        var result = await _svc.ProcessInboundEmailAsync(email);

        Assert.True(result.Accepted);
        Assert.Equal(_deal.Id, result.DealId);
    }

    [Fact]
    public async Task AuthorizedSender_IsAccepted()
    {
        await _authSenderSvc.AddAsync(OwnerId, "broker@example.com", "Broker");

        var email = MakeEmail("broker@example.com");
        var result = await _svc.ProcessInboundEmailAsync(email);

        Assert.True(result.Accepted);
        Assert.Equal(_deal.Id, result.DealId);
    }

    [Fact]
    public async Task UnknownSender_IsRejected()
    {
        var email = MakeEmail("stranger@evil.com");
        var result = await _svc.ProcessInboundEmailAsync(email);

        Assert.False(result.Accepted);
        Assert.Contains("not authorized", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UnknownSender_CreatesRejectionLog()
    {
        var email = MakeEmail("stranger@evil.com");
        await _svc.ProcessInboundEmailAsync(email);

        var log = await _db.EmailIngestionLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Equal(EmailIngestionStatus.Rejected, log.Status);
        Assert.Equal("stranger@evil.com", log.SenderEmail);
    }

    [Fact]
    public async Task AcceptedEmail_CreatesAcceptedLog()
    {
        var email = MakeEmail(OwnerEmail);
        await _svc.ProcessInboundEmailAsync(email);

        var log = await _db.EmailIngestionLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Equal(EmailIngestionStatus.Accepted, log.Status);
        Assert.Equal(_deal.Id, log.DealId);
    }

    [Fact]
    public async Task MultipleAttachments_AllProcessed()
    {
        var attachments = new List<EmailAttachmentDto>
        {
            new() { FileName = "rent_roll.pdf", ContentType = "application/pdf", Content = new MemoryStream(new byte[100]), Size = 100 },
            new() { FileName = "t12.xlsx", ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", Content = new MemoryStream(new byte[200]), Size = 200 }
        };

        var email = MakeEmail(OwnerEmail, attachments: attachments);
        var result = await _svc.ProcessInboundEmailAsync(email);

        Assert.True(result.Accepted);
        Assert.Equal(2, result.AttachmentsProcessed);
    }

    [Fact]
    public async Task NoDealFound_IsRejected()
    {
        var email = MakeEmail(OwnerEmail, toShortCode: "notacode");
        var result = await _svc.ProcessInboundEmailAsync(email);

        Assert.False(result.Accepted);
        Assert.Contains("deal not found", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SenderEmail_CaseInsensitive()
    {
        var email = MakeEmail("OWNER@EXAMPLE.COM");
        var result = await _svc.ProcessInboundEmailAsync(email);

        Assert.True(result.Accepted);
    }

    [Fact]
    public async Task AcceptedEmail_LogRecordsAttachmentCount()
    {
        var attachments = new List<EmailAttachmentDto>
        {
            new() { FileName = "doc.pdf", ContentType = "application/pdf", Content = new MemoryStream(new byte[50]), Size = 50 }
        };

        var email = MakeEmail(OwnerEmail, attachments: attachments);
        await _svc.ProcessInboundEmailAsync(email);

        var log = await _db.EmailIngestionLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Equal(1, log.AttachmentCount);
    }

    // --- Integration / endpoint-level scenarios ---

    [Fact]
    public async Task MultipleEmails_SameDeal_AllLogged()
    {
        await _svc.ProcessInboundEmailAsync(MakeEmail(OwnerEmail));
        await _svc.ProcessInboundEmailAsync(MakeEmail("stranger@evil.com"));
        await _svc.ProcessInboundEmailAsync(MakeEmail(OwnerEmail));

        var logs = await _db.EmailIngestionLogs.ToListAsync();
        Assert.Equal(3, logs.Count);
        Assert.Equal(2, logs.Count(l => l.Status == EmailIngestionStatus.Accepted));
        Assert.Single(logs, l => l.Status == EmailIngestionStatus.Rejected);
    }

    [Fact]
    public async Task RevokedSender_RejectedAfterRemoval()
    {
        var sender = await _authSenderSvc.AddAsync(OwnerId, "temp@example.com", "Temp");
        Assert.NotNull(sender);

        // First email accepted
        var result1 = await _svc.ProcessInboundEmailAsync(MakeEmail("temp@example.com"));
        Assert.True(result1.Accepted);

        // Revoke authorization
        await _authSenderSvc.RemoveAsync(OwnerId, sender.Id);

        // Second email rejected
        var result2 = await _svc.ProcessInboundEmailAsync(MakeEmail("temp@example.com"));
        Assert.False(result2.Accepted);
    }

    [Fact]
    public async Task MalformedToAddress_IsRejected()
    {
        var email = new InboundEmailDto
        {
            From = OwnerEmail,
            To = "random@otherdomain.com",
            Subject = "Test",
            Attachments = new()
        };

        var result = await _svc.ProcessInboundEmailAsync(email);
        Assert.False(result.Accepted);
    }

    [Fact]
    public async Task EmptyAttachments_AcceptedWithZeroProcessed()
    {
        var email = MakeEmail(OwnerEmail, attachments: new List<EmailAttachmentDto>());
        var result = await _svc.ProcessInboundEmailAsync(email);

        Assert.True(result.Accepted);
        Assert.Equal(0, result.AttachmentsProcessed);
    }
}

internal class StubUploadService : IDocumentUploadService
{
    public Task<FileUploadResultDto> UploadDocumentAsync(Guid dealId, Stream fileStream, string fileName, DocumentType documentType, string userId, CancellationToken ct = default)
        => Task.FromResult(new FileUploadResultDto { DocumentId = Guid.NewGuid(), FileName = fileName });

    public Task<IReadOnlyList<FileUploadResultDto>> GetDocumentsForDealAsync(Guid dealId, string userId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<FileUploadResultDto>>(new List<FileUploadResultDto>());

    public Task DeleteDocumentAsync(Guid documentId, string userId, CancellationToken ct = default)
        => Task.CompletedTask;
}
