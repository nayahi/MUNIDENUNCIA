using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MUNIDENUNCIA.Models
{
    /// <summary>
    /// Entidad que representa una denuncia ciudadana en el sistema municipal
    /// SEMANA 4: Actualizada con soporte para cifrado de datos sensibles y archivos adjuntos
    /// </summary>
    public class Denuncia
    {
        [Key]
        public int Id { get; set; }

        // ============================================================================
        // DATOS SENSIBLES CIFRADOS - SEMANA 4
        // Estos campos almacenarán datos cifrados usando Data Protection API
        // Los valores se cifran antes de guardar y descifran al leer
        // ============================================================================

        /// <summary>
        /// Cédula del ciudadano (CIFRADA en base de datos)
        /// Se almacena en formato cifrado para proteger información personal
        /// </summary>
        [Required]
        [StringLength(500)] // Más largo para almacenar datos cifrados
        public string CedulaCifrada { get; set; }

        /// <summary>
        /// Teléfono del ciudadano (CIFRADO en base de datos)
        /// Se almacena en formato cifrado para proteger información personal
        /// </summary>
        [Required]
        [StringLength(500)] // Más largo para almacenar datos cifrados
        public string TelefonoCifrado { get; set; }

        /// <summary>
        /// Email del ciudadano (CIFRADO en base de datos)
        /// Se almacena en formato cifrado para proteger información personal
        /// </summary>
        [Required]
        [StringLength(500)] // Más largo para almacenar datos cifrados
        public string EmailCifrado { get; set; }

        // ============================================================================
        // DATOS NO SENSIBLES - Sin cifrado
        // ============================================================================

        [Required]
        [StringLength(100)]
        public string NombreCompleto { get; set; }

        // ============================================================================
        // INFORMACIÓN DE LA DENUNCIA
        // ============================================================================

        [Required]
        public CategoriaDenuncia Categoria { get; set; }

        [Required]
        [StringLength(200)]
        public string Ubicacion { get; set; }

        [Required]
        [StringLength(1000)]
        public string Descripcion { get; set; }

        // ============================================================================
        // ARCHIVO ADJUNTO - SEMANA 4
        // Información del archivo PDF de evidencia
        // ============================================================================

        /// <summary>
        /// Nombre del archivo PDF original subido por el usuario
        /// Ejemplo: "evidencia_bache.pdf"
        /// </summary>
        [StringLength(255)]
        public string? ArchivoNombreOriginal { get; set; }

        /// <summary>
        /// Nombre del archivo guardado en el servidor (aleatorio por seguridad)
        /// Ejemplo: "e7f3a9b2-4c1d-8e6f-9a3b-5d2c4e1f7a8b.pdf"
        /// </summary>
        [StringLength(255)]
        public string? ArchivoNombreServidor { get; set; }

        /// <summary>
        /// Ruta completa donde se almacenó el archivo en el servidor
        /// Ejemplo: "~/uploads/denuncias/e7f3a9b2-4c1d-8e6f-9a3b-5d2c4e1f7a8b.pdf"
        /// </summary>
        [StringLength(500)]
        public string? ArchivoRuta { get; set; }

        /// <summary>
        /// Tamaño del archivo en bytes
        /// </summary>
        public long? ArchivoTamanoBytes { get; set; }

        /// <summary>
        /// Tipo MIME del archivo (siempre application/pdf en este caso)
        /// </summary>
        [StringLength(100)]
        public string? ArchivoTipoMime { get; set; }

        /// <summary>
        /// Fecha y hora en que se subió el archivo
        /// </summary>
        public DateTime? ArchivoFechaSubida { get; set; }

        // ============================================================================
        // CONTROL DEL SISTEMA
        // ============================================================================

        [Required]
        [StringLength(50)]
        public string Estado { get; set; } // Recibida, EnProceso, Resuelta, Rechazada

        [Required]
        public DateTime FechaCreacion { get; set; }

        public DateTime? FechaActualizacion { get; set; }

        public DateTime? FechaResolucion { get; set; }

        [StringLength(500)]
        public string? Observaciones { get; set; }

        /// <summary>
        /// Usuario (funcionario) que tiene asignada esta denuncia
        /// Se relaciona con AspNetUsers (Identity)
        /// </summary>
        [StringLength(450)]
        public string? AsignadoAUserId { get; set; }

        // ============================================================================
        // PROPIEDADES NO MAPEADAS - SEMANA 4
        // Estas propiedades NO se guardan en BD, solo se usan en memoria
        // Para manipular los datos descifrados
        // ============================================================================

        /// <summary>
        /// Cédula descifrada para uso en la aplicación (NO se mapea a BD)
        /// </summary>
        [NotMapped]
        public string Cedula { get; set; }

        /// <summary>
        /// Teléfono descifrado para uso en la aplicación (NO se mapea a BD)
        /// </summary>
        [NotMapped]
        public string Telefono { get; set; }

        /// <summary>
        /// Email descifrado para uso en la aplicación (NO se mapea a BD)
        /// </summary>
        [NotMapped]
        public string Email { get; set; }

        // ============================================================================
        // CONSTRUCTOR
        // ============================================================================

        public Denuncia()
        {
            FechaCreacion = DateTime.UtcNow;
            Estado = "Recibida"; // Estado inicial
        }
    }

    /// <summary>
    /// Categorías de denuncias ciudadanas en San José
    /// </summary>
    public enum CategoriaDenuncia
    {
        [Display(Name = "Bache en Calle")]
        BacheCalle = 1,

        [Display(Name = "Alumbrado Público Defectuoso")]
        AlumbradoPublico = 2,

        [Display(Name = "Acumulación de Basura")]
        AcumulacionBasura = 3,

        [Display(Name = "Problema con Alcantarillado")]
        ProblemaAlcantarillado = 4,

        [Display(Name = "Vandalismo o Grafiti")]
        VandalismoGrafiti = 5,

        [Display(Name = "Señalización Vial Dañada")]
        SenalizacionDanada = 6,

        [Display(Name = "Parque o Área Verde Descuidada")]
        ParqueDescuidado = 7,

        [Display(Name = "Ruido Excesivo")]
        RuidoExcesivo = 8,

        [Display(Name = "Otros")]
        Otros = 99
    }

    /// <summary>
    /// Estados posibles de una denuncia - SEMANA 4
    /// </summary>
    public static class EstadoDenuncia
    {
        public const string Recibida = "Recibida";
        public const string EnProceso = "EnProceso";
        public const string Resuelta = "Resuelta";
        public const string Rechazada = "Rechazada";
    }
}
