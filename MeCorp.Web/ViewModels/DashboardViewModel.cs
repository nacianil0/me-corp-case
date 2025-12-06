using MeCorp.Web.Domain.Enums;

namespace MeCorp.Web.ViewModels;

public class DashboardViewModel
{
    public required string Email { get; init; }
    public required UserRole Role { get; init; }
    public required string ReferralCode { get; init; }
    public required DateTime CreatedAt { get; init; }
    public int? TotalUsers { get; init; }
    public int? CustomerCount { get; init; }
    public int? ManagerCount { get; init; }

    public bool IsAdmin => Role == UserRole.Admin;

    public string ReferralLink => $"/Auth/Register?ref={ReferralCode}";
}

