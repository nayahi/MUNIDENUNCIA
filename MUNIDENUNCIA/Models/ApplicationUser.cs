using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace MUNIDENUNCIA.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string NombreCompleto { get; set; } = string.Empty;

        [StringLength(100)]
        public string Departamento { get; set; } = string.Empty;

        [StringLength(50)]
        public string Cedula { get; set; } = string.Empty;

        public DateTime FechaRegistro { get; set; }

        public DateTime? UltimoAcceso { get; set; }

        public bool RequiereCambioContrasena { get; set; }

        public virtual ICollection<AuditLog> LogsAuditoria { get; set; }
            = new List<AuditLog>();
    }
}
