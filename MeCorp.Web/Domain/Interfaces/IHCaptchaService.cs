namespace MeCorp.Web.Domain.Interfaces;

public interface IHCaptchaService
{
    Task<bool> VerifyTokenAsync(string token, string remoteIp);
}

