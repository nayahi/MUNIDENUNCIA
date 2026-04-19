// =============================================================================
// IntegridadSeguroController.cs
// Semana 2 - A08: Fallas en el Software y en la Integridad de los Datos
// Ubicación: Controllers/IntegridadSeguroController.cs
// =============================================================================
// PROPÓSITO EDUCATIVO: Este controlador muestra las prácticas SEGURAS de
// deserialización y verificación de integridad que mitigan OWASP A08.
// =============================================================================

using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace MUNIDENUNCIA.Controllers;

// =========================================================================
// Modelo fuertemente tipado para configuración — solo acepta campos conocidos
// =========================================================================
public class ConfiguracionUsuarioDto
{
    [Required]
    [RegularExpression(@"^(claro|oscuro)$", ErrorMessage = "Tema debe ser 'claro' u 'oscuro'.")]
    public string Tema { get; set; } = "claro";

    [Required]
    [RegularExpression(@"^(es|en)$", ErrorMessage = "Idioma debe ser 'es' o 'en'.")]
    public string Idioma { get; set; } = "es";

    [Range(10, 100, ErrorMessage = "Registros por página debe estar entre 10 y 100.")]
    public int RegistrosPorPagina { get; set; } = 25;
}

// =========================================================================
// Modelo para verificación de integridad de actualizaciones
// =========================================================================
public class ActualizacionVerificadaDto
{
    [Required]
    [Url]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Hash SHA-256 esperado del contenido, en formato hexadecimal.
    /// El proveedor publica este hash en un canal separado y seguro.
    /// </summary>
    [Required]
    [RegularExpression(@"^[a-fA-F0-9]{64}$", ErrorMessage = "Hash SHA-256 inválido.")]
    public string HashEsperado { get; set; } = string.Empty;
}

/// <summary>
/// Controlador SEGURO que demuestra las defensas contra fallas de integridad (OWASP A08).
/// Incluye: deserialización tipada, validación de esquema, y verificación de integridad.
/// </summary>
[Route("[controller]")]
public class IntegridadSeguroController : Controller
{
    private readonly ILogger<IntegridadSeguroController> _logger;

    // Lista blanca de dominios confiables para actualizaciones
    private static readonly HashSet<string> DominiosConfiables = new(StringComparer.OrdinalIgnoreCase)
    {
        "updates.munidenuncia.go.cr",
        "nuget.org",
        "github.com"
    };

    public IntegridadSeguroController(ILogger<IntegridadSeguroController> logger)
    {
        _logger = logger;
    }

    // =========================================================================
    // DEMO 1: Página principal
    // =========================================================================
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    // =========================================================================
    // DEMO 2: Deserialización segura con System.Text.Json y modelo tipado
    // =========================================================================
    // DEFENSA: Usar System.Text.Json (seguro por defecto) con un DTO
    // fuertemente tipado que solo acepta propiedades conocidas.
    // System.Text.Json NO soporta TypeNameHandling, eliminando el vector
    // de ataque de instanciación arbitraria de tipos.
    //
    // Ley 8968 Art. 10: Medidas técnicas para proteger datos — la
    // deserialización controlada evita que un atacante manipule los datos
    // del sistema de la municipalidad.
    // =========================================================================

    /// <summary>
    /// SEGURO: Deserializa JSON usando un modelo fuertemente tipado.
    /// System.Text.Json ignora propiedades desconocidas por defecto
    /// y no permite instanciar tipos arbitrarios.
    /// </summary>
    [HttpPost("importar-configuracion")]
    public IActionResult ImportarConfiguracion([FromBody] ConfiguracionUsuarioDto? configuracion)
    {
        // ✅ SEGURO: El model binding de ASP.NET Core + System.Text.Json:
        // 1. Solo acepta las propiedades definidas en ConfiguracionUsuarioDto
        // 2. Aplica Data Annotations ([Required], [Range], [RegularExpression])
        // 3. Ignora propiedades desconocidas (ej: "rol", "permisos")
        // 4. No permite $type ni instanciación de tipos arbitrarios

        if (!ModelState.IsValid)
        {
            _logger.LogWarning(
                "A08 Defensa: Configuración rechazada por validación. Errores: {Errores}",
                string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)));

