using System.ComponentModel.DataAnnotations;

namespace SmartRoute.Api.DTOs.Deliveries;

/// <summary>
/// Confirmation d'une proposition de assign-best-driver. Le serveur ne fait pas confiance à ces valeurs :
/// il revérifie la tournée, sa version et recalcule la position d'insertion.
/// </summary>
public class ConfirmAssignmentRequest
{
    /// <summary>Adresse de la livraison (celle envoyée à assign-best-driver).</summary>
    [Required(ErrorMessage = "L'adresse de livraison est obligatoire.")]
    public required AddressRequest Address { get; set; }

    /// <summary>Tournée proposée.</summary>
    [Required(ErrorMessage = "La tournée est obligatoire.")]
    public required string RouteId { get; set; }

    /// <summary>Position d'insertion proposée (1 = premier arrêt).</summary>
    [Range(1, int.MaxValue, ErrorMessage = "La position d'insertion doit être supérieure ou égale à 1.")]
    public int InsertionPosition { get; set; }

    /// <summary>Version de la tournée au moment de la proposition (contrôle de concurrence).</summary>
    [Range(0, int.MaxValue, ErrorMessage = "La version de la tournée est invalide.")]
    public int RouteVersion { get; set; }
}
