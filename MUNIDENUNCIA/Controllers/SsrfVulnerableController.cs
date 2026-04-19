// =============================================================================
// SsrfVulnerableController.cs
// Semana 2 - A10: Falsificación de Solicitudes del Lado del Servidor (SSRF)
// Ubicación: Controllers/SsrfVulnerableController.cs
// =============================================================================
// PROPÓSITO EDUCATIVO: Este controlador muestra un endpoint VULNERABLE a SSRF.
// NUNCA usar en producción.
// =============================================================================
// CONTEXTO MUNICIPAL: Muchos sistemas de gobierno costarricense integran
// servicios externos (TSE para validar cédulas, Hacienda para consultar
// estados tributarios, SINPE para pagos). Si estas integraciones aceptan
// URLs del usuario sin validar, un atacante puede usar el servidor de la
// municipalidad como proxy para acceder a la red interna.
// =============================================================================

using Microsoft.AspNetCore.Mvc;

namespace MUNIDENUNCIA.Controllers;

/// <summary>
/// Controlador VULNERABLE a SSRF (OWASP A10).
/// Acepta una URL del usuario y realiza una petición HTTP sin validar
/// el destino, permitiendo al atacante acceder a recursos internos.
/// </summary>
[Route("[controller]")]
public class SsrfVulnerableController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SsrfVulnerableController> _logger;

    public SsrfVulnerableController(
        IHttpClientFactory httpClientFactory,
        ILogger<SsrfVulnerableController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    // =========================================================================
    // ENDPOINT VULNERABLE: Proxy abierto que acepta cualquier URL
    // =========================================================================
    // Escenario: "Vista previa de enlace" — el usuario pega una URL y el
    // servidor la descarga para mostrar un preview (como hace Slack, Teams, etc.)
    //
    // Ataque: El atacante envía URLs como:
    //   - http://localhost:5000/admin/config    → accede al panel admin interno
    //   - http://169.254.169.254/latest/meta-data → metadata de cloud (AWS/Azure)
    //   - http://192.168.1.1/admin              → router/firewall interno
    //   - http://10.0.0.5:1433                  → escaneo de puertos internos
    //   - file:///etc/passwd                    → archivos locales del servidor
    // =========================================================================

    /// <summary>
    /// INSEGURO: Descarga contenido de cualquier URL proporcionada por el usuario.
    /// No valida el dominio, esquema, ni destino de la petición.
    /// </summary>
    [HttpGet("preview")]
    public async Task<IActionResult> VistaPrevia([FromQuery] string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            ViewBag.Error = "Debe proporcionar una URL.";
            return View("Resultado");
        }

        try
        {
            // ⚠️ VULNERABLE: No valida nada sobre la URL
            // El servidor actúa como proxy abierto
            var client = _httpClientFactory.CreateClient();

            _logger.LogInformation(
                "SSRF VULNERABLE: Realizando petición a URL del usuario: {Url}", url);

            // ⚠️ VULNERABLE: Petición directa sin ninguna restricción
            var response = await client.GetAsync(url);
            var contenido = await response.Content.ReadAsStringAsync();

            // Limitar contenido mostrado para la demo
            ViewBag.Url = url;
            ViewBag.StatusCode = (int)response.StatusCode;
            ViewBag.Contenido = contenido.Length > 2000
                ? contenido[..2000] + "\n\n[... contenido truncado ...]"
                : contenido;
            ViewBag.Headers = response.Headers.ToString();

            return View("Resultado");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "SSRF VULNERABLE: Error al acceder a {Url}", url);

            ViewBag.Url = url;
            ViewBag.Error = $"Error al acceder a la URL: {ex.Message}";
            return View("Resultado");
        }
    }

    // =========================================================================
    // ENDPOINT VULNERABLE 2: Verificación de estado de servicio externo
    // =========================================================================
    // Escenario: "Health check" de un servicio externo que la municipalidad
    // necesita monitorear (ej: API del TSE, Hacienda, BCCR).
    //
    // Ataque: El atacante puede cambiar la URL del health check para
    // escanear la red interna de la municipalidad.
    // =========================================================================

    /// <summary>
    /// INSEGURO: Verifica si un servicio externo responde, pero acepta
    /// cualquier URL sin restricción.
    /// </summary>
    [HttpGet("health-check")]
    public async Task<IActionResult> HealthCheck([FromQuery] string serviceUrl)
    {
        if (string.IsNullOrWhiteSpace(serviceUrl))
        {
            return Json(new { status = "error", message = "URL requerida" });
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            // ⚠️ VULNERABLE: timeout largo permite escaneo de puertos
            client.Timeout = TimeSpan.FromSeconds(10);

            var response = await client.GetAsync(serviceUrl);

            // ⚠️ VULNERABLE: Expone información del servicio interno al atacante
            return Json(new
            {
                status = "ok",
                url = serviceUrl,
                statusCode = (int)response.StatusCode,
                responseTime = "< 1s",
                serverHeader = response.Headers.Server?.ToString() ?? "N/A"
            });
        }
        catch (Exception ex)
        {
            // ⚠️ VULNERABLE: El mensaje de error revela información sobre la red
            // Un atacante puede determinar si un host/puerto existe basándose
            // en el tipo de error: "Connection refused" vs "Timeout" vs "Name resolution"
            return Json(new
            {
                status = "error",
                url = serviceUrl,
                error = ex.Message // ← Fuga de información de red interna
            });
        }
    }
}
