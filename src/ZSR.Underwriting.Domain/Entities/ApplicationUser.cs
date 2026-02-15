using Microsoft.AspNetCore.Identity;

namespace ZSR.Underwriting.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
}
