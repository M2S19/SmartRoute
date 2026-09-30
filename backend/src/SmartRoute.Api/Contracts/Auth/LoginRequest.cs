using System.ComponentModel.DataAnnotations;

namespace SmartRoute.Api.Contracts.Auth;

public sealed class LoginRequest
{
    [Required(ErrorMessage = "L'email est obligatoire.")]
    [EmailAddress(ErrorMessage = "L'email n'est pas valide.")]
    [MaxLength(254, ErrorMessage = "L'email ne doit pas dépasser 254 caractères.")]
    public string? Email { get; init; }

    [Required(ErrorMessage = "Le mot de passe est obligatoire.")]
    [MaxLength(128, ErrorMessage = "Le mot de passe ne doit pas dépasser 128 caractères.")]
    public string? Password { get; init; }
}
