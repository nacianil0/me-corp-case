using FluentValidation;

namespace MeCorp.Web.Features.Auth.Register;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters.")
            .MaximumLength(128).WithMessage("Password must not exceed 128 characters.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Password confirmation is required.")
            .Equal(x => x.Password).WithMessage("Passwords do not match.");

        RuleFor(x => x.ReferralCode)
            .MaximumLength(32).WithMessage("Referral code must not exceed 32 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.ReferralCode));

        RuleFor(x => x.CaptchaToken)
            .NotEmpty().WithMessage("CAPTCHA verification is required.");

        RuleFor(x => x.IpAddress)
            .NotEmpty().WithMessage("IP address is required.");
    }
}

