using Microsoft.AspNetCore.Identity;
using SmartRoute.Api.Models;

namespace SmartRoute.Api.Auth;

/// <summary>
/// Hachage des mots de passe : PasswordHasher d'ASP.NET Core Identity (PBKDF2, sel aléatoire, format versionné).
/// </summary>
public sealed class PasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(User user, string password) => _hasher.HashPassword(user, password);

    public bool Verify(User user, string password)
    {
        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
