using System.Text.Json;
using System.Text.Json.Serialization;
using MeCorp.Web.Domain.Interfaces;

namespace MeCorp.Web.Services;

public class CaptchaService : ICaptchaService
{
    private readonly HttpClient _httpClient;
    private readonly string _secretKey;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<CaptchaService> _logger;
    private readonly bool _enabled;
    private const double ScoreThreshold = 0.5;
    private const string PlaceholderSecretKey = "your-secret-key";

    public CaptchaService(HttpClient httpClient, IConfiguration configuration, IWebHostEnvironment environment, ILogger<CaptchaService> logger)
    {
        _httpClient = httpClient;
        _secretKey = configuration["ReCaptcha:SecretKey"] ?? throw new InvalidOperationException("ReCaptcha:SecretKey not configured");
        _environment = environment;
        _logger = logger;
        _enabled = configuration.GetValue<bool>("ReCaptcha:Enabled", true);
    }

    public async Task<bool> VerifyTokenAsync(string token, string remoteIp)
    {
        if (!_enabled)
        {
            _logger.LogInformation("CAPTCHA is disabled in configuration");
            return true;
        }

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

        if (token == "dev-bypass" && _environment.IsDevelopment())
        {
            _logger.LogInformation("CAPTCHA bypassed in development mode (dev-bypass token)");
            return true;
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
        _logger.LogInformation("CAPTCHA API response: {Response}", jsonResponse);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var result = JsonSerializer.Deserialize<CaptchaResponse>(jsonResponse, options);

        if (result is null)
        {
            _logger.LogWarning("CAPTCHA response deserialization failed. Response: {Response}", jsonResponse);
            return false;
        }

        if (!result.Success)
        {
            var errorCodes = result.ErrorCodes != null ? string.Join(", ", result.ErrorCodes) : "Unknown";
            _logger.LogWarning("CAPTCHA verification failed for IP: {IpAddress}. Error codes: {ErrorCodes}. Response: {Response}", 
                remoteIp, errorCodes, jsonResponse);
            return false;
        }

        if (result.Score > 0 && result.Score < ScoreThreshold)
        {
            _logger.LogWarning("CAPTCHA score {Score} below threshold {Threshold} for IP: {IpAddress}", 
                result.Score, ScoreThreshold, remoteIp);
            return false;
        }
        
        if (result.Score == 0 && result.Success)
        {
            _logger.LogInformation("CAPTCHA score is 0 but success is true. This might be v2 or test mode. Allowing.");
        }

        _logger.LogInformation("CAPTCHA verification successful. Score: {Score}, Action: {Action}", result.Score, result.Action);
        return true;
    }

    private sealed class CaptchaResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        
        [JsonPropertyName("score")]
        public double Score { get; set; }
        
        [JsonPropertyName("action")]
        public string? Action { get; set; }
        
        [JsonPropertyName("challenge_ts")]
        public string? ChallengeTs { get; set; }
        
        [JsonPropertyName("hostname")]
        public string? Hostname { get; set; }
        
        [JsonPropertyName("error-codes")]
        public string[]? ErrorCodes { get; set; }
    }
}

