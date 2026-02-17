using FluentValidation.TestHelper;
using ZSR.Underwriting.Application.Validators;

namespace ZSR.Underwriting.Tests.Validators;

public class RegisterInputValidatorTests
{
    private readonly RegisterInputValidator _validator = new();

    [Fact]
    public void Valid_Input_Passes()
    {
        var model = new RegisterInputModel
        {
            FullName = "John Doe",
            Email = "john@zsr.com",
            Password = "SecurePass1!",
            ConfirmPassword = "SecurePass1!",
            AcceptedTos = true
        };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_FullName_Fails()
    {
        var model = new RegisterInputModel { FullName = "" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.FullName);
    }

    [Fact]
    public void Empty_Email_Fails()
    {
        var model = new RegisterInputModel { Email = "" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Invalid_Email_Fails()
    {
        var model = new RegisterInputModel { Email = "not-an-email" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Password_Too_Short_Fails()
    {
        var model = new RegisterInputModel { Password = "Ab1!" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Password_Without_Uppercase_Fails()
    {
        var model = new RegisterInputModel { Password = "lowercase1!" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Password_Without_Lowercase_Fails()
    {
        var model = new RegisterInputModel { Password = "UPPERCASE1!" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Password_Without_Digit_Fails()
    {
        var model = new RegisterInputModel { Password = "NoDigits!!" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Mismatched_ConfirmPassword_Fails()
    {
        var model = new RegisterInputModel
        {
            Password = "SecurePass1!",
            ConfirmPassword = "DifferentPass1!"
        };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ConfirmPassword);
    }

    [Fact]
    public void AcceptedTos_False_Fails()
    {
        var model = new RegisterInputModel { AcceptedTos = false };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.AcceptedTos);
    }

    [Fact]
    public void AcceptedTos_True_Passes()
    {
        var model = new RegisterInputModel { AcceptedTos = true };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.AcceptedTos);
    }
}
