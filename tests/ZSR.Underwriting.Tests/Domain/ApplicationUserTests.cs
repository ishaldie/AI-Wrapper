using Microsoft.AspNetCore.Identity;
using ZSR.Underwriting.Domain.Entities;

namespace ZSR.Underwriting.Tests.Domain;

public class ApplicationUserTests
{
    [Fact]
    public void ApplicationUser_Extends_IdentityUser()
    {
        var user = new ApplicationUser();
        Assert.IsAssignableFrom<IdentityUser>(user);
    }

    [Fact]
    public void ApplicationUser_Has_FullName_Property()
    {
        var user = new ApplicationUser { FullName = "John Doe" };
        Assert.Equal("John Doe", user.FullName);
    }

    [Fact]
    public void ApplicationUser_FullName_Defaults_To_Empty()
    {
        var user = new ApplicationUser();
        Assert.Equal(string.Empty, user.FullName);
    }

    [Fact]
    public void ApplicationUser_Inherits_Email_From_IdentityUser()
    {
        var user = new ApplicationUser { Email = "test@zsr.com" };
        Assert.Equal("test@zsr.com", user.Email);
    }
}
