// =============================================================================
// FirmaController.cs
// Semana 3 - A08: Controlador de demostración de firma digital
// Ubicación: Controllers/FirmaController.cs
// =============================================================================
// PROPÓSITO EDUCATIVO
// Este controller expone los flujos de firma digital para la demo de clase:
//   - GET  /Firma                           → página con formulario de prueba
//   - GET  /Firma/ClavePublica              → descarga la clave pública PEM
//   - POST /Firma/DescargarReporteFirmado   → genera reporte + firma
//   - POST /Firma/Verificar                 → verifica archivo + firma subidos
//
// CONEXIÓN CON LEY 8968
// Art. 10 (deber de seguridad) exige preservar la integridad de los datos.
// Un reporte de denuncias que sale del sistema con firma digital:
//   - Permite al ciudadano verificar que NO fue alterado post-descarga
//   - Protege al municipio contra reclamos de "ese documento no es el que
//     me dieron" (no-repudio)
//   - Cumple el requisito de "medidas técnicas razonablemente exigibles"
// =============================================================================

using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MUNIDENUNCIA.Services;

namespace MUNIDENUNCIA.Controllers;

[Authorize]
public class FirmaController : Controller
{
    private readonly FirmaDigitalService         _firmaService;
    private readonly ILogger<FirmaController>    _logger;

    public FirmaController(
        FirmaDigitalService firmaService,
        ILogger<FirmaController> logger)
    {
        _firmaService = firmaService;
        _logger       = logger;
    }

    // =========================================================================
    // GET /Firma — Página de demostración
    // =========================================================================
    public IActionResult Index() => View();

    // =========================================================================
    // GET /Firma/ClavePublica — Descargar clave pública PEM
    // Cualquier ciudadano puede descargarla y verificar firmas con openssl:
    //   openssl dgst -sha256 -sigopt rsa_padding_mode:pss \
    //     -verify public-key.pem -signature reporte.sig reporte.pdf
    // =========================================================================
    [AllowAnonymous]   // La clave PÚBLICA es, por definición, pública
    [HttpGet]
    public IActionResult ClavePublica()
    {
        var pem = _firmaService.ExportarClavePublicaPem();
        var bytes = Encoding.ASCII.GetBytes(pem);
        return File(bytes, "application/x-pem-file", "munidenuncia-public-key.pem");
    }

    // Whitelist explícita: evita path traversal en nombres de entrada ZIP (A08)
    private static readonly HashSet<string> _tiposPermitidos =
        new(StringComparer.OrdinalIgnoreCase) { "denuncias", "permisos", "acta" };

    // =========================================================================
    // POST /Firma/DescargarReporteFirmado
    // En la demo real, este método generaría un PDF de denuncias del ciudadano.
    // Para el ejercicio pedagógico, genera un reporte TXT sintético que
    // muestra el mecanismo sin requerir dependencias extra de PDF.
    // =========================================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DescargarReporteFirmado(string tipoReporte = "denuncias")
    {
        // Rechazar valores no permitidos: un tipoReporte arbitrario podría
        // generar nombres de entrada ZIP como "../../evil" (ZIP path traversal).
        if (!_tiposPermitidos.Contains(tipoReporte))
            tipoReporte = "denuncias";

        // 1. Generar el contenido del reporte
        var contenido = GenerarContenidoReporte(tipoReporte);

        // 2. Firmar el contenido
        var firma = _firmaService.Firmar(contenido);

        // 3. Construir respuesta multipart:
        //    Opción A: dos archivos (reporte.txt + reporte.txt.sig)
        //    Opción B: ZIP con ambos dentro
        //    Para demo, usamos opción B por claridad al estudiante.
        var zipBytes = CrearZipConReporteYFirma(contenido, firma, tipoReporte);

        _logger.LogInformation(
            "Reporte {Tipo} firmado y entregado al usuario {User}",
            tipoReporte, User.Identity?.Name);

        return File(zipBytes, "application/zip",
            $"reporte-{tipoReporte}-{DateTime.UtcNow:yyyyMMdd-HHmmss}Z.zip");
    }

