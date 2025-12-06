namespace MeCorp.Web.Domain.Interfaces;

public interface ICaptchaService
{
    Task<bool> VerifyTokenAsync(string token, string remoteIp);
}

