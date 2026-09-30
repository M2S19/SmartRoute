namespace SmartRoute.Api.Errors;

/// <summary>
/// Seule exception métier de l'API. Convertie en ProblemDetails par <see cref="GlobalExceptionHandler"/> :
/// <paramref name="status"/> → statut HTTP, <paramref name="code"/> → extension « code », message → « detail ».
/// </summary>
public sealed class ApiException(int status, string code, string message) : Exception(message)
{
    public int Status { get; } = status;

    public string Code { get; } = code;
}

/// <summary>Codes d'erreur stables, lus par le front dans l'extension « code » du ProblemDetails.</summary>
public static class ErrorCodes
{
    public const string ValidationError = "VALIDATION_ERROR";
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string NotFound = "NOT_FOUND";
    public const string InternalError = "INTERNAL_ERROR";
}
