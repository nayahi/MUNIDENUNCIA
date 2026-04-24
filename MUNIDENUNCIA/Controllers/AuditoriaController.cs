// =============================================================================
// AuditoriaController.cs
// Semana 3 - A09: Dashboard de auditoría con filtros y alertas
// Ubicación: Controllers/AuditoriaController.cs
// =============================================================================
// PROPÓSITO EDUCATIVO
// Este controller es la contraparte VISUAL del Serilog + tabla AuditLogs
// que ya existe en MuniDenuncia desde Nivel 1. Traduce "Anatomía de un Log
// Seguro" (diapositiva 6) en una interfaz que el funcionario municipal
// puede usar diariamente:
//   - GET /Auditoria               → dashboard con alertas + últimos eventos
//   - GET /Auditoria/Logs          → listado paginado con filtros
//   - GET /Auditoria/Exportar      → exporta CSV (firmado con A08!)
//
// AUTORIZACIÓN
// El dashboard es INFORMACIÓN SENSIBLE: muestra IPs, usuarios con fallos,
// patrones de acceso. Solo el rol Admin (y opcionalmente Auditor si se crea
// como rol separado) debe tener acceso.
//
// CONEXIÓN LEY 8968
// Art. 10 exige registro de accesos. Art. 17 obliga a responder en 5 días
// a solicitudes de acceso del titular — para cumplirlo, la municipalidad
// debe poder buscar rápidamente "todos los eventos del usuario X en el
// período Y", que es exactamente lo que este dashboard habilita.
// =============================================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MUNIDENUNCIA.Data;
using MUNIDENUNCIA.Models;
using MUNIDENUNCIA.Services;
using MUNIDENUNCIA.ViewModels;

namespace MUNIDENUNCIA.Controllers;

// ✅ Usa la política centralizada creada en Semana 2 (AuthorizationPolicies)
[Authorize(Policy = "RequiereAdministrador")]
[Route("[controller]")]
public class AuditoriaController : Controller
{
    private readonly ApplicationDbContext        _db;
    private readonly AnomalyDetectionService     _anomalias;
    private readonly FirmaDigitalService         _firmaService;
    private readonly ILogger<AuditoriaController> _logger;

    private const int TamanioPagina = 50;

    public AuditoriaController(
        ApplicationDbContext        db,
        AnomalyDetectionService     anomalias,
        FirmaDigitalService         firmaService,
        ILogger<AuditoriaController> logger)
    {
        _db           = db;
        _anomalias    = anomalias;
        _firmaService = firmaService;
        _logger       = logger;
    }

    // =========================================================================
    // GET /Auditoria — Dashboard con alertas y resumen
    // =========================================================================
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var ahora = DateTime.UtcNow;
        var hace24h = ahora.AddHours(-24);

        var modelo = new DashboardAuditoriaViewModel
        {
            Alertas = await _anomalias.DetectarAnomalias(ct),

            // Estadísticas de las últimas 24 horas
            TotalEventos24h = await _db.AuditLogs
                .CountAsync(a => a.Timestamp >= hace24h, ct),

            LoginsExitosos24h = await _db.AuditLogs
                .CountAsync(a => a.Timestamp >= hace24h
                              && a.EventType == "LOGIN_SUCCESS", ct),

            LoginsFallidos24h = await _db.AuditLogs
                .CountAsync(a => a.Timestamp >= hace24h
                              && a.EventType == "LOGIN_FAILED", ct),

            MfaFallidos24h = await _db.AuditLogs
                .CountAsync(a => a.Timestamp >= hace24h
                              && a.EventType == "MFA_FAILED", ct),

            // Top 5 eventos de las últimas 24h
            TopEventos = await _db.AuditLogs
                .Where(a => a.Timestamp >= hace24h)
                .GroupBy(a => a.EventType)
                .Select(g => new TopEventoViewModel
                {
                    Tipo   = g.Key,
                    Total  = g.Count()
                })
                .OrderByDescending(t => t.Total)
                .Take(5)
                .ToListAsync(ct),

            // Últimos 20 eventos para la vista en vivo
            EventosRecientes = await _db.AuditLogs
                .OrderByDescending(a => a.Timestamp)
                .Take(20)
                .ToListAsync(ct)
        };

