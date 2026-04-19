// =============================================================================
// FirmaDigitalService.cs
// Semana 3 - A08: Fallas en Software y en la Integridad de los Datos
// Ubicación: Services/FirmaDigitalService.cs
// =============================================================================
// PROPÓSITO EDUCATIVO
// La diapositiva 5 de Semana 3 muestra el "Integrity Checkpoint & Digital
// Signature" como el control que transforma un flujo INSEGURO (datos sin
// verificar llegan al servidor) en uno SEGURO (checkpoint de integridad
// antes de procesar).
//
// Este servicio implementa ese checkpoint para archivos exportados por
// MuniDenuncia:
//   - Reportes PDF/CSV de denuncias descargables por ciudadanos
//   - Actas firmadas por funcionarios
//   - Cualquier documento oficial que salga del sistema
//
// CONTEXTO MUNICIPAL COSTA RICA
// El MICITT publica la Política de Formatos Oficiales para Firma Digital
// de Documentos Electrónicos (Decreto 38880-MICITT y siguientes). Aunque
// este servicio NO produce firmas con la CA del SINPE (eso requiere la
// tarjeta de firma digital del funcionario), sí establece la MECÁNICA
// correcta: clave privada en el servidor, clave pública publicada, firma
// sobre hash SHA-256, verificación independiente.
//
// USO TÍPICO
//   // Al exportar un reporte:
//   var bytes = GenerarPdfDenuncia(id);
//   var firma = firmaService.Firmar(bytes);
//   // Devolver (bytes + firma) al cliente
//
//   // Al recibir de vuelta (o al verificar desde auditoría):
//   bool integro = firmaService.Verificar(bytes, firma);
//
// CRÍTICO: GESTIÓN DE CLAVES
// La clave privada NUNCA debe vivir en appsettings.json ni en el
// repositorio Git. Este servicio la carga desde:
//   1. Variable de entorno (desarrollo local)
//   2. Archivo protegido por ACL (producción self-hosted)
//   3. Opcionalmente, certificado X.509 del Windows Certificate Store
// En producción real se usaría Azure Key Vault o HashiCorp Vault.
// =============================================================================

using System.Security.Cryptography;

namespace MUNIDENUNCIA.Services;

/// <summary>
/// Servicio de firma digital RSA-2048 con SHA-256 (RSA-PSS padding).
/// Garantiza integridad y autenticidad de archivos exportados por MuniDenuncia.
/// </summary>
public class FirmaDigitalService : IDisposable
{
    private readonly RSA _rsaParaFirmar;
    private readonly RSA _rsaParaVerificar;
    private readonly ILogger<FirmaDigitalService> _logger;

    // =========================================================================
    // Constantes del esquema criptográfico
    // Cambiar estos valores requiere migrar todas las firmas previas.
    // =========================================================================
    private const int    TamanioClaveBits = 2048;   // Mínimo recomendado por NIST SP 800-57
    private static readonly HashAlgorithmName HashAlg = HashAlgorithmName.SHA256;
    private static readonly RSASignaturePadding Padding = RSASignaturePadding.Pss;

    public FirmaDigitalService(
        IConfiguration configuration,
        IWebHostEnvironment env,
        ILogger<FirmaDigitalService> logger)
    {
        _logger = logger;

        // Ruta donde viven las claves. En desarrollo, dentro de App_Data
        // (agregada a .gitignore). En producción, fuera del directorio web.
        var directorioClaves = configuration["FirmaDigital:DirectorioClaves"]
            ?? Path.Combine(env.ContentRootPath, "App_Data", "firma-digital");

        var rutaPrivada = Path.Combine(directorioClaves, "private-key.pem");
        var rutaPublica = Path.Combine(directorioClaves, "public-key.pem");

        // Si no existen, generarlas en primer arranque (SOLO apto para demo).
        // En producción, generarlas fuera de banda y copiarlas al servidor.
        if (!File.Exists(rutaPrivada) || !File.Exists(rutaPublica))
        {
            _logger.LogWarning(
                "No se encontraron claves RSA en {Dir}. Generando nuevo par de claves. " +
                "En producción, las claves deben generarse fuera de banda.",
                directorioClaves);
            Directory.CreateDirectory(directorioClaves);
            GenerarParDeClaves(rutaPrivada, rutaPublica);
        }

        // Cargar clave privada (para firmar)
        _rsaParaFirmar = RSA.Create();
        _rsaParaFirmar.ImportFromPem(File.ReadAllText(rutaPrivada));

        // Cargar clave pública (para verificar — podríamos usar la privada
        // también, pero separar hace el código auto-documentado)
        _rsaParaVerificar = RSA.Create();
        _rsaParaVerificar.ImportFromPem(File.ReadAllText(rutaPublica));

        _logger.LogInformation(
            "FirmaDigitalService inicializado con clave RSA-{Bits}",
            _rsaParaFirmar.KeySize);
    }

