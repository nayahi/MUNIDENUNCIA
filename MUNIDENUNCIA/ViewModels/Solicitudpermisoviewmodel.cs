using System;
using MUNIDENUNCIA.Models;

namespace MUNIDENUNCIA.ViewModels
{

    /// <summary>
    /// ViewModel para solicitud de permiso SIN validación
    /// VERSIÓN VULNERABLE - Usar solo para demostración de riesgos
    /// </summary>
    public class SolicitudPermisoViewModel
    {
        // Información del Propietario
        public string CedulaPropietario { get; set; }
        public string NombreCompletoPropietario { get; set; }
        public string EmailPropietario { get; set; }
        public string TelefonoPropietario { get; set; }

        // Información de la Propiedad
        public string Distrito { get; set; }
        public string DireccionCompleta { get; set; }
        public string PlanoCatastrado { get; set; }

        // Información del Proyecto
        public TipoConstruccion TipoConstruccion { get; set; }
        public decimal AreaConstruccionM2 { get; set; }
        public int NumeroPlantas { get; set; }
        public string DescripcionProyecto { get; set; }
        public decimal PresupuestoEstimado { get; set; }

        public SolicitudPermisoViewModel()
        {
            // Sin inicialización ni valores predeterminados
            // Esto permite enviar valores nulos o inválidos
        }
    }

    /// <summary>
    /// ViewModel para agregar comentarios SIN validación
    /// VERSIÓN VULNERABLE - Usar solo para demostración de XSS
    /// </summary>
    public class ComentarioViewModel
    {
        public int SolicitudPermisoId { get; set; }
        public string NombreFuncionario { get; set; }
        public string CargoFuncionario { get; set; }
        public string TextoComentario { get; set; }
        public bool EsAprobacion { get; set; }
        public bool EsRechazo { get; set; }
    }

    /// <summary>
    /// ViewModel para búsqueda SIN validación
    /// VERSIÓN VULNERABLE - Usado para demostrar SQL Injection
    /// </summary>
    public class BusquedaSolicitudViewModel
    {
        public string CedulaBuscada { get; set; }
        public string PlanoCatastrado { get; set; }
        public TipoConstruccion? TipoConstruccion { get; set; }
        public EstadoSolicitud? Estado { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
    }
}
