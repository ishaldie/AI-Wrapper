using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Infrastructure.Services;

public class WindowsDefenderScanService : IVirusScanService
{
    private readonly ILogger<WindowsDefenderScanService> _logger;
    private static readonly string MpCmdRunPath = FindMpCmdRun();

    public WindowsDefenderScanService(ILogger<WindowsDefenderScanService> logger)
    {
        _logger = logger;
    }

    public async Task<VirusScanResult> ScanAsync(Stream fileStream, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(MpCmdRunPath))
        {
            _logger.LogWarning("Windows Defender MpCmdRun.exe not found; skipping virus scan");
            return new VirusScanResult(VirusScanStatus.ScanFailed, "Scanner not available");
        }

        // Write stream to temp file for scanning
        var tempFile = Path.Combine(Path.GetTempPath(), $"scan_{Guid.NewGuid():N}");
        try
        {
            var originalPosition = fileStream.Position;
            fileStream.Position = 0;

            await using (var fs = File.Create(tempFile))
            {
                await fileStream.CopyToAsync(fs, ct);
            }

            fileStream.Position = originalPosition;

            // Run MpCmdRun.exe scan
            var psi = new ProcessStartInfo
            {
                FileName = MpCmdRunPath,
                Arguments = $"-Scan -ScanType 3 -File \"{tempFile}\" -DisableRemediation",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                _logger.LogWarning("Failed to start MpCmdRun.exe process");
                return new VirusScanResult(VirusScanStatus.ScanFailed, "Failed to start scanner");
            }

            var output = await process.StandardOutput.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);

            // MpCmdRun exit codes: 0 = clean, 2 = threat found
            return process.ExitCode switch
            {
                0 => new VirusScanResult(VirusScanStatus.Clean),
                2 => new VirusScanResult(VirusScanStatus.Infected, ExtractThreatName(output)),
                _ => new VirusScanResult(VirusScanStatus.ScanFailed, $"Scanner exited with code {process.ExitCode}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Virus scan failed for temp file {TempFile}", tempFile);
            return new VirusScanResult(VirusScanStatus.ScanFailed, ex.Message);
        }
        finally
        {
            try { File.Delete(tempFile); } catch { /* best effort cleanup */ }
        }
    }

    private static string? ExtractThreatName(string output)
    {
        // MpCmdRun outputs "Threat  : <name>" when found
        foreach (var line in output.Split('\n'))
        {
            if (line.TrimStart().StartsWith("Threat", StringComparison.OrdinalIgnoreCase))
            {
                var parts = line.Split(':', 2);
                if (parts.Length > 1)
                    return parts[1].Trim();
            }
        }
        return "Unknown threat";
    }

    private static string FindMpCmdRun()
    {
        // Standard Windows Defender path
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var defenderPath = Path.Combine(programFiles, "Windows Defender", "MpCmdRun.exe");
        if (File.Exists(defenderPath))
            return defenderPath;

        // Try x86 path
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var defenderPathX86 = Path.Combine(programFilesX86, "Windows Defender", "MpCmdRun.exe");
        if (File.Exists(defenderPathX86))
            return defenderPathX86;

        return string.Empty;
    }
}
