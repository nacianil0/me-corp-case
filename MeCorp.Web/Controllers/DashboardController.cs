using System.Security.Claims;
using MediatR;
using MeCorp.Web.Features.Dashboard;
using MeCorp.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeCorp.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        string? userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out int userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        var query = new GetDashboardQuery { UserId = userId };
        DashboardResult result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            return RedirectToAction("Login", "Auth");
        }

        var viewModel = new DashboardViewModel
        {
            Email = result.Email!,
            Role = result.Role,
            ReferralCode = result.ReferralCode!,
            CreatedAt = result.CreatedAt,
            TotalUsers = result.TotalUsers,
            CustomerCount = result.CustomerCount,
            ManagerCount = result.ManagerCount,
            Referrals = result.Referrals.Select(r => new ReferralViewModel
            {
                Email = r.Email,
                Role = r.Role,
                JoinedAt = r.JoinedAt
            }).ToList(),
            AllUsersWithReferrals = result.AllUsersWithReferrals.Select(u => new UserWithReferralsViewModel
            {
                Id = u.Id,
                Email = u.Email,
                Role = u.Role,
                CreatedAt = u.CreatedAt,
                ReferralCount = u.ReferralCount,
                Referrals = u.Referrals.Select(r => new ReferralViewModel
                {
                    Email = r.Email,
                    Role = r.Role,
                    JoinedAt = r.JoinedAt
                }).ToList()
            }).ToList()
        };

        return View(viewModel);
    }
}

