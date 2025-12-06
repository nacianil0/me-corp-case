using System.Security.Claims;
using MediatR;
using MeCorp.Web.Data;
using MeCorp.Web.Data.Entities;
using MeCorp.Web.Domain.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace MeCorp.Web.Features.Auth.Login;

public class LoginCommand : IRequest<LoginResult>
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required string CaptchaToken { get; init; }
    public required string IpAddress { get; init; }
    public required HttpContext HttpContext { get; init; }

    public class Handler : IRequestHandler<LoginCommand, LoginResult>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IHashingService _hashingService;
        private readonly ICaptchaService _captchaService;
        private readonly ILogger<Handler> _logger;

        public Handler(
            ApplicationDbContext dbContext,
            IHashingService hashingService,
            ICaptchaService captchaService,
            ILogger<Handler> logger)
        {
            _dbContext = dbContext;
            _hashingService = hashingService;
            _captchaService = captchaService;
            _logger = logger;
        }

        public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            bool captchaValid = await _captchaService.VerifyTokenAsync(request.CaptchaToken, request.IpAddress);
            if (!captchaValid)
            {
                _logger.LogWarning("CAPTCHA validation failed for IP: {IpAddress}", request.IpAddress);
                return LoginResult.CaptchaFailed();
            }

            User? user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

            if (user is null || !_hashingService.VerifyPassword(request.Password, user.PasswordHash))
            {
                await RecordLoginAttempt(request.IpAddress, user?.Id, false, cancellationToken);
                _logger.LogWarning("Failed login attempt for email: {Email} from IP: {IpAddress}", request.Email, request.IpAddress);
                return LoginResult.InvalidCredentials();
            }

            await RecordLoginAttempt(request.IpAddress, user.Id, true, cancellationToken);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
            };

            await request.HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authProperties);

            _logger.LogInformation("Successful login for user: {UserId} from IP: {IpAddress}", user.Id, request.IpAddress);
            return LoginResult.Success();
        }

        private async Task RecordLoginAttempt(string ipAddress, int? userId, bool isSuccessful, CancellationToken cancellationToken)
        {
            var attempt = new LoginAttempt
            {
                IpAddress = ipAddress,
                UserId = userId,
                AttemptTime = DateTime.UtcNow,
                IsSuccessful = isSuccessful
            };

            _dbContext.LoginAttempts.Add(attempt);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

public class LoginResult
{
    public bool IsSuccess { get; private init; }
    public string? ErrorMessage { get; private init; }
    public bool IsCaptchaError { get; private init; }

    public static LoginResult Success() => new() { IsSuccess = true };

    public static LoginResult InvalidCredentials() => new()
    {
        IsSuccess = false,
        ErrorMessage = "Invalid email or password."
    };

    public static LoginResult CaptchaFailed() => new()
    {
        IsSuccess = false,
        ErrorMessage = "CAPTCHA verification failed. Please try again.",
        IsCaptchaError = true
    };
}

