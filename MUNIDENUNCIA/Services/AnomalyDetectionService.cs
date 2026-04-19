// =============================================================================
// AnomalyDetectionService.cs
// Semana 3 - A09: Fallas en el Registro y Monitoreo
// Ubicación: Services/AnomalyDetectionService.cs
// =============================================================================
// PROPÓSITO EDUCATIVO
// La diapositiva 6 de Semana 3 contrasta "Puntos Ciegos" (datos incompletos,
// sin alertas) contra "Cobertura Total" (datos completos, umbrales definidos,
// alertas en tiempo real con alerta crítica marcada).
//
// Este servicio implementa las REGLAS de detección que convierten logs
// crudos (tabla AuditLogs) en ALERTAS accionables. Es el "cerebro" del
// dashboard de A09.
//
// REGLAS IMPLEMENTADAS
//   1. LoginFallidoMultiple         — 5+ LOGIN_FAILED desde una IP en 5 min
//   2. MfaFallidoMultiple           — 3+ MFA_FAILED de un usuario en 10 min
//   3. AccesoMasivoDenuncias        — ciudadano que ve 10+ denuncias en 1 hora
//   4. DesactivacionMfaReciente     — MFA_DISABLED en últimas 24h
//   5. UsoCodigoRespaldo            — MFA_SUCCESS_RECOVERY en últimas 24h
//
// Estas reglas son CONFIGURABLES pero simples por diseño: la clase es
// pedagógica, no un SIEM de producción. El mapa de ruta (slide 2) sitúa
// el SIEM real en Semana 4 (Arquitecturas Seguras / integración externa).
// =============================================================================

using Microsoft.EntityFrameworkCore;
using MUNIDENUNCIA.Data;
using MUNIDENUNCIA.Models;

namespace MUNIDENUNCIA.Services;

public record Alerta(
    string Severidad,        // "alta", "media", "baja"
    string Tipo,
    string Descripcion,
    DateTime DetectadaEn,
    int OcurrenciasRelacionadas,
    string? IpOrigen = null,
    string? UsuarioRelacionado = null);

