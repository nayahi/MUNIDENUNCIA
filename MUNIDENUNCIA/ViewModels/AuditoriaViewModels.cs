// =============================================================================
// AuditoriaViewModels.cs
// Semana 3 - A09: ViewModels para el dashboard de auditoría
// Ubicación: ViewModels/AuditoriaViewModels.cs
// =============================================================================

using MUNIDENUNCIA.Models;
using MUNIDENUNCIA.Services;

namespace MUNIDENUNCIA.ViewModels;

/// <summary>
/// ViewModel del dashboard principal (/Auditoria).
/// Combina alertas activas, estadísticas de 24h y eventos recientes.
/// </summary>
public class DashboardAuditoriaViewModel
{
    public List<Alerta> Alertas { get; set; } = new();

    public int TotalEventos24h    { get; set; }
    public int LoginsExitosos24h  { get; set; }
    public int LoginsFallidos24h  { get; set; }
    public int MfaFallidos24h     { get; set; }

    public List<TopEventoViewModel> TopEventos       { get; set; } = new();
    public List<AuditLog>           EventosRecientes { get; set; } = new();

    public int AlertasAltas  => Alertas.Count(a => a.Severidad == "alta");
    public int AlertasMedias => Alertas.Count(a => a.Severidad == "media");
    public int AlertasBajas  => Alertas.Count(a => a.Severidad == "baja");
}

/// <summary>Ranking de tipos de evento más frecuentes.</summary>
public class TopEventoViewModel
{
    public string Tipo  { get; set; } = string.Empty;
    public int    Total { get; set; }
}

/// <summary>
/// ViewModel de la vista de logs paginados (/Auditoria/Logs).
/// Mantiene los valores de filtros para re-renderizar el formulario.
/// </summary>
public class LogsAuditoriaViewModel
{
    public List<AuditLog> Eventos          { get; set; } = new();
    public int            Pagina           { get; set; } = 1;
    public int            TotalPaginas     { get; set; } = 1;
    public int            TotalEventos     { get; set; }
    public List<string>   TiposDisponibles { get; set; } = new();

    public string?   FiltroTipo    { get; set; }
    public string?   FiltroUsuario { get; set; }
    public string?   FiltroIp      { get; set; }
    public DateTime? FiltroDesde   { get; set; }
    public DateTime? FiltroHasta   { get; set; }
}
