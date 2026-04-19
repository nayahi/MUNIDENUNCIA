// =============================================================================
// SsrfToctouDemoController.cs
// Semana 3 - A10: Demostración de ataque TOCTOU y su mitigación
// Ubicación: Controllers/SsrfToctouDemoController.cs
// =============================================================================
// PROPÓSITO EDUCATIVO
// En Semana 2 vimos las 7 defensas SSRF estáticas. Esta demo muestra un
// ataque SUTIL que esas defensas NO cubren por completo: el TOCTOU DNS
// rebinding.
//
// EL ATAQUE TOCTOU (Time Of Check - Time Of Use)
//   1. Atacante registra dominio malicioso.munidenuncia.example
//   2. Atacante configura DNS con TTL=0 (caché deshabilitada)
//   3. Primera consulta DNS (check): devuelve IP pública inofensiva
//      que pasa la validación anti-IP-privada.
//   4. Segunda consulta DNS (use): atacante ha cambiado el registro,
//      ahora devuelve 127.0.0.1 o 169.254.169.254 (metadata AWS/Azure).
//   5. HttpClient se conecta a la IP maliciosa.
//
// MITIGACIÓN (lo que implementa IpFilteringHttpHandler)
//   - Resolver DNS UNA sola vez, justo antes de SendAsync
//   - Validar TODAS las IPs resueltas (algunas apps validan solo la primera)
//   - NO cachear — el atacante puede rotar entre consultas
//   - Rechazar redirects (que podrían llevar a un dominio no validado)
//
// NOTA: Este controller expone ambas versiones (vulnerable y segura) con
// fines pedagógicos. La vulnerable debe REMOVERSE antes de producción.
// =============================================================================

using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace MUNIDENUNCIA.Controllers;

[Route("[controller]")]
public class SsrfToctouDemoController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SsrfToctouDemoController> _logger;

    public SsrfToctouDemoController(
        IHttpClientFactory httpClientFactory,
        ILogger<SsrfToctouDemoController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index() => View();

    // =========================================================================
    // ⚠️ VERSIÓN VULNERABLE A TOCTOU ⚠️
    // Valida la IP antes de llamar, pero HttpClient resuelve DNS de nuevo
    // al conectar. Un atacante con control del DNS puede cambiar la IP
    // entre las dos resoluciones.
    // =========================================================================
    [HttpGet("vulnerable")]
    public async Task<IActionResult> VersionVulnerable([FromQuery] string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            ViewBag.Error = "Debe proporcionar un host.";
            return View("Resultado");
        }

        // ⚠️ CHECK: resolver DNS ahora
        IPAddress[] ips;
        try
        {
            ips = await Dns.GetHostAddressesAsync(host);
        }
        catch
        {
            ViewBag.Error = $"No se pudo resolver {host}.";
            return View("Resultado");
        }

        // ⚠️ Validar IP (parece seguro...)
        if (ips.Any(EsIpPrivada))
        {
            ViewBag.Error = "IP privada detectada.";
            return View("Resultado");
        }

        // ⚠️ USE: HttpClient resuelve DNS OTRA vez aquí.
        // Si el atacante cambió el registro DNS en esta ventana de tiempo
        // (típicamente pocos milisegundos), la IP real usada será distinta
        // a la validada arriba.
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(3);

        try
        {
            var url = $"https://{host}/";
            var response = await client.GetAsync(url);
            var body = await response.Content.ReadAsStringAsync();

            ViewBag.Resultado = $"[VULNERABLE a TOCTOU] Status: {response.StatusCode}";
            ViewBag.Contenido = body.Length > 500 ? body[..500] + "..." : body;
        }
        catch (Exception ex)
        {
            ViewBag.Error = $"Error al conectar: {ex.Message}";
        }

        return View("Resultado");
    }

    // =========================================================================
    // ✅ VERSIÓN SEGURA CONTRA TOCTOU ✅
    // Resuelve DNS UNA sola vez, fuerza HttpClient a usar esa IP exacta
    // (no re-resolver) usando SocketsHttpHandler con ConnectCallback.
    // =========================================================================
    [HttpGet("seguro")]
    public async Task<IActionResult> VersionSegura([FromQuery] string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            ViewBag.Error = "Debe proporcionar un host.";
            return View("Resultado");
        }

        // Resolver DNS UNA vez
        IPAddress[] ips;
        try
        {
            ips = await Dns.GetHostAddressesAsync(host);
        }
        catch
        {
            ViewBag.Error = $"No se pudo resolver {host}.";
            return View("Resultado");
        }

        // ✅ Validar TODAS las IPs (no solo la primera)
        var ipSegura = ips.FirstOrDefault(ip => !EsIpPrivada(ip));
        if (ipSegura is null)
        {
            ViewBag.Error = "Ninguna IP pública válida encontrada para este host.";
            return View("Resultado");
        }

        // ✅ Usar SocketsHttpHandler con ConnectCallback que FUERZA la conexión
        // a la IP ya validada, sin re-resolver DNS. Esto cierra la ventana TOCTOU.
        var handler = new SocketsHttpHandler
        {
            ConnectCallback = async (context, ct) =>
            {
                // Conectarnos a la IP específica que YA validamos
                var socket = new System.Net.Sockets.Socket(
                    System.Net.Sockets.SocketType.Stream,
                    System.Net.Sockets.ProtocolType.Tcp);
                socket.NoDelay = true;
                await socket.ConnectAsync(ipSegura, context.DnsEndPoint.Port, ct);
                return new System.Net.Sockets.NetworkStream(socket, ownsSocket: true);
            },
            AllowAutoRedirect = false,
            ConnectTimeout = TimeSpan.FromSeconds(3)
        };

        using var client = new HttpClient(handler, disposeHandler: true)
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        try
        {
            var url = $"https://{host}/";
            var response = await client.GetAsync(url);
            var body = await response.Content.ReadAsStringAsync();

            _logger.LogInformation(
                "SSRF Seguro (anti-TOCTOU): petición a {Host} conectó a IP {Ip}",
                host, ipSegura);

            ViewBag.Resultado = $"[SEGURO anti-TOCTOU] Conectado a IP {ipSegura}. " +
                                $"Status: {response.StatusCode}";
            ViewBag.Contenido = body.Length > 500 ? body[..500] + "..." : body;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("SSRF Seguro (anti-TOCTOU): error - {Msg}", ex.Message);
            ViewBag.Error = "No se pudo conectar al servicio.";
        }

        return View("Resultado");
    }

    // =========================================================================
    // Helper: detector básico de IPs privadas (el handler global tiene
    // la versión completa).
    // =========================================================================
    private static bool EsIpPrivada(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip)) return true;
        var b = ip.GetAddressBytes();
        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            if (b[0] == 10) return true;
            if (b[0] == 172 && b[1] >= 16 && b[1] <= 31) return true;
            if (b[0] == 192 && b[1] == 168) return true;
            if (b[0] == 169 && b[1] == 254) return true;
        }
        return false;
    }
}
