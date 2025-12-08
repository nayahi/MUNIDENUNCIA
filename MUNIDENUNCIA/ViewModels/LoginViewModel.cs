using System.ComponentModel.DataAnnotations;

namespace MUNIDENUNCIA.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "El correo electrónico es requerido")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido")]
        [Display(Name = "Correo electrónico institucional")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es requerida")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Recordar mi sesión")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }
}

//DataAnnotations proporcionan validación tanto del lado del servidor como del cliente
//cuando se combinan con los scripts de validación de jQuery.