    // =========================================================================
    // MÉTODO 1: Firmar un arreglo de bytes
    // =========================================================================

    /// <summary>
    /// Firma el contenido con la clave privada RSA-2048 usando SHA-256 y PSS padding.
    /// La firma resultante es de 256 bytes.
    /// </summary>
    public byte[] Firmar(byte[] contenido)
    {
        if (contenido is null || contenido.Length == 0)
            throw new ArgumentException("El contenido a firmar no puede estar vacío.",
                nameof(contenido));

        var firma = _rsaParaFirmar.SignData(contenido, HashAlg, Padding);

        _logger.LogInformation(
            "Documento firmado. Tamaño del contenido: {Bytes} bytes. Firma: {FirmaBytes} bytes.",
            contenido.Length, firma.Length);

        return firma;
    }

    /// <summary>
    /// Firma el contenido y devuelve la firma en Base64 (conveniente para guardar
    /// en un campo NVARCHAR o enviarla como cabecera HTTP).
    /// </summary>
    public string FirmarBase64(byte[] contenido) => Convert.ToBase64String(Firmar(contenido));

    // =========================================================================
    // MÉTODO 2: Verificar una firma
    // =========================================================================

    /// <summary>
    /// Verifica que la firma provista corresponde al contenido.
    /// Devuelve false ante CUALQUIER falla (firma inválida, contenido modificado,
    /// padding incorrecto, etc.) sin revelar cuál fue.
    /// </summary>
    public bool Verificar(byte[] contenido, byte[] firma)
    {
        if (contenido is null || contenido.Length == 0 ||
            firma is null || firma.Length == 0)
        {
            return false;
        }

        try
        {
            return _rsaParaVerificar.VerifyData(contenido, firma, HashAlg, Padding);
        }
        catch (CryptographicException ex)
        {
            _logger.LogWarning(ex, "Firma digital inválida (causa criptográfica).");
            return false;
        }
    }

    public bool VerificarBase64(byte[] contenido, string firmaBase64)
    {
        try
        {
            return Verificar(contenido, Convert.FromBase64String(firmaBase64));
        }
        catch (FormatException)
        {
            return false;
        }
    }

    // =========================================================================
    // MÉTODO 3: Exportar clave pública para distribución
    // =========================================================================

    /// <summary>
    /// Devuelve la clave pública en formato PEM para publicarla en el sitio.
    /// Los ciudadanos pueden descargarla para verificar firmas usando herramientas
    /// como OpenSSL sin depender del servidor.
    /// </summary>
    public string ExportarClavePublicaPem() =>
        _rsaParaVerificar.ExportSubjectPublicKeyInfoPem();

    // =========================================================================
    // Helpers privados
    // =========================================================================

    private static void GenerarParDeClaves(string rutaPrivada, string rutaPublica)
    {
        using var rsa = RSA.Create(TamanioClaveBits);
        File.WriteAllText(rutaPrivada, rsa.ExportPkcs8PrivateKeyPem());
        File.WriteAllText(rutaPublica, rsa.ExportSubjectPublicKeyInfoPem());

        // En Linux establecer permisos 600 sobre la clave privada.
        // En Windows, los permisos ACL deben configurarse manualmente al
        // desplegar (heredar de "IIS AppPool\MuniDenuncia" + Administradores).
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(rutaPrivada,
                UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    public void Dispose()
    {
        _rsaParaFirmar?.Dispose();
        _rsaParaVerificar?.Dispose();
        GC.SuppressFinalize(this);
    }
}