            ViewBag.Resultado = "RECHAZADO";
            ViewBag.Mensaje = "La configuración fue rechazada porque no cumple " +
                "con el esquema esperado. Campos inyectados como 'rol' o 'permisos' " +
                "fueron ignorados automáticamente.";
            return View("ResultadoImportacion");
        }

        // Solo los campos válidos y conocidos llegan aquí
        _logger.LogInformation(
            "A08 Defensa: Configuración válida aplicada. Tema={Tema}, Idioma={Idioma}, " +
            "Registros={Registros}",
            configuracion!.Tema, configuracion.Idioma, configuracion.RegistrosPorPagina);

        ViewBag.Resultado = "IMPORTADO_SEGURO";
        ViewBag.Datos = JsonSerializer.Serialize(configuracion, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        ViewBag.Mensaje = "Configuración importada de forma segura. " +
            "Solo se aceptaron los campos definidos en el esquema (Tema, Idioma, " +
            "RegistrosPorPagina). Cualquier campo adicional fue descartado.";

        return View("ResultadoImportacion");
    }

    // =========================================================================
    // DEMO 3: Importación de datos con deserialización segura explícita
    // =========================================================================

    /// <summary>
    /// SEGURO: Muestra cómo deserializar JSON con opciones restrictivas
    /// explícitas, para casos donde se necesita más control.
    /// </summary>
    [HttpPost("importar-datos")]
    public IActionResult ImportarDatos()
    {
        try
        {
            // ✅ SEGURO: Opciones explícitas de deserialización
            var opciones = new JsonSerializerOptions
            {
                // No permitir comentarios en JSON (podrían ocultar payloads)
                ReadCommentHandling = JsonCommentHandling.Disallow,

                // Limitar profundidad máxima (previene stack overflow por JSON anidado)
                MaxDepth = 10,

                // Nombres de propiedades case-insensitive para robustez
                PropertyNameCaseInsensitive = true,

                // No permitir trailing commas (JSON estricto)
                AllowTrailingCommas = false,

                // Tamaño máximo del buffer — limita el tamaño del payload
                DefaultBufferSize = 1024 * 64 // 64 KB máximo
            };

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var jsonString = reader.ReadToEndAsync().Result;

            // Limitar tamaño del payload antes de deserializar
            if (jsonString.Length > 65_536)
            {
                _logger.LogWarning("A08 Defensa: Payload JSON demasiado grande ({Size} bytes)",
                    jsonString.Length);
                return BadRequest("El payload excede el tamaño máximo permitido (64 KB).");
            }

            // Deserializar a tipo conocido — NO a object o dynamic
            var datos = JsonSerializer.Deserialize<ConfiguracionUsuarioDto>(jsonString, opciones);

            if (datos is null)
            {
                return BadRequest("No se pudo procesar el JSON proporcionado.");
            }

            // Validar manualmente si no se usa [ApiController]
            var contextoValidacion = new ValidationContext(datos);
            var resultados = new List<ValidationResult>();
            if (!Validator.TryValidateObject(datos, contextoValidacion, resultados, true))
            {
                _logger.LogWarning("A08 Defensa: Validación manual falló.");
                return BadRequest(resultados.Select(r => r.ErrorMessage));
            }

            ViewBag.Resultado = "IMPORTADO_SEGURO";
            ViewBag.Mensaje = "Datos importados con deserialización segura: " +
                "tipo fijo, profundidad limitada, tamaño restringido.";
            return View("ResultadoImportacion");
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "A08 Defensa: JSON malformado rechazado.");
            return BadRequest("El formato JSON es inválido.");
        }
    }

    // =========================================================================
    // DEMO 4: Verificación de integridad con SHA-256
    // =========================================================================
    // DEFENSA: Antes de aplicar cualquier actualización, verificar que el
    // hash SHA-256 del contenido descargado coincida con el hash publicado
    // por el proveedor en un canal separado y confiable.
    //
    // Ley 8968 Art. 10: La integridad de los sistemas que procesan datos
    // personales debe ser garantizada. Aplicar actualizaciones sin verificar
    // integridad viola este principio.
    // =========================================================================

    /// <summary>
    /// SEGURO: Verifica la integridad de una "actualización" comparando
    /// el hash SHA-256 del contenido con el hash esperado.
    /// </summary>
    [HttpPost("aplicar-actualizacion")]
    public IActionResult AplicarActualizacion([FromBody] ActualizacionVerificadaDto? solicitud)
    {
        if (!ModelState.IsValid || solicitud is null)
        {
            ViewBag.Resultado = "RECHAZADO";
            ViewBag.Mensaje = "Solicitud de actualización inválida.";
            return View("ResultadoImportacion");
        }

        // ✅ Paso 1: Verificar que la URL proviene de un dominio confiable
        if (!Uri.TryCreate(solicitud.Url, UriKind.Absolute, out var uri)
            || uri.Scheme != "https")
        {
            _logger.LogWarning(
                "A08 Defensa: URL de actualización rechazada (no HTTPS): {Url}",
                solicitud.Url);

            ViewBag.Resultado = "RECHAZADO";
            ViewBag.Mensaje = "Solo se aceptan URLs con HTTPS.";
            return View("ResultadoImportacion");
        }

        if (!DominiosConfiables.Contains(uri.Host))
        {
            _logger.LogWarning(
                "A08 Defensa: Dominio no confiable rechazado: {Host}", uri.Host);

            ViewBag.Resultado = "RECHAZADO";
            ViewBag.Mensaje = $"El dominio '{uri.Host}' no está en la lista de " +
                "dominios confiables para actualizaciones.";
            return View("ResultadoImportacion");
        }

        // ✅ Paso 2: Simulación — en producción, se descargaría el archivo
        // y se calcularía su hash SHA-256 real
        var contenidoSimulado = "contenido-de-actualizacion-demo"u8.ToArray();
        var hashCalculado = CalcularSha256(contenidoSimulado);

        // ✅ Paso 3: Comparar hash calculado con hash esperado
        // Usar comparación en tiempo constante para prevenir timing attacks
        var hashesCoinciden = CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(hashCalculado),
            Convert.FromHexString(solicitud.HashEsperado));

        if (!hashesCoinciden)
        {
            _logger.LogError(
                "A08 Defensa: INTEGRIDAD COMPROMETIDA. Hash esperado: {Esperado}, " +
                "Hash calculado: {Calculado}",
                solicitud.HashEsperado, hashCalculado);

            ViewBag.Resultado = "INTEGRIDAD_FALLIDA";
            ViewBag.Mensaje = "⚠️ La verificación de integridad FALLÓ. " +
                "El contenido descargado no coincide con el hash esperado. " +
                "La actualización fue RECHAZADA. Posible manipulación en tránsito (MITM).";
            return View("ResultadoImportacion");
        }

        _logger.LogInformation(
            "A08 Defensa: Actualización verificada exitosamente. Hash: {Hash}",
            hashCalculado);

        ViewBag.Resultado = "VERIFICADO_OK";
        ViewBag.Mensaje = "✅ Integridad verificada. El hash SHA-256 del contenido " +
            $"coincide con el esperado ({hashCalculado[..16]}...). " +
            "La actualización puede aplicarse de forma segura.";
        return View("ResultadoImportacion");
    }

    // =========================================================================
    // Utilidad: Cálculo de hash SHA-256
    // =========================================================================
    private static string CalcularSha256(byte[] contenido)
    {
        var hashBytes = SHA256.HashData(contenido);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
