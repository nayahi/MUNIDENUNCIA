using System.ComponentModel.DataAnnotations;

namespace MUNIDENUNCIA.ViewModels
{
    public class VerifyMfaViewModel
    {
        [Required(ErrorMessage = "El código de verificación es requerido")]
        [StringLength(6, MinimumLength = 6,
            ErrorMessage = "El código debe tener 6 dígitos")]
        [Display(Name = "Código de Verificación")]
        public string VerificationCode { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }
}
