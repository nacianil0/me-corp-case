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
    public List<ReferralViewModel> Referrals { get; init; } = new();
    public List<UserWithReferralsViewModel> AllUsersWithReferrals { get; init; } = new();

    public bool IsAdmin => Role == UserRole.Admin;
    public bool HasReferrals => Referrals.Count > 0;

    public string ReferralLink => $"/Auth/Register?ref={ReferralCode}";
}

public class ReferralViewModel
{
    public required string Email { get; init; }
    public required UserRole Role { get; init; }
    public required DateTime JoinedAt { get; init; }
}

public class UserWithReferralsViewModel
{
    public int Id { get; init; }
    public required string Email { get; init; }
    public required UserRole Role { get; init; }
    public required DateTime CreatedAt { get; init; }
    public int ReferralCount { get; init; }
    public List<ReferralViewModel> Referrals { get; init; } = new();
}

