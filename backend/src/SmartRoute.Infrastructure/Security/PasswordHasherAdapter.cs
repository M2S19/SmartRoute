using Microsoft.AspNetCore.Identity;
using SmartRoute.Core.Interfaces;

namespace SmartRoute.Infrastructure.Security;

/// <summary>
/// Hachage via le PasswordHasher d'ASP.NET Core Identity (PBKDF2, sel aléatoire, format versionné).
/// </summary>
public sealed class PasswordHasherAdapter : IPasswordHasher
{
    // L'implémentation par défaut n'utilise pas l'instance utilisateur : un objet neutre suffit.
    private static readonly object NoUser = new();
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(NoUser, password);

    public bool Verify(string passwordHash, string providedPassword)
    {
        var result = _hasher.VerifyHashedPassword(NoUser, passwordHash, providedPassword);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
