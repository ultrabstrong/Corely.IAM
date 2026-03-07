using Corely.IAM.Invitations.Constants;
using Corely.IAM.Invitations.Models;
using Corely.IAM.Invitations.Validators;
using FluentValidation.TestHelper;

namespace Corely.IAM.UnitTests.Invitations.Validators;

public class CreateInvitationRequestValidatorTests
{
    private readonly CreateInvitationRequestValidator _validator = new();

    private static CreateInvitationRequest ValidRequest =>
        new(
            Guid.CreateVersion7(),
            "valid@example.com",
            "A description",
            InvitationConstants.MIN_EXPIRY_SECONDS
        );

    [Fact]
    public void Validate_Passes_WithValidRequest()
    {
        var result = _validator.TestValidate(ValidRequest);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Fails_WhenAccountIdIsEmpty()
    {
        var request = ValidRequest with { AccountId = Guid.Empty };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.AccountId);
    }

    [Fact]
    public void Validate_Fails_WhenExpiresInSecondsBelowMinimum()
    {
        var request = ValidRequest with
        {
            ExpiresInSeconds = InvitationConstants.MIN_EXPIRY_SECONDS - 1,
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.ExpiresInSeconds);
    }

    [Fact]
    public void Validate_Fails_WhenExpiresInSecondsAboveMaximum()
    {
        var request = ValidRequest with
        {
            ExpiresInSeconds = InvitationConstants.MAX_EXPIRY_SECONDS + 1,
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.ExpiresInSeconds);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_Fails_WhenEmailIsNullOrEmpty(string? email)
    {
        var request = ValidRequest with { Email = email! };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_Fails_WhenEmailIsInvalidFormat()
    {
        var request = ValidRequest with { Email = "not-an-email" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_Fails_WhenEmailExceedsMaxLength()
    {
        var longEmail =
            new string('a', InvitationConstants.EMAIL_MAX_LENGTH - "@b.com".Length + 1) + "@b.com";
        var request = ValidRequest with { Email = longEmail };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_Fails_WhenDescriptionExceedsMaxLength()
    {
        var request = ValidRequest with
        {
            Description = new string('x', InvitationConstants.DESCRIPTION_MAX_LENGTH + 1),
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_Passes_WhenDescriptionIsNull()
    {
        var request = ValidRequest with { Description = null };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }
}
