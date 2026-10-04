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
