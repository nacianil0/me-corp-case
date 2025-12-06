using System.Text.Json;
using System.Text.Json.Serialization;
using MeCorp.Web.Domain.Interfaces;

namespace MeCorp.Web.Services;

public class HCaptchaService : IHCaptchaService
{
    private readonly HttpClient _httpClient;
    private readonly string _secretKey;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<HCaptchaService> _logger;
    private readonly bool _enabled;
    private const string VerifyUrl = "https://api.hcaptcha.com/siteverify";

    public HCaptchaService(
        HttpClient httpClient, 
        IConfiguration configuration, 
        IWebHostEnvironment environment, 
        ILogger<HCaptchaService> logger)
    {
        _httpClient = httpClient;
        _secretKey = configuration["HCaptcha:SecretKey"] ?? throw new InvalidOperationException("HCaptcha:SecretKey not configured");
        _environment = environment;
        _logger = logger;
        _enabled = configuration.GetValue<bool>("HCaptcha:Enabled", true);

        string siteKey = configuration["HCaptcha:SiteKey"] ?? "N/A";
        bool isTestMode = siteKey == "10000000-ffff-ffff-ffff-000000000001";
        
        _logger.LogInformation(
            "hCaptcha initialized - Environment: {Environment}, Mode: {Mode}, Enabled: {Enabled}",
            environment.EnvironmentName,
            isTestMode ? "TEST" : "PRODUCTION",
            _enabled);
    }

    public async Task<bool> VerifyTokenAsync(string token, string remoteIp)
    {
        if (!_enabled)
        {
            _logger.LogInformation("hCaptcha is disabled in configuration");
            return true;
        }

        if (_environment.IsDevelopment() && string.IsNullOrWhiteSpace(_secretKey))
        {
            _logger.LogInformation("hCaptcha bypassed in development mode (no secret key)");
            return true;
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("hCaptcha token is empty from IP: {IpAddress}", remoteIp);
            return false;
        }

        if (token == "dev-bypass" && _environment.IsDevelopment())
        {
            _logger.LogInformation("hCaptcha bypassed in development mode (dev-bypass token)");
            return true;
        }

        var parameters = new Dictionary<string, string>
        {
            { "secret", _secretKey },
            { "response", token },
            { "remoteip", remoteIp }
        };

        var content = new FormUrlEncodedContent(parameters);
        
        try
        {
            HttpResponseMessage response = await _httpClient.PostAsync(VerifyUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("hCaptcha verification request failed with status: {StatusCode}", response.StatusCode);
                return false;
            }

            string jsonResponse = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("hCaptcha API response: {Response}", jsonResponse);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            HCaptchaResponse? result = JsonSerializer.Deserialize<HCaptchaResponse>(jsonResponse, options);

            if (result is null)
            {
                _logger.LogWarning("hCaptcha response deserialization failed. Response: {Response}", jsonResponse);
                return false;
            }

            if (!result.Success)
            {
                string errorCodes = result.ErrorCodes != null ? string.Join(", ", result.ErrorCodes) : "Unknown";
                _logger.LogWarning("hCaptcha verification failed for IP: {IpAddress}. Error codes: {ErrorCodes}", 
                    remoteIp, errorCodes);
                return false;
            }

            _logger.LogInformation("hCaptcha verification successful for IP: {IpAddress}", remoteIp);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "hCaptcha verification threw an exception for IP: {IpAddress}", remoteIp);
            return false;
        }
    }

    private sealed class HCaptchaResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        
        [JsonPropertyName("challenge_ts")]
        public string? ChallengeTs { get; set; }
        
        [JsonPropertyName("hostname")]
        public string? Hostname { get; set; }
        
        [JsonPropertyName("error-codes")]
        public string[]? ErrorCodes { get; set; }
    }
}

