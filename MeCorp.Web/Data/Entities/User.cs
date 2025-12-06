using System.ComponentModel.DataAnnotations;
using MeCorp.Web.Domain.Enums;

namespace MeCorp.Web.Data.Entities;

public class User
{
    public int Id { get; set; }

    [MaxLength(256)]
    public required string Email { get; set; }

    [MaxLength(512)]
    public required string PasswordHash { get; set; }

    public UserRole Role { get; set; }

    [MaxLength(32)]
    public required string ReferralCode { get; set; }

    public int? ReferredBy { get; set; }

    public User? Referrer { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<User> Referrals { get; set; } = new List<User>();

    public ICollection<LoginAttempt> LoginAttempts { get; set; } = new List<LoginAttempt>();
}

