using FluentValidation.TestHelper;
using ZSR.Underwriting.Application.Validators;

namespace ZSR.Underwriting.Tests.Validators;

public class LoginInputValidatorTests
{
    private readonly LoginInputValidator _validator = new();

    [Fact]
    public void Valid_Input_Passes()
    {
        var model = new LoginInputModel
        {
            Email = "john@zsr.com",
            Password = "SecurePass1!"
        };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_Email_Fails()
    {
        var model = new LoginInputModel { Email = "", Password = "pass" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Invalid_Email_Fails()
    {
        var model = new LoginInputModel { Email = "bad", Password = "pass" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Empty_Password_Fails()
    {
        var model = new LoginInputModel { Email = "test@zsr.com", Password = "" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void RememberMe_Defaults_To_False()
    {
        var model = new LoginInputModel();
        Assert.False(model.RememberMe);
    }
}
