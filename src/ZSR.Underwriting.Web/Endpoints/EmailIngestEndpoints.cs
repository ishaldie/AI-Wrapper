using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;

namespace ZSR.Underwriting.Web.Endpoints;

public static class EmailIngestEndpoints
{
    public static void MapEmailIngestEndpoints(this WebApplication app)
    {
        app.MapPost("/api/ingest/email", HandleInboundEmail)
            .DisableAntiforgery();
    }

    private static async Task<IResult> HandleInboundEmail(
        HttpRequest request,
        IEmailIngestionService ingestionService,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("EmailIngest");

        // SendGrid Inbound Parse sends multipart/form-data
        if (!request.HasFormContentType)
            return Results.BadRequest(new { error = "Expected multipart/form-data" });

        var form = await request.ReadFormAsync(ct);

        var from = form["from"].ToString();
        var to = form["to"].ToString();
        var subject = form["subject"].ToString();

        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
            return Results.BadRequest(new { error = "Missing required fields: from, to" });

        var attachments = new List<EmailAttachmentDto>();
        foreach (var file in form.Files)
        {
            attachments.Add(new EmailAttachmentDto
            {
                FileName = file.FileName,
                ContentType = file.ContentType,
                Content = file.OpenReadStream(),
                Size = file.Length
            });
        }

        var email = new InboundEmailDto
        {
            From = from,
            To = to,
            Subject = subject,
            Attachments = attachments
        };

        logger.LogInformation("Inbound email from {From} to {To} with {AttachmentCount} attachments",
            from, to, attachments.Count);

        var result = await ingestionService.ProcessInboundEmailAsync(email);

        if (!result.Accepted)
        {
            logger.LogWarning("Email rejected: {Reason}", result.Reason);
            return Results.Ok(new { accepted = false, reason = result.Reason });
        }

        logger.LogInformation("Email accepted for deal {DealId}, {Count} attachments processed",
            result.DealId, result.AttachmentsProcessed);

        return Results.Ok(new
        {
            accepted = true,
            dealId = result.DealId,
            attachmentsProcessed = result.AttachmentsProcessed
        });
    }
}
