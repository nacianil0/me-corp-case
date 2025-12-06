using System.ComponentModel.DataAnnotations;

namespace MeCorp.Web.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    [MaxLength(128)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password confirmation is required.")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? ReferralCode { get; set; }

    public string? CaptchaToken { get; set; }

    public string? ErrorMessage { get; set; }

    public bool HasReferralCode => !string.IsNullOrWhiteSpace(ReferralCode);
}

