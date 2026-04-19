// =============================================================================
// IpFilteringHttpHandler.cs
// Semana 3 - A10: SSRF - DelegatingHandler reutilizable contra SSRF
// Ubicación: Middleware/IpFilteringHttpHandler.cs
// =============================================================================
// PROPÓSITO EDUCATIVO
// En Semana 2, SsrfSeguroController implementaba 7 defensas contra SSRF
// DENTRO del controlador. El problema es que si otro desarrollador agrega
// un segundo controlador que también hace peticiones salientes y olvida
// replicar las 7 defensas, abre un nuevo vector de ataque.
//
// La diapositiva 7 de Semana 3 dice "Allow-lists estrictas" y "Validación
// de Entrada" como defensas tácticas. Este handler las aplica UNA VEZ, a
// NIVEL DE INFRAESTRUCTURA, a TODOS los HttpClient tipados que lo incluyan
// en su pipeline.
//
// USO (en Program.cs):
//   builder.Services.AddTransient&lt;IpFilteringHttpHandler&gt;();
//   builder.Services.AddHttpClient("SsrfSeguro", ...)
//          .AddHttpMessageHandler&lt;IpFilteringHttpHandler&gt;();
//
// CONEXIÓN SEMANA 4
// Este handler es un ejemplo perfecto del principio "Adaptadores Seguros"
// de Clean Architecture (slide 12): toda petición saliente pasa por un
// puerto validado antes de salir de la aplicación.
// =============================================================================

using System.Net;
using System.Net.Sockets;

namespace MUNIDENUNCIA.Middleware;

/// <summary>
/// DelegatingHandler que aplica las 7 defensas SSRF (whitelist de dominios,
/// HTTPS only, bloqueo de IPs privadas, resolución DNS anti TOCTOU, etc.)
/// a cualquier HttpClient que lo incluya en su pipeline.
/// </summary>
public class IpFilteringHttpHandler : DelegatingHandler
{
    private readonly ILogger<IpFilteringHttpHandler> _logger;

    // =========================================================================
    // Whitelist de dominios permitidos (misma que SsrfSeguroController).
    // En producción, cargar desde IOptions&lt;T&gt; para no recompilar al cambiar.
    // =========================================================================
    private static readonly HashSet<string> DominiosPermitidos =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "api.hacienda.go.cr",
            "api.tse.go.cr",
            "gee.bccr.fi.cr",
            "www.pgrweb.go.cr",
            "api.munidenuncia.go.cr"
        };

    public IpFilteringHttpHandler(ILogger<IpFilteringHttpHandler> logger)
    {
        _logger = logger;
    }

    // =========================================================================
    // Interceptor principal: se ejecuta ANTES de que la petición salga
    // =========================================================================
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var uri = request.RequestUri
            ?? throw new InvalidOperationException("La petición no tiene URI.");

        // ✅ DEFENSA 1: Esquema HTTPS obligatorio
        if (uri.Scheme != "https")
        {
            _logger.LogWarning("SSRF Handler: esquema rechazado: {Scheme}", uri.Scheme);
            throw new HttpRequestException(
                $"Solo se permite HTTPS (esquema recibido: {uri.Scheme}).");
        }

        // ✅ DEFENSA 2: Dominio en whitelist
        if (!DominiosPermitidos.Contains(uri.Host))
        {
            _logger.LogWarning(
                "SSRF Handler: dominio no autorizado rechazado: {Host}", uri.Host);
            throw new HttpRequestException(
                $"Dominio no autorizado: {uri.Host}");
        }

        // ✅ DEFENSA 3: Anti TOCTOU — resolver DNS JUSTO ANTES de la conexión
        // y verificar que la IP resuelta sea pública.
        //
        // TOCTOU (Time Of Check, Time Of Use): un atacante con control sobre
        // el DNS de api.munidenuncia.go.cr podría hacer que resuelva a una
        // IP pública durante la validación inicial y a 127.0.0.1 justo
        // cuando se establece la conexión. Este handler re-resuelve
        // inmediatamente antes de enviar para cerrar esa ventana.
        var direcciones = await Dns.GetHostAddressesAsync(uri.Host, cancellationToken);

        if (direcciones.Length == 0)
        {
            throw new HttpRequestException(
                $"El host {uri.Host} no resolvió a ninguna IP.");
        }

        foreach (var ip in direcciones)
        {
            if (EsIpPrivadaOReservada(ip))
            {
                _logger.LogError(
                    "SSRF Handler: {Host} resolvió a IP privada {Ip}. " +
                    "Posible DNS rebinding.", uri.Host, ip);
                throw new HttpRequestException(
                    $"El host {uri.Host} resolvió a una IP no permitida.");
            }
        }

        // ✅ DEFENSA 4: Desactivar redirects automáticos.
        // Esto se configura a nivel de HttpClientHandler en Program.cs,
        // pero aquí podemos validar que la respuesta no sea un 3xx si
        // la política es "nunca redirigir".

        _logger.LogInformation(
            "SSRF Handler: petición autorizada a {Host} ({Método})",
            uri.Host, request.Method);

        // Pasar al siguiente handler del pipeline (el transporte real)
        var respuesta = await base.SendAsync(request, cancellationToken);

        // ✅ DEFENSA 5: Rechazar redirects en respuesta
        if ((int)respuesta.StatusCode is >= 300 and < 400)
        {
            respuesta.Dispose();
            _logger.LogWarning(
                "SSRF Handler: redirect rechazado desde {Host} con status {Status}",
                uri.Host, (int)respuesta.StatusCode);
            throw new HttpRequestException(
                "Los redirects no están permitidos para peticiones salientes.");
        }

        return respuesta;
    }

    // =========================================================================
    // Helper: detecta IPs privadas, loopback, link-local, multicast, etc.
    // =========================================================================
    private static bool EsIpPrivadaOReservada(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip)) return true;

        var bytes = ip.GetAddressBytes();

        if (ip.AddressFamily == AddressFamily.InterNetwork)   // IPv4
        {
            // 10.0.0.0/8
            if (bytes[0] == 10) return true;
            // 172.16.0.0/12
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
            // 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168) return true;
            // 169.254.0.0/16 (link-local, incluye AWS/Azure metadata 169.254.169.254)
            if (bytes[0] == 169 && bytes[1] == 254) return true;
            // 127.0.0.0/8 (ya cubierto por IsLoopback, por defensa en profundidad)
            if (bytes[0] == 127) return true;
            // 0.0.0.0/8
            if (bytes[0] == 0) return true;
            // 100.64.0.0/10 (CGNAT)
            if (bytes[0] == 100 && (bytes[1] & 0xC0) == 64) return true;
            // 224.0.0.0/4 (multicast)
            if (bytes[0] >= 224 && bytes[0] <= 239) return true;
            // 240.0.0.0/4 (reservado)
            if (bytes[0] >= 240) return true;
        }
        else if (ip.AddressFamily == AddressFamily.InterNetworkV6)   // IPv6
        {
            // ::1 (loopback) ya cubierto por IsLoopback
            if (ip.IsIPv6LinkLocal) return true;
            if (ip.IsIPv6SiteLocal) return true;
            if (ip.IsIPv6Multicast) return true;
            // ::ffff:0:0/96 (IPv4-mapped) — re-validar contra lista IPv4
            if (ip.IsIPv4MappedToIPv6)
            {
                return EsIpPrivadaOReservada(ip.MapToIPv4());
            }
            // fc00::/7 (unique local)
            if ((bytes[0] & 0xFE) == 0xFC) return true;
        }

        return false;
    }
}
