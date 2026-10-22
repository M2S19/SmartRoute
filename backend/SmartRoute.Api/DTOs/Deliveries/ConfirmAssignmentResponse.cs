namespace SmartRoute.Api.DTOs.Deliveries;

/// <summary>Affectation enregistrée : la livraison est un nouvel arrêt de la tournée.</summary>
public class ConfirmAssignmentResponse
{
    public required string RouteId { get; set; }

    public required string RouteName { get; set; }

    /// <summary>Nouvelle version de la tournée après l'insertion.</summary>
    public int RouteVersion { get; set; }

    /// <summary>Identifiant de l'arrêt créé.</summary>
    public required string StopId { get; set; }

    /// <summary>Ordre de l'arrêt créé dans la tournée (1 = premier arrêt).</summary>
    public int InsertionPosition { get; set; }

    public double ExtraDistanceKm { get; set; }

    public required DriverSummaryResponse Driver { get; set; }
}
