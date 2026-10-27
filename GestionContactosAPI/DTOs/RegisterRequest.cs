using System.ComponentModel.DataAnnotations;

namespace GestionContactosAPI.DTOs;

public class RegisterRequest
{
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres")]
    public string? Nombre { get; set; }

    [Required(ErrorMessage = "El correo electrónico es obligatorio")]
    [EmailAddress(ErrorMessage = "El formato de correo no es válido")]
    [StringLength(150, ErrorMessage = "El correo no puede superar los 150 caracteres")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener entre 6 y 100 caracteres")]
    public string Password { get; set; } = string.Empty;
}
