using MediatR;
using MeCorp.Web.Features.Auth.Login;
using MeCorp.Web.Features.Auth.Register;
using MeCorp.Web.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace MeCorp.Web.Controllers;

public class AuthController : Controller
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;

    public AuthController(IMediator mediator, IConfiguration configuration)
    {
        _mediator = mediator;
        _configuration = configuration;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        bool captchaEnabled = _configuration.GetValue<bool>("ReCaptcha:Enabled", true);
        string? siteKey = _configuration["ReCaptcha:SiteKey"];
        ViewBag.ReturnUrl = returnUrl;
        ViewBag.SiteKey = siteKey;
        ViewBag.HasValidCaptcha = captchaEnabled && 
                                   !string.IsNullOrWhiteSpace(siteKey) && 
                                   siteKey != "your-site-key" && 
                                   siteKey != "your-recaptcha-site-key-here";
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        bool captchaEnabled = _configuration.GetValue<bool>("ReCaptcha:Enabled", true);
        string? siteKey = _configuration["ReCaptcha:SiteKey"];
        ViewBag.ReturnUrl = returnUrl;
        ViewBag.SiteKey = siteKey;
        ViewBag.HasValidCaptcha = captchaEnabled && 
                                   !string.IsNullOrWhiteSpace(siteKey) && 
                                   siteKey != "your-site-key" && 
                                   siteKey != "your-recaptcha-site-key-here";

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var command = new LoginCommand
        {
            Email = model.Email,
            Password = model.Password,
            CaptchaToken = model.CaptchaToken ?? string.Empty,
            IpAddress = ipAddress,
            HttpContext = HttpContext
        };

        LoginResult result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            model.ErrorMessage = result.ErrorMessage;
            return View(model);
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpGet]
    public IActionResult Register(string? @ref = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        bool captchaEnabled = _configuration.GetValue<bool>("ReCaptcha:Enabled", true);
        string? siteKey = _configuration["ReCaptcha:SiteKey"];
        ViewBag.SiteKey = siteKey;
        ViewBag.HasValidCaptcha = captchaEnabled && 
                                   !string.IsNullOrWhiteSpace(siteKey) && 
                                   siteKey != "your-site-key" && 
                                   siteKey != "your-recaptcha-site-key-here";
        return View(new RegisterViewModel { ReferralCode = @ref });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        bool captchaEnabled = _configuration.GetValue<bool>("ReCaptcha:Enabled", true);
        string? siteKey = _configuration["ReCaptcha:SiteKey"];
        ViewBag.SiteKey = siteKey;
        ViewBag.HasValidCaptcha = captchaEnabled && 
                                   !string.IsNullOrWhiteSpace(siteKey) && 
                                   siteKey != "your-site-key" && 
                                   siteKey != "your-recaptcha-site-key-here";

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var command = new RegisterCommand
        {
            Email = model.Email,
            Password = model.Password,
            ConfirmPassword = model.ConfirmPassword,
            ReferralCode = model.ReferralCode,
            CaptchaToken = model.CaptchaToken ?? string.Empty,
            IpAddress = ipAddress
        };

        RegisterResult result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            model.ErrorMessage = result.ErrorMessage;
            return View(model);
        }

        TempData["SuccessMessage"] = "Registration successful. Please log in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }
}

