namespace MeCorp.Web.Domain.Interfaces;

public interface IHashingService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}

