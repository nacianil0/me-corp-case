using System.Text.Json;
using MeCorp.Web.Domain.Interfaces;

namespace MeCorp.Web.Services;

public class CaptchaService : ICaptchaService
{
    private readonly HttpClient _httpClient;
    private readonly string _secretKey;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<CaptchaService> _logger;
    private const double ScoreThreshold = 0.5;
    private const string PlaceholderSecretKey = "your-secret-key";

    public CaptchaService(HttpClient httpClient, IConfiguration configuration, IWebHostEnvironment environment, ILogger<CaptchaService> logger)
    {
        _httpClient = httpClient;
        _secretKey = configuration["ReCaptcha:SecretKey"] ?? throw new InvalidOperationException("ReCaptcha:SecretKey not configured");
        _environment = environment;
        _logger = logger;
    }

    public async Task<bool> VerifyTokenAsync(string token, string remoteIp)
    {
        if (_environment.IsDevelopment() && (_secretKey == PlaceholderSecretKey || string.IsNullOrWhiteSpace(_secretKey)))
        {
            _logger.LogInformation("CAPTCHA bypassed in development mode (placeholder key detected)");
            return true;
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("CAPTCHA token is empty from IP: {IpAddress}", remoteIp);
            return false;
        }

        var parameters = new Dictionary<string, string>
        {
            { "secret", _secretKey },
            { "response", token },
            { "remoteip", remoteIp }
        };

        var content = new FormUrlEncodedContent(parameters);
        var response = await _httpClient.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("CAPTCHA verification request failed with status: {StatusCode}", response.StatusCode);
            return false;
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<CaptchaResponse>(jsonResponse);

        if (result is null || !result.Success)
        {
            _logger.LogWarning("CAPTCHA verification failed for IP: {IpAddress}", remoteIp);
            return false;
        }

        if (result.Score < ScoreThreshold)
        {
            _logger.LogWarning("CAPTCHA score {Score} below threshold for IP: {IpAddress}", result.Score, remoteIp);
            return false;
        }

        return true;
    }

    private sealed class CaptchaResponse
    {
        public bool Success { get; set; }
        public double Score { get; set; }
        public string? Action { get; set; }
        public DateTime ChallengeTs { get; set; }
        public string? Hostname { get; set; }
    }
}

