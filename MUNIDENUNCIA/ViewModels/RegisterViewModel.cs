using System.ComponentModel.DataAnnotations;

namespace MUNIDENUNCIA.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "El nombre completo es requerido")]
        [StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
        [Display(Name = "Nombre completo")]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required(ErrorMessage = "La cédula es requerida")]
        [RegularExpression(@"^\d{9}$",
            ErrorMessage = "La cédula debe contener 9 dígitos")]
        [Display(Name = "Cédula de identidad")]
        public string Cedula { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es requerido")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido")]
        [Display(Name = "Correo institucional")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "El departamento es requerido")]
        [StringLength(100)]
        [Display(Name = "Departamento")]
        public string Departamento { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es requerida")]
        [StringLength(100, MinimumLength = 12,
            ErrorMessage = "Mínimo 12 caracteres")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contraseña")]
        [Compare("Password",
            ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe seleccionar un rol")]
        [Display(Name = "Rol del funcionario")]
        public string Role { get; set; } = string.Empty;
    }
}
