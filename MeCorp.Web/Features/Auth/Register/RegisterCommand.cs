using System.Security.Cryptography;
using MediatR;
using MeCorp.Web.Data;
using MeCorp.Web.Data.Entities;
using MeCorp.Web.Domain.Enums;
using MeCorp.Web.Domain.Interfaces;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace MeCorp.Web.Features.Auth.Register;

public class RegisterCommand : IRequest<RegisterResult>
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required string ConfirmPassword { get; init; }
    public string? ReferralCode { get; init; }
    public required string CaptchaToken { get; init; }
    public required string IpAddress { get; init; }

    public class Handler : IRequestHandler<RegisterCommand, RegisterResult>
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

        public async Task<RegisterResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            bool captchaValid = await _captchaService.VerifyTokenAsync(request.CaptchaToken, request.IpAddress);
            if (!captchaValid)
            {
                _logger.LogWarning("CAPTCHA validation failed during registration from IP: {IpAddress}", request.IpAddress);
                return RegisterResult.CaptchaFailed();
            }

            bool emailExists = await _dbContext.Users
                .AnyAsync(u => u.Email == request.Email, cancellationToken);

            if (emailExists)
            {
                return RegisterResult.EmailTaken();
            }

            User? referrer = null;
            UserRole role = UserRole.Customer;

            if (!string.IsNullOrWhiteSpace(request.ReferralCode))
            {
                referrer = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.ReferralCode == request.ReferralCode, cancellationToken);

                if (referrer is null)
                {
                    _logger.LogWarning("Invalid referral code attempt: {ReferralCode} from IP: {IpAddress}",
                        request.ReferralCode, request.IpAddress);
                    return RegisterResult.InvalidReferralCode();
                }

                if (referrer.Role == UserRole.Manager || referrer.Role == UserRole.Admin)
                {
                    role = UserRole.Manager;
                }
            }

            string newReferralCode = GenerateReferralCode();

            var user = new User
            {
                Email = request.Email,
                PasswordHash = _hashingService.HashPassword(request.Password),
                Role = role,
                ReferralCode = newReferralCode,
                ReferredBy = referrer?.Id,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("New user registered: {UserId} as {Role} from IP: {IpAddress}",
                user.Id, role, request.IpAddress);

            return RegisterResult.Success(user.Id);
        }

        private static string GenerateReferralCode()
        {
            byte[] randomBytes = RandomNumberGenerator.GetBytes(12);
            return WebEncoders.Base64UrlEncode(randomBytes);
        }
    }
}

public class RegisterResult
{
    public bool IsSuccess { get; private init; }
    public int? UserId { get; private init; }
    public string? ErrorMessage { get; private init; }
    public bool IsCaptchaError { get; private init; }

    public static RegisterResult Success(int userId) => new() { IsSuccess = true, UserId = userId };

    public static RegisterResult EmailTaken() => new()
    {
        IsSuccess = false,
        ErrorMessage = "This email address is already registered."
    };

    public static RegisterResult InvalidReferralCode() => new()
    {
        IsSuccess = false,
        ErrorMessage = "Invalid referral code."
    };

    public static RegisterResult CaptchaFailed() => new()
    {
        IsSuccess = false,
        ErrorMessage = "CAPTCHA verification failed. Please try again.",
        IsCaptchaError = true
    };
}

