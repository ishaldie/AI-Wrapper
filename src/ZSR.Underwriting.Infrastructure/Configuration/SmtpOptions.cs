namespace ZSR.Underwriting.Infrastructure.Configuration;

public class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string FromEmail { get; set; } = "noreply@underwritinganalyst.com";
    public string FromName { get; set; } = "Underwriting Analyst";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}