        return View(modelo);
    }

    // =========================================================================
    // GET /Auditoria/Logs — Listado filtrable y paginado
    // =========================================================================
    [HttpGet("Logs")]
    public async Task<IActionResult> Logs(
        string? tipo,
        string? usuario,
        string? ip,
        DateTime? desde,
        DateTime? hasta,
        int pagina = 1,
        CancellationToken ct = default)
    {
        if (pagina < 1) pagina = 1;

        // El input datetime-local envía tiempo sin zona. Marcamos explícitamente
        // como UTC para que la comparación contra AuditLog.Timestamp sea correcta.
        if (desde.HasValue) desde = DateTime.SpecifyKind(desde.Value, DateTimeKind.Utc);
        if (hasta.HasValue) hasta = DateTime.SpecifyKind(hasta.Value, DateTimeKind.Utc);

        var query = _db.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(tipo))
            query = query.Where(a => a.EventType == tipo);

        if (!string.IsNullOrWhiteSpace(usuario))
            query = query.Where(a => a.UserId != null && a.UserId.Contains(usuario));

        if (!string.IsNullOrWhiteSpace(ip))
            query = query.Where(a => a.IpAddress == ip);

        if (desde.HasValue)
            query = query.Where(a => a.Timestamp >= desde.Value);

        if (hasta.HasValue)
            query = query.Where(a => a.Timestamp <= hasta.Value);

        var total = await query.CountAsync(ct);

        var eventos = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((pagina - 1) * TamanioPagina)
            .Take(TamanioPagina)
            .ToListAsync(ct);

        var tiposDisponibles = await _db.AuditLogs
            .Select(a => a.EventType)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync(ct);

        var modelo = new LogsAuditoriaViewModel
        {
            Eventos           = eventos,
            Pagina            = pagina,
            TotalPaginas      = (int)Math.Ceiling(total / (double)TamanioPagina),
            TotalEventos      = total,
            TiposDisponibles  = tiposDisponibles,
            FiltroTipo        = tipo,
            FiltroUsuario     = usuario,
            FiltroIp          = ip,
            FiltroDesde       = desde,
            FiltroHasta       = hasta
        };

        return View(modelo);
    }

    // =========================================================================
    // GET /Auditoria/Exportar — Exporta CSV FIRMADO digitalmente
    // Aquí se conecta A09 (el qué se exporta) con A08 (la firma de lo exportado)
    // =========================================================================
    [HttpGet("Exportar")]
    public async Task<IActionResult> Exportar(
        DateTime? desde, DateTime? hasta, CancellationToken ct)
    {
        desde ??= DateTime.UtcNow.AddDays(-7);
        hasta ??= DateTime.UtcNow;
        desde = DateTime.SpecifyKind(desde.Value, DateTimeKind.Utc);
        hasta = DateTime.SpecifyKind(hasta.Value, DateTimeKind.Utc);

        const int MaxExportRows = 10_000;
        var eventos = await _db.AuditLogs
            .Where(a => a.Timestamp >= desde && a.Timestamp <= hasta)
            .OrderBy(a => a.Timestamp)
            .Take(MaxExportRows)
            .ToListAsync(ct);

        // Construir CSV
        using var ms = new MemoryStream();
        using (var writer = new StreamWriter(ms, leaveOpen: true))
        {
            writer.WriteLine("Timestamp,EventType,UserId,IpAddress,Success,Description");
            foreach (var e in eventos)
            {
                writer.WriteLine(
                    $"{e.Timestamp:O}," +
                    $"{Escapar(e.EventType)}," +
                    $"{Escapar(e.UserId)}," +
                    $"{Escapar(e.IpAddress)}," +
                    $"{e.Success}," +
                    $"{Escapar(e.Description)}");
            }
        }
        var csvBytes = ms.ToArray();

        // Firmar el CSV (A08)
        var firma = _firmaService.Firmar(csvBytes);

        // Empaquetar en ZIP
        using var zipMs = new MemoryStream();
        using (var zip = new System.IO.Compression.ZipArchive(
            zipMs, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
        {
            var eCsv = zip.CreateEntry("auditoria.csv");
            using (var s = eCsv.Open()) s.Write(csvBytes);

            var eSig = zip.CreateEntry("auditoria.csv.sig");
            using (var s = eSig.Open()) s.Write(firma);
        }

        _logger.LogInformation(
            "Exportación de auditoría solicitada por {User}. Rango: {Desde} a {Hasta}. Eventos: {Count}.",
            User.Identity?.Name, desde, hasta, eventos.Count);

        return File(zipMs.ToArray(), "application/zip",
            $"auditoria-{desde:yyyyMMdd}-a-{hasta:yyyyMMdd}.zip");
    }

    // =========================================================================
    // Helper privado para CSV escaping (RFC 4180)
    // =========================================================================
    private static string Escapar(string? valor)
    {
        if (string.IsNullOrEmpty(valor)) return "";
        if (valor.Contains(',') || valor.Contains('"') || valor.Contains('\n'))
        {
            return "\"" + valor.Replace("\"", "\"\"") + "\"";
        }
        return valor;
    }
}
