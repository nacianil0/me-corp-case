using MediatR;
using MeCorp.Web.Data;
using MeCorp.Web.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MeCorp.Web.Features.Dashboard;

public class GetDashboardQuery : IRequest<DashboardResult>
{
    public required int UserId { get; init; }

    public class Handler : IRequestHandler<GetDashboardQuery, DashboardResult>
    {
        private readonly ApplicationDbContext _dbContext;

        public Handler(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<DashboardResult> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
        {
            var user = await _dbContext.Users
                .Where(u => u.Id == request.UserId)
                .Select(u => new { u.Email, u.Role, u.ReferralCode, u.CreatedAt })
                .FirstOrDefaultAsync(cancellationToken);

            if (user is null)
            {
                return DashboardResult.NotFound();
            }

            var referrals = await _dbContext.Users
                .Where(u => u.Referrer != null && u.Referrer.Id == request.UserId)
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new ReferralDto
                {
                    Email = u.Email,
                    Role = u.Role,
                    JoinedAt = u.CreatedAt
                })
                .ToListAsync(cancellationToken);

            var result = new DashboardResult
            {
                IsSuccess = true,
                Email = user.Email,
                Role = user.Role,
                ReferralCode = user.ReferralCode,
                CreatedAt = user.CreatedAt,
                Referrals = referrals
            };

            if (user.Role == UserRole.Admin)
            {
                result.TotalUsers = await _dbContext.Users.CountAsync(cancellationToken);
                result.CustomerCount = await _dbContext.Users.CountAsync(u => u.Role == UserRole.Customer, cancellationToken);
                result.ManagerCount = await _dbContext.Users.CountAsync(u => u.Role == UserRole.Manager, cancellationToken);
            }

            return result;
        }
    }
}

public class ReferralDto
{
    public required string Email { get; init; }
    public UserRole Role { get; init; }
    public DateTime JoinedAt { get; init; }
}

public class DashboardResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Email { get; set; }
    public UserRole Role { get; set; }
    public string? ReferralCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? TotalUsers { get; set; }
    public int? CustomerCount { get; set; }
    public int? ManagerCount { get; set; }
    public List<ReferralDto> Referrals { get; set; } = new();

    public static DashboardResult NotFound() => new()
    {
        IsSuccess = false,
        ErrorMessage = "User not found."
    };
}

