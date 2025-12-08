using System;
using System.ComponentModel.DataAnnotations;
using MUNIDENUNCIA.Models;

namespace MUNIDENUNCIA.ViewModels
{
    /// <summary>
    /// ViewModel para solicitud de permiso CON validación completa
    /// VERSIÓN SEGURA - Implementa Data Annotations y validaciones de negocio
    /// </summary>
    public class SolicitudPermisoValidadoViewModel
    {
        // ===== INFORMACIÓN DEL PROPIETARIO =====

        [Required(ErrorMessage = "La cédula del propietario es obligatoria")]
        [RegularExpression(@"^\d{9}$|^\d{10}$",
            ErrorMessage = "La cédula debe tener 9 dígitos (física) o 10 dígitos (jurídica)")]
        [Display(Name = "Cédula del Propietario")]
        public string CedulaPropietario { get; set; }

        [Required(ErrorMessage = "El nombre completo es obligatorio")]
        [StringLength(100, MinimumLength = 5,
            ErrorMessage = "El nombre debe tener entre 5 y 100 caracteres")]
        [Display(Name = "Nombre Completo")]
        public string NombreCompletoPropietario { get; set; }

        [Required(ErrorMessage = "El correo electrónico es obligatorio")]
        [EmailAddress(ErrorMessage = "Debe proporcionar un correo electrónico válido")]
        [StringLength(100, ErrorMessage = "El correo no puede exceder 100 caracteres")]
        [Display(Name = "Correo Electrónico")]
        public string EmailPropietario { get; set; }

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [RegularExpression(@"^\d{8}$",
            ErrorMessage = "El teléfono debe tener exactamente 8 dígitos")]
        [Display(Name = "Teléfono")]
        public string TelefonoPropietario { get; set; }

        // ===== INFORMACIÓN DE LA PROPIEDAD =====

        [Required(ErrorMessage = "El distrito es obligatorio")]
        [StringLength(50, MinimumLength = 3,
            ErrorMessage = "El distrito debe tener entre 3 y 50 caracteres")]
        [Display(Name = "Distrito")]
        public string Distrito { get; set; }

        [Required(ErrorMessage = "La dirección completa es obligatoria")]
        [StringLength(300, MinimumLength = 10,
            ErrorMessage = "La dirección debe tener entre 10 y 300 caracteres")]
        [Display(Name = "Dirección Completa")]
        public string DireccionCompleta { get; set; }

        [Required(ErrorMessage = "El número de plano catastrado es obligatorio")]
        [RegularExpression(@"^[HPC]?\d{1}-\d{6}-\d{4}$",
            ErrorMessage = "Formato de plano inválido. Ejemplo: H-1-123456-2023 o 1-123456-2023")]
        [Display(Name = "Plano Catastrado")]
        public string PlanoCatastrado { get; set; }

        // ===== INFORMACIÓN DEL PROYECTO =====

        [Required(ErrorMessage = "Debe seleccionar el tipo de construcción")]
        [Display(Name = "Tipo de Construcción")]
        public TipoConstruccion TipoConstruccion { get; set; }

        [Required(ErrorMessage = "El área de construcción es obligatoria")]
        [Range(1, 5000, ErrorMessage = "El área debe estar entre 1 m² y 5,000 m²")]
        [Display(Name = "Área de Construcción (m²)")]
        public decimal AreaConstruccionM2 { get; set; }

        [Required(ErrorMessage = "El número de plantas es obligatorio")]
        [Range(1, 10, ErrorMessage = "El número de plantas debe estar entre 1 y 10")]
        [Display(Name = "Número de Plantas")]
        public int NumeroPlantas { get; set; }

        [Required(ErrorMessage = "La descripción del proyecto es obligatoria")]
        [StringLength(2000, MinimumLength = 20,
            ErrorMessage = "La descripción debe tener entre 20 y 2000 caracteres")]
        [Display(Name = "Descripción del Proyecto")]
        public string DescripcionProyecto { get; set; }

        [Required(ErrorMessage = "El presupuesto estimado es obligatorio")]
        [Range(100000, 500000000,
            ErrorMessage = "El presupuesto debe estar entre ₡100,000 y ₡500,000,000")]
        [Display(Name = "Presupuesto Estimado (₡)")]
        [DataType(DataType.Currency)]
        public decimal PresupuestoEstimado { get; set; }

        public SolicitudPermisoValidadoViewModel()
        {
            // Valores predeterminados seguros
            NumeroPlantas = 1;
        }
    }

    /// <summary>
    /// ViewModel para agregar comentarios CON validación
    /// VERSIÓN SEGURA - Implementa validación completa
    /// </summary>
    public class ComentarioValidadoViewModel
    {
        [Required]
        public int SolicitudPermisoId { get; set; }

        [Required(ErrorMessage = "El nombre del funcionario es obligatorio")]
        [StringLength(100, MinimumLength = 3,
            ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres")]
        [Display(Name = "Nombre del Funcionario")]
        public string NombreFuncionario { get; set; }

        [Required(ErrorMessage = "El cargo es obligatorio")]
        [StringLength(50, MinimumLength = 3,
            ErrorMessage = "El cargo debe tener entre 3 y 50 caracteres")]
        [Display(Name = "Cargo")]
        public string CargoFuncionario { get; set; }

        [Required(ErrorMessage = "El comentario es obligatorio")]
        [StringLength(2000, MinimumLength = 10,
            ErrorMessage = "El comentario debe tener entre 10 y 2000 caracteres")]
        [Display(Name = "Comentario u Observación")]
        public string TextoComentario { get; set; }

        [Display(Name = "Marcar como Aprobación")]
        public bool EsAprobacion { get; set; }

        [Display(Name = "Marcar como Rechazo")]
        public bool EsRechazo { get; set; }
    }

    /// <summary>
    /// ViewModel para búsqueda CON validación
    /// VERSIÓN SEGURA
    /// </summary>
    public class BusquedaSolicitudValidadaViewModel
    {
        [RegularExpression(@"^\d{9}$|^\d{10}$",
            ErrorMessage = "La cédula debe tener 9 o 10 dígitos")]
        [Display(Name = "Cédula del Propietario")]
        public string CedulaBuscada { get; set; }

        [RegularExpression(@"^[HPC]?\d{1}-\d{6}-\d{4}$",
            ErrorMessage = "Formato de plano inválido")]
        [Display(Name = "Plano Catastrado")]
        public string PlanoCatastrado { get; set; }

        [Display(Name = "Tipo de Construcción")]
        public TipoConstruccion? TipoConstruccion { get; set; }

        [Display(Name = "Estado")]
        public EstadoSolicitud? Estado { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Fecha Desde")]
        public DateTime? FechaDesde { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Fecha Hasta")]
        public DateTime? FechaHasta { get; set; }

        /// <summary>
        /// Validación personalizada para asegurar que FechaHasta >= FechaDesde
        /// </summary>
        public bool ValidarRangoFechas()
        {
            if (FechaDesde.HasValue && FechaHasta.HasValue)
            {
                return FechaHasta.Value >= FechaDesde.Value;
            }
            return true;
        }
    }
}
