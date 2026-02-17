namespace ZSR.Underwriting.Application.Interfaces;

public interface IEmailCodeService
{
    Task<string> GenerateCodeAsync(string email);
    bool ValidateCode(string email, string code);
}
