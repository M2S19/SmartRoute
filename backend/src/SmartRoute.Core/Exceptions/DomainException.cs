namespace SmartRoute.Core.Exceptions;

/// <summary>
/// Erreur métier attendue. <see cref="Code"/> est le code stable renvoyé au front
/// (extension « code » du ProblemDetails) ; le statut HTTP est choisi par l'API.
/// </summary>
public abstract class DomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed class InvalidCredentialsException()
    : DomainException(ErrorCodes.InvalidCredentials, "Email ou mot de passe incorrect.");