public class AnomalyDetectionService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AnomalyDetectionService> _logger;

    public AnomalyDetectionService(
        ApplicationDbContext db,
        ILogger<AnomalyDetectionService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // =========================================================================
    // Método principal: ejecuta todas las reglas y devuelve alertas
    // =========================================================================
    public async Task<List<Alerta>> DetectarAnomalias(CancellationToken ct = default)
    {
        var alertas = new List<Alerta>();
        var ahora = DateTime.UtcNow;

        alertas.AddRange(await ReglaLoginFallidoMultiple(ahora, ct));
        alertas.AddRange(await ReglaMfaFallidoMultiple(ahora, ct));
        alertas.AddRange(await ReglaAccesoMasivoDenuncias(ahora, ct));
        alertas.AddRange(await ReglaDesactivacionMfaReciente(ahora, ct));
        alertas.AddRange(await ReglaUsoCodigoRespaldo(ahora, ct));

        if (alertas.Count > 0)
        {
            _logger.LogWarning(
                "Detección de anomalías: {Total} alertas activas", alertas.Count);
        }

        return alertas
            .OrderBy(a => a.Severidad switch { "alta" => 0, "media" => 1, _ => 2 })
            .ThenByDescending(a => a.DetectadaEn)
            .ToList();
    }

    // =========================================================================
    // Regla 1: 5+ LOGIN_FAILED desde una misma IP en ventana de 5 minutos
    // =========================================================================
    private async Task<IEnumerable<Alerta>> ReglaLoginFallidoMultiple(
        DateTime ahora, CancellationToken ct)
    {
        var desde = ahora.AddMinutes(-5);

        var grupos = await _db.AuditLogs
            .Where(a => a.EventType == "LOGIN_FAILED" && a.Timestamp >= desde)
            .GroupBy(a => a.IpAddress)
            .Where(g => g.Count() >= 5)
            .Select(g => new { Ip = g.Key, Total = g.Count() })
            .ToListAsync(ct);

        return grupos.Select(g => new Alerta(
            Severidad: "alta",
            Tipo: "LoginFallidoMultiple",
            Descripcion: $"5 o más intentos fallidos de login desde la IP {g.Ip} en 5 min. Posible fuerza bruta.",
            DetectadaEn: ahora,
            OcurrenciasRelacionadas: g.Total,
            IpOrigen: g.Ip));
    }

    // =========================================================================
    // Regla 2: 3+ MFA_FAILED de un mismo usuario en 10 min
    // =========================================================================
    private async Task<IEnumerable<Alerta>> ReglaMfaFallidoMultiple(
        DateTime ahora, CancellationToken ct)
    {
        var desde = ahora.AddMinutes(-10);

        var grupos = await _db.AuditLogs
            .Where(a => a.EventType == "MFA_FAILED" && a.Timestamp >= desde)
            .GroupBy(a => a.UserId)
            .Where(g => g.Count() >= 3)
            .Select(g => new { Usuario = g.Key, Total = g.Count() })
            .ToListAsync(ct);

        return grupos.Select(g => new Alerta(
            Severidad: "alta",
            Tipo: "MfaFallidoMultiple",
            Descripcion: $"El usuario {g.Usuario} falló el segundo factor 3+ veces en 10 min. Posible robo de credenciales.",
            DetectadaEn: ahora,
            OcurrenciasRelacionadas: g.Total,
            UsuarioRelacionado: g.Usuario));
    }

    // =========================================================================
    // Regla 3: Ciudadano accediendo a 10+ denuncias en 1 hora
    // =========================================================================
    private async Task<IEnumerable<Alerta>> ReglaAccesoMasivoDenuncias(
        DateTime ahora, CancellationToken ct)
    {
        var desde = ahora.AddHours(-1);

        var grupos = await _db.AuditLogs
            .Where(a => a.EventType == "DENUNCIA_VIEW" && a.Timestamp >= desde)
            .GroupBy(a => a.UserId)
            .Where(g => g.Count() >= 10)
            .Select(g => new { Usuario = g.Key, Total = g.Count() })
            .ToListAsync(ct);

        return grupos.Select(g => new Alerta(
            Severidad: "media",
            Tipo: "AccesoMasivoDenuncias",
            Descripcion: $"El usuario {g.Usuario} accedió a {g.Total} denuncias en 1 hora. Revisar si corresponde a su rol.",
            DetectadaEn: ahora,
            OcurrenciasRelacionadas: g.Total,
            UsuarioRelacionado: g.Usuario));
    }

    // =========================================================================
    // Regla 4: MFA desactivado en últimas 24h (evento poco común)
    // =========================================================================
    private async Task<IEnumerable<Alerta>> ReglaDesactivacionMfaReciente(
        DateTime ahora, CancellationToken ct)
    {
        var desde = ahora.AddHours(-24);

        var eventos = await _db.AuditLogs
            .Where(a => a.EventType == "MFA_DISABLED" && a.Timestamp >= desde)
            .Select(a => new { a.UserId, a.Timestamp, a.IpAddress })
            .ToListAsync(ct);

        return eventos.Select(e => new Alerta(
            Severidad: "media",
            Tipo: "DesactivacionMfaReciente",
            Descripcion: $"El usuario {e.UserId} desactivó su MFA. Verificar que haya sido acción legítima.",
            DetectadaEn: e.Timestamp,
            OcurrenciasRelacionadas: 1,
            IpOrigen: e.IpAddress,
            UsuarioRelacionado: e.UserId));
    }

    // =========================================================================
    // Regla 5: Uso de código de respaldo en últimas 24h
    // =========================================================================
    private async Task<IEnumerable<Alerta>> ReglaUsoCodigoRespaldo(
        DateTime ahora, CancellationToken ct)
    {
        var desde = ahora.AddHours(-24);

        var eventos = await _db.AuditLogs
            .Where(a => a.EventType == "MFA_SUCCESS_RECOVERY" && a.Timestamp >= desde)
            .Select(a => new { a.UserId, a.Timestamp, a.IpAddress })
            .ToListAsync(ct);

        return eventos.Select(e => new Alerta(
            Severidad: "baja",
            Tipo: "UsoCodigoRespaldo",
            Descripcion: $"El usuario {e.UserId} usó un código de respaldo. Recomendarle regenerar los códigos.",
            DetectadaEn: e.Timestamp,
            OcurrenciasRelacionadas: 1,
            IpOrigen: e.IpAddress,
            UsuarioRelacionado: e.UserId));
    }
}
