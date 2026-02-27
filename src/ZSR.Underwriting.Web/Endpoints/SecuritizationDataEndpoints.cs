using System.Security.Claims;
using ZSR.Underwriting.Application.Interfaces;

namespace ZSR.Underwriting.Web.Endpoints;

public static class SecuritizationDataEndpoints
{
    public static void MapSecuritizationDataEndpoints(this WebApplication app)
    {
        app.MapPost("/api/admin/import-securitization-data", HandleImport)
            .RequireAuthorization("AdminOnly")
            .DisableAntiforgery();
    }

    private static async Task<IResult> HandleImport(
        HttpRequest request,
        IAgencyDataImporter importer,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var source = request.Query["source"].ToString().ToLowerInvariant();
        var totalImported = 0;

        switch (source)
        {
            case "fanniemae":
            {
                var file = request.Form.Files.FirstOrDefault();
                if (file is null)
                    return Results.BadRequest("CSV file required for Fannie Mae import");
                await using var stream = file.OpenReadStream();
                totalImported = await importer.ImportFannieMaeCsvAsync(stream, ct);
                break;
            }
            case "freddiemac":
            {
                var file = request.Form.Files.FirstOrDefault();
                if (file is null)
                    return Results.BadRequest("CSV file required for Freddie Mac import");
                await using var stream = file.OpenReadStream();
                totalImported = await importer.ImportFreddieMacCsvAsync(stream, ct);
                break;
            }
            case "edgar":
            case "cmbs":
            {
                var monthsBack = 36;
                if (int.TryParse(request.Query["monthsBack"], out var mb))
                    monthsBack = mb;
                totalImported = await importer.ImportEdgarCmbsAsync(monthsBack, ct);
                break;
            }
            default:
                return Results.BadRequest("source parameter required: fanniemae, freddiemac, or edgar");
        }

        return Results.Ok(new { source, imported = totalImported });
    }
}
