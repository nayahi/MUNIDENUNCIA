using System.ComponentModel.DataAnnotations;

namespace MUNIDENUNCIA.ViewModels
{
    public class ConfirmEmailViewModel
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;

        public bool EmailConfirmed { get; set; }

        public string? ErrorMessage { get; set; }
    }
}
