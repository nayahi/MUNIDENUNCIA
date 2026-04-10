// =============================================================================
// IntegridadVulnerableController.cs
// Semana 2 - A08: Fallas en el Software y en la Integridad de los Datos
// Ubicación: Controllers/IntegridadVulnerableController.cs
// =============================================================================
// PROPÓSITO EDUCATIVO: Este controlador muestra prácticas INSEGURAS de
// deserialización y verificación de integridad. NUNCA usar en producción.
// =============================================================================

using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace MuniDenuncia.Controllers;

/// <summary>
/// Controlador VULNERABLE que demuestra fallas de integridad de datos (OWASP A08).
/// Incluye: deserialización insegura, falta de verificación de integridad,
/// y aceptación de datos no confiables sin validación.
/// </summary>
[Route("[controller]")]
public class IntegridadVulnerableController : Controller
{
    private readonly ILogger<IntegridadVulnerableController> _logger;

    public IntegridadVulnerableController(ILogger<IntegridadVulnerableController> logger)
    {
        _logger = logger;
    }

    // =========================================================================
    // DEMO 1: Página principal con explicación
    // =========================================================================
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    // =========================================================================
    // DEMO 2: Deserialización insegura con BinaryFormatter
    // =========================================================================
    // VULNERABILIDAD: BinaryFormatter puede ejecutar código arbitrario durante
    // la deserialización. Un atacante puede enviar un payload serializado que
    // ejecute comandos en el servidor.
    //
    // Contexto municipal: Si un sistema municipal acepta archivos binarios
    // serializados (ej: importación de datos de otro sistema), un atacante
    // podría enviar un archivo malicioso que ejecute código en el servidor
    // de la municipalidad, comprometiendo toda la red interna.
    // =========================================================================

    /// <summary>
    /// INSEGURO: Acepta datos binarios y los deserializa con BinaryFormatter.
    /// BinaryFormatter está obsoleto desde .NET 5 y prohibido desde .NET 8
    /// precisamente por este riesgo.
    /// </summary>
    [HttpPost("importar-binario")]
    public IActionResult ImportarDatosBinario()
    {
        try
        {
            // ⚠️ VULNERABLE: BinaryFormatter ejecuta código durante deserialización
            // En .NET 8, BinaryFormatter lanza NotSupportedException por defecto.
            // Se incluye aquí como ejemplo conceptual de lo que NUNCA debe hacerse.
            //
            // En versiones anteriores (.NET Framework, .NET 5-7 con config habilitada):
            // var formatter = new BinaryFormatter();
            // var obj = formatter.Deserialize(Request.Body); // ← PELIGROSO
            //
            // Un atacante envía un payload serializado con ysoserial.net que
            // ejecuta: Process.Start("cmd", "/c net user hacker P@ss /add")

            // Simulación para la demo (sin ejecutar BinaryFormatter real):
            _logger.LogWarning(
                "DEMO A08: Intento de deserialización binaria detectado. " +
                "BinaryFormatter está bloqueado en .NET 8 por seguridad.");

            ViewBag.Resultado = "ERROR_CONCEPTUAL";
            ViewBag.Mensaje = "BinaryFormatter está prohibido en .NET 8. " +
                "En versiones anteriores, este código permitiría ejecución " +
                "remota de código (RCE) a través de payloads maliciosos.";

            return View("ResultadoImportacion");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en importación binaria");
            return View("ResultadoImportacion");
        }
    }

    // =========================================================================
    // DEMO 3: Deserialización JSON sin validación de tipo
    // =========================================================================
    // VULNERABILIDAD: Aceptar JSON con información de tipo ($type) permite
    // al atacante instanciar cualquier clase del ensamblado, potencialmente
    // ejecutando código malicioso.
    //
    // Contexto municipal: Un formulario web que acepta JSON de configuración
    // podría ser explotado para instanciar clases peligrosas si usa
    // JsonSerializerOptions con TypeNameHandling habilitado.
    // =========================================================================

    /// <summary>
    /// INSEGURO: Deserializa JSON usando configuración permisiva que acepta
    /// propiedades desconocidas sin validación de esquema.
    /// </summary>
    [HttpPost("importar-configuracion")]
    public IActionResult ImportarConfiguracion([FromBody] JsonElement configuracion)
    {
        try
        {
            // ⚠️ VULNERABLE: No valida el esquema del JSON entrante.
            // Acepta cualquier estructura sin verificar que corresponda
            // al modelo esperado. Un atacante puede inyectar campos
            // adicionales que alteren el comportamiento del sistema.

            // Ejemplo: el sistema espera { "tema": "oscuro", "idioma": "es" }
            // pero el atacante envía:
            // { "tema": "oscuro", "idioma": "es", "rol": "Administrador",
            //   "permisos": ["eliminar_todo"] }

            var jsonString = configuracion.GetRawText();

            // Sin validación de esquema — acepta TODO
            _logger.LogInformation(
                "Configuración recibida sin validación: {Config}", jsonString);

            // ⚠️ Peor aún: si se usa Newtonsoft.Json con TypeNameHandling.All:
            // var settings = new JsonSerializerSettings {
            //     TypeNameHandling = TypeNameHandling.All  // ← PELIGROSO
            // };
            // var obj = JsonConvert.DeserializeObject(jsonString, settings);
            // Esto permitiría instanciar cualquier tipo del ensamblado.

            ViewBag.Resultado = "IMPORTADO_SIN_VALIDACION";
            ViewBag.Datos = jsonString;
            ViewBag.Mensaje = "Configuración importada sin validar esquema ni tipo. " +
                "Un atacante podría inyectar campos maliciosos como 'rol' o 'permisos'.";

            return View("ResultadoImportacion");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en importación de configuración");
            ViewBag.Resultado = "ERROR";
            return View("ResultadoImportacion");
        }
    }

    // =========================================================================
    // DEMO 4: Falta de verificación de integridad en actualizaciones
    // =========================================================================
    // VULNERABILIDAD: Aceptar actualizaciones (scripts, plugins, configuración)
    // sin verificar su integridad mediante hash o firma digital.
    //
    // Contexto municipal: Si el sistema de la municipalidad descarga
    // actualizaciones automáticas sin verificar firma digital, un atacante
    // en la red (MITM) podría inyectar código malicioso en la actualización.
    // =========================================================================

    /// <summary>
    /// INSEGURO: Simula la descarga y aplicación de un "plugin" o "actualización"
    /// sin verificar su hash o firma digital.
    /// </summary>
    [HttpPost("aplicar-actualizacion")]
    public IActionResult AplicarActualizacion(string urlActualizacion)
    {
        // ⚠️ VULNERABLE: No verifica integridad del recurso descargado
        // No compara hash SHA-256 del archivo
        // No verifica firma digital del proveedor
        // No usa HTTPS exclusivamente

        _logger.LogWarning(
            "DEMO A08: Simulación de actualización sin verificación de integridad " +
            "desde URL: {Url}", urlActualizacion);

        ViewBag.Resultado = "ACTUALIZADO_SIN_VERIFICAR";
        ViewBag.Mensaje = $"Se aplicó la 'actualización' desde {urlActualizacion} " +
            "SIN verificar hash SHA-256 ni firma digital. " +
            "Un atacante MITM podría haber reemplazado el contenido.";

        return View("ResultadoImportacion");
    }
}
