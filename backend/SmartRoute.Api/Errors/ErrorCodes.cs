namespace SmartRoute.Api.Errors;

/// <summary>Codes d'erreur stables, lus par le front dans l'extension « code » du ProblemDetails.</summary>
public static class ErrorCodes
{
    public const string ValidationError = "VALIDATION_ERROR";
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string NotFound = "NOT_FOUND";
    public const string InternalError = "INTERNAL_ERROR";
    public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";
    public const string Conflict = "CONFLICT";
    public const string UnprocessableEntity = "UNPROCESSABLE_ENTITY";

    // Affectation d'une livraison (US2)
    public const string NoEligibleRoute = "NO_ELIGIBLE_ROUTE";
    public const string RouteNotEligible = "ROUTE_NOT_ELIGIBLE";
    public const string ProposalOutdated = "PROPOSAL_OUTDATED";

    // Tournée du livreur (US3)
    public const string RouteNotModifiable = "ROUTE_NOT_MODIFIABLE";
    public const string StopAlreadyDelivered = "STOP_ALREADY_DELIVERED";
    public const string StopNotNext = "STOP_NOT_NEXT";
}
