using System.ComponentModel.DataAnnotations;

namespace MeCorp.Web.Data.Entities;

public class LoginAttempt
{
    public int Id { get; set; }

    [MaxLength(45)]
    public required string IpAddress { get; set; }

    public int? UserId { get; set; }

    public User? User { get; set; }

    public DateTime AttemptTime { get; set; }

    public bool IsSuccessful { get; set; }
}