    // =========================================================================
    // POST /Firma/Verificar — Verificar archivo + firma subidos
    // =========================================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(5_000_000)]   // 5 MB máximo
    public async Task<IActionResult> Verificar(IFormFile archivo, IFormFile firma)
    {
        if (archivo is null || firma is null)
        {
            ViewBag.Resultado = "Debe subir el archivo Y su firma.";
            return View("Index");
        }

        byte[] contenidoBytes;
        byte[] firmaBytes;

        using (var ms = new MemoryStream())
        {
            await archivo.CopyToAsync(ms);
            contenidoBytes = ms.ToArray();
        }
        using (var ms = new MemoryStream())
        {
            await firma.CopyToAsync(ms);
            firmaBytes = ms.ToArray();
        }

        var esValida = _firmaService.Verificar(contenidoBytes, firmaBytes);

        _logger.LogInformation(
            "Verificación de firma. Archivo={Archivo}, Resultado={Resultado}",
            archivo.FileName, esValida ? "VÁLIDA" : "INVÁLIDA");

        ViewBag.Resultado = esValida
            ? "✓ La firma es VÁLIDA. El archivo no ha sido modificado."
            : "✗ La firma es INVÁLIDA. El archivo fue modificado o la firma no corresponde.";
        ViewBag.Archivo = archivo.FileName;

        return View("Index");
    }

    // =========================================================================
    // Helpers privados
    // =========================================================================

    private static byte[] GenerarContenidoReporte(string tipo)
    {
        var sb = new StringBuilder();
        sb.AppendLine("==============================================");
        sb.AppendLine("  MUNIDENUNCIA - Reporte Oficial");
        sb.AppendLine("  Municipalidad Demo Costa Rica");
        sb.AppendLine("==============================================");
        sb.AppendLine($"Tipo:       {tipo}");
        sb.AppendLine($"Generado:   {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
        sb.AppendLine($"Ejemplar:   {Guid.NewGuid()}");
        sb.AppendLine();
        sb.AppendLine("Este documento está firmado digitalmente con la");
        sb.AppendLine("clave RSA-2048 de MuniDenuncia. Para verificar su");
        sb.AppendLine("integridad, use la clave pública disponible en:");
        sb.AppendLine("  /Firma/ClavePublica");
        sb.AppendLine();
        sb.AppendLine("OpenSSL:");
        sb.AppendLine("  openssl dgst -sha256 -sigopt rsa_padding_mode:pss \\");
        sb.AppendLine("    -verify clave-publica.pem -signature reporte.sig reporte.txt");
        sb.AppendLine("==============================================");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static byte[] CrearZipConReporteYFirma(
        byte[] contenido, byte[] firma, string tipoReporte)
    {
        using var ms = new MemoryStream();
        using (var zip = new System.IO.Compression.ZipArchive(
            ms, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
        {
            var entradaReporte = zip.CreateEntry($"reporte-{tipoReporte}.txt");
            using (var stream = entradaReporte.Open())
            {
                stream.Write(contenido);
            }

            var entradaFirma = zip.CreateEntry($"reporte-{tipoReporte}.sig");
            using (var stream = entradaFirma.Open())
            {
                stream.Write(firma);
            }

            var entradaReadme = zip.CreateEntry("LEEME.txt");
            using (var stream = entradaReadme.Open())
            {
                var txt = Encoding.UTF8.GetBytes(
                    "Este ZIP contiene:\n" +
                    "  - reporte-*.txt : el documento\n" +
                    "  - reporte-*.sig : la firma digital (RSA-2048, SHA-256, PSS)\n" +
                    "\n" +
                    "Para verificar:\n" +
                    "  1. Descargar la clave pública: /Firma/ClavePublica\n" +
                    "  2. openssl dgst -sha256 -sigopt rsa_padding_mode:pss \\\n" +
                    "       -verify munidenuncia-public-key.pem \\\n" +
                    "       -signature reporte-*.sig reporte-*.txt\n");
                stream.Write(txt);
            }
        }
        return ms.ToArray();
    }
}
