// =============================================================================
// SsrfSeguroController.cs
// Semana 2 - A10: Falsificación de Solicitudes del Lado del Servidor (SSRF)
// Ubicación: Controllers/SsrfSeguroController.cs
// =============================================================================
// PROPÓSITO EDUCATIVO: Este controlador muestra las defensas contra SSRF
// usando whitelist de dominios, validación de esquema, y bloqueo de IPs
// privadas/reservadas.
// =============================================================================

using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Mvc;

namespace MUNIDENUNCIA.Controllers;

/// <summary>
/// Controlador SEGURO contra SSRF (OWASP A10).
/// Implementa: whitelist de dominios, validación de esquema HTTPS,
/// bloqueo de IPs privadas/reservadas, y timeouts restrictivos.
/// </summary>
[Route("[controller]")]
public class SsrfSeguroController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SsrfSeguroController> _logger;

    // =========================================================================
    // ✅ DEFENSA 1: Lista blanca de dominios permitidos
    // Solo se pueden hacer peticiones a servicios conocidos y aprobados.
    // En contexto municipal CR, estos serían los servicios de gobierno.
    // =========================================================================
    private static readonly Dictionary<string, string> ServiciosPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        ["api.hacienda.go.cr"] = "Ministerio de Hacienda - Facturación Electrónica",
        ["api.tse.go.cr"] = "Tribunal Supremo de Elecciones - Padrón Electoral",
        ["gee.bccr.fi.cr"] = "Banco Central - Tipo de Cambio",
        ["www.pgrweb.go.cr"] = "Procuraduría General - SINALEVI",
        ["api.munidenuncia.go.cr"] = "MuniDenuncia - Servicios Internos (demo)"
    };

    public SsrfSeguroController(
        IHttpClientFactory httpClientFactory,
        ILogger<SsrfSeguroController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        ViewBag.ServiciosPermitidos = ServiciosPermitidos;
        return View();
    }

    // =========================================================================
    // ENDPOINT SEGURO: Vista previa con múltiples capas de validación
    // =========================================================================

    /// <summary>
    /// SEGURO: Descarga contenido únicamente de dominios en la whitelist,
    /// valida el esquema (HTTPS), bloquea IPs privadas, y limita el tamaño
    /// de la respuesta.
    /// </summary>
    [HttpGet("preview")]
    public async Task<IActionResult> VistaPrevia([FromQuery] string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            ViewBag.Error = "Debe proporcionar una URL.";
            return View("Resultado");
        }

        // ✅ DEFENSA 2: Validar que sea una URL bien formada con esquema HTTPS
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            _logger.LogWarning("SSRF Defensa: URL malformada rechazada: {Url}", url);
            ViewBag.Error = "La URL proporcionada no es válida.";
            return View("Resultado");
        }

        if (uri.Scheme != "https")
        {
            _logger.LogWarning(
                "SSRF Defensa: Esquema no-HTTPS rechazado: {Scheme}://{Host}",
                uri.Scheme, uri.Host);
            ViewBag.Error = "Solo se permiten URLs con HTTPS.";
            return View("Resultado");
        }

        // ✅ DEFENSA 3: Verificar que el dominio esté en la whitelist
        if (!ServiciosPermitidos.ContainsKey(uri.Host))
        {
            _logger.LogWarning(
                "SSRF Defensa: Dominio no autorizado rechazado: {Host}", uri.Host);
            ViewBag.Error = $"El dominio '{uri.Host}' no está en la lista de " +
                "servicios autorizados. Dominios permitidos: " +
                string.Join(", ", ServiciosPermitidos.Keys);
            return View("Resultado");
        }

        // ✅ DEFENSA 4: Resolver DNS y bloquear IPs privadas/reservadas
        // Previene ataques de DNS rebinding donde un dominio de whitelist
        // resuelve temporalmente a una IP interna
        if (!await EsDireccionPublicaSegura(uri.Host))
        {
            _logger.LogError(
                "SSRF Defensa: El dominio {Host} resuelve a una IP privada/reservada. " +
                "Posible ataque de DNS rebinding.", uri.Host);
            ViewBag.Error = "La dirección IP del servicio no es válida para acceso externo.";
            return View("Resultado");
        }

        try
        {
            // ✅ DEFENSA 5: Usar HttpClient con timeout restrictivo
            var client = _httpClientFactory.CreateClient("SsrfSeguro");

            _logger.LogInformation(
                "SSRF Seguro: Petición autorizada a {Host} ({Servicio})",
                uri.Host, ServiciosPermitidos[uri.Host]);

            var response = await client.GetAsync(uri);
            var contenido = await response.Content.ReadAsStringAsync();

            // ✅ DEFENSA 6: Limitar tamaño de respuesta mostrada
            const int maxContenido = 1000;
            ViewBag.Url = url;
            ViewBag.Servicio = ServiciosPermitidos[uri.Host];
            ViewBag.StatusCode = (int)response.StatusCode;
            ViewBag.Contenido = contenido.Length > maxContenido
                ? contenido[..maxContenido] + "\n[... truncado por seguridad ...]"
                : contenido;

            return View("Resultado");
        }
        catch (TaskCanceledException)
        {
            // ✅ DEFENSA 7: Mensajes de error genéricos — no revelan red interna
            _logger.LogWarning("SSRF Seguro: Timeout al conectar con {Host}", uri.Host);
            ViewBag.Error = "El servicio no respondió en el tiempo permitido.";
            return View("Resultado");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SSRF Seguro: Error al conectar con {Host}", uri.Host);
            // ✅ NO exponer ex.Message al usuario — podría revelar detalles de red
            ViewBag.Error = "No se pudo conectar con el servicio solicitado.";
            return View("Resultado");
        }
    }

    // =========================================================================
    // ENDPOINT SEGURO 2: Health check restringido
    // =========================================================================

    /// <summary>
    /// SEGURO: Health check que solo permite verificar servicios de la whitelist.
    /// </summary>
    [HttpGet("health-check")]
    public async Task<IActionResult> HealthCheck([FromQuery] string serviceUrl)
    {
        if (string.IsNullOrWhiteSpace(serviceUrl)
            || !Uri.TryCreate(serviceUrl, UriKind.Absolute, out var uri)
            || uri.Scheme != "https"
            || !ServiciosPermitidos.ContainsKey(uri.Host))
        {
            return Json(new
            {
                status = "rechazado",
                message = "Solo se permite verificar servicios de la lista autorizada.",
                serviciosPermitidos = ServiciosPermitidos.Keys
            });
        }

        try
        {
            var client = _httpClientFactory.CreateClient("SsrfSeguro");
            var response = await client.GetAsync(uri);

            return Json(new
            {
                status = "ok",
                servicio = ServiciosPermitidos[uri.Host],
                statusCode = (int)response.StatusCode
                // ✅ NO expone headers del servidor ni detalles técnicos
            });
        }
        catch
        {
            // ✅ Respuesta genérica — no revela tipo de error de red
            return Json(new
            {
                status = "no_disponible",
                servicio = ServiciosPermitidos[uri.Host],
                message = "El servicio no respondió."
            });
        }
    }

    // =========================================================================
    // Utilidad: Verificación de IP pública (anti DNS-rebinding)
    // =========================================================================

    /// <summary>
    /// Resuelve el hostname a IP y verifica que NO sea una dirección
    /// privada, loopback, link-local, o reservada.
    /// Previene ataques de DNS rebinding.
    /// </summary>
    private static async Task<bool> EsDireccionPublicaSegura(string hostname)
    {
        try
        {
            var addresses = await Dns.GetHostAddressesAsync(hostname);

            foreach (var ip in addresses)
            {
                // Bloquear loopback (127.0.0.0/8, ::1)
                if (IPAddress.IsLoopback(ip))
                    return false;

                // Bloquear IPv6 link-local (fe80::/10)
                if (ip.AddressFamily == AddressFamily.InterNetworkV6
                    && ip.IsIPv6LinkLocal)
                    return false;

                // Bloquear rangos privados IPv4
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    var bytes = ip.GetAddressBytes();

                    // 10.0.0.0/8
                    if (bytes[0] == 10) return false;

                    // 172.16.0.0/12
                    if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return false;

                    // 192.168.0.0/16
                    if (bytes[0] == 192 && bytes[1] == 168) return false;

                    // 169.254.0.0/16 (link-local / cloud metadata)
                    if (bytes[0] == 169 && bytes[1] == 254) return false;

                    // 0.0.0.0
                    if (bytes[0] == 0) return false;
                }
            }

            return true;
        }
        catch
        {
            // Si no se puede resolver DNS, rechazar por seguridad
            return false;
        }
    }
}
