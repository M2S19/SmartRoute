using SmartRoute.Core.Entities;

namespace SmartRoute.Core.Interfaces;

public sealed record AccessToken(string Token, DateTime ExpiresAtUtc);

public interface ITokenService
{
    AccessToken CreateToken(User user);
}
