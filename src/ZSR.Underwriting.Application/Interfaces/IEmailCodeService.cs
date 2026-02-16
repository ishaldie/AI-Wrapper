namespace ZSR.Underwriting.Application.Interfaces;

public interface IEmailCodeService
{
    string GenerateCode(string email);
    bool ValidateCode(string email, string code);
}
