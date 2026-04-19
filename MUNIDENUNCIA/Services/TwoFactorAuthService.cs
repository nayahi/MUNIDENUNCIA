// =============================================================================
// TwoFactorAuthService.cs
// Semana 3 - A07: Fallas de Identificación y Autenticación
// Ubicación: Services/TwoFactorAuthService.cs
// =============================================================================
// PROPÓSITO EDUCATIVO
// Este servicio implementa el [Paso 2] de la diapositiva 4 de Semana 3:
// "Autenticación Multifactor (MFA) obligatoria".
//
// Usa el estándar TOTP (Time-based One-Time Password, RFC 6238), que es el
// mismo protocolo que usan Google Authenticator, Microsoft Authenticator,
// Authy, y cualquier app compatible con "FreeOTP" u "otpauth://".
//
// CONTEXTO LEY 8968
// Art. 10 (deber de seguridad): "El responsable de la base de datos... deberá
// adoptar las medidas de índole técnica y organizativa necesarias para
// garantizar la seguridad de los datos de carácter personal". Para cuentas
// de funcionarios municipales con acceso a denuncias ciudadanas, el MFA es
// una medida técnica RAZONABLEMENTE EXIGIBLE hoy — su ausencia podría
// interpretarse como negligencia en auditorías de PRODHAB.
//
// DEPENDENCIAS NuGet REQUERIDAS
//   dotnet add package Otp.NET        (versión 1.4.0 o superior)
//   dotnet add package QRCoder        (versión 1.6.0 o superior)
//
// Otp.NET es la librería de referencia para TOTP en .NET (MIT license).
// QRCoder genera el PNG del código QR sin servicios externos (crítico:
// NUNCA usar generadores de QR online — enviarían el secreto TOTP a terceros).
// =============================================================================

using System.Security.Cryptography;
using OtpNet;
using QRCoder;

namespace MUNIDENUNCIA.Services;

/// <summary>
/// Servicio de autenticación de dos factores basado en TOTP (RFC 6238).
/// Genera secretos, códigos QR para enrolamiento, y verifica códigos de 6 dígitos
/// con tolerancia a desfase de reloj (drift window).
/// </summary>
public class TwoFactorAuthService
{
    private readonly ILogger<TwoFactorAuthService> _logger;

    // =========================================================================
    // Constantes del estándar TOTP
    // No modificar sin actualizar la configuración del authenticator del usuario.
    // =========================================================================
    private const string Emisor           = "MuniDenuncia CR";   // Aparece en Google Authenticator
    private const int    TamanioSecreto   = 20;                  // 160 bits (recomendado por RFC 4226)
    private const int    DigitosCodigo    = 6;                   // Estándar de la industria
    private const int    PasoSegundos     = 30;                  // Ventana de 30s entre códigos

    public TwoFactorAuthService(ILogger<TwoFactorAuthService> logger)
    {
        _logger = logger;
    }

    // =========================================================================
    // MÉTODO 1: Generar un secreto TOTP aleatorio para un nuevo usuario
    // =========================================================================

    /// <summary>
    /// Genera un secreto TOTP criptográficamente aleatorio de 160 bits.
    /// El secreto se devuelve en Base32 (formato esperado por authenticator apps).
    /// DEBE persistirse asociado al usuario — si se pierde, el usuario queda
    /// bloqueado y debe usar sus códigos de respaldo.
    /// </summary>
    public string GenerarSecreto()
    {
        // ✅ Usar RandomNumberGenerator (CSPRNG), NUNCA System.Random
        var bytesAleatorios = RandomNumberGenerator.GetBytes(TamanioSecreto);
        return Base32Encoding.ToString(bytesAleatorios);
    }

    // =========================================================================
    // MÉTODO 2: Construir la URI otpauth:// para el código QR
    // =========================================================================

    /// <summary>
    /// Construye la URI estándar otpauth://totp/... que se codifica en el QR.
    /// Formato definido por Google Authenticator Key Uri Format.
    /// </summary>
    public string ConstruirUriProvisionamiento(string emailUsuario, string secretoBase32)
    {
        // Se codifican emisor y email para evitar problemas con caracteres especiales
        var emisorCodificado = Uri.EscapeDataString(Emisor);
        var emailCodificado  = Uri.EscapeDataString(emailUsuario);

        return $"otpauth://totp/{emisorCodificado}:{emailCodificado}" +
               $"?secret={secretoBase32}" +
               $"&issuer={emisorCodificado}" +
               $"&algorithm=SHA1" +
               $"&digits={DigitosCodigo}" +
               $"&period={PasoSegundos}";
    }

    // =========================================================================
    // MÉTODO 3: Generar el PNG del código QR
    // =========================================================================

    /// <summary>
    /// Genera el código QR como PNG en memoria.
    /// IMPORTANTE: No persistir este PNG en disco ni enviarlo por email —
    /// contiene el secreto TOTP en texto plano dentro de la URI codificada.
    /// </summary>
    public byte[] GenerarQrPng(string uriProvisionamiento)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(uriProvisionamiento, QRCodeGenerator.ECCLevel.Q);
        using var qrPng  = new PngByteQRCode(qrData);
        return qrPng.GetGraphic(pixelsPerModule: 10);
    }

    // =========================================================================
    // MÉTODO 4: Verificar un código TOTP de 6 dígitos
    // =========================================================================

    /// <summary>
    /// Verifica que el código proporcionado por el usuario coincida con el esperado
    /// para el timestamp actual. Permite una ventana de ±1 paso (±30s) para tolerar
    /// desfase de reloj entre el servidor y el teléfono del usuario.
    /// </summary>
    /// <param name="secretoBase32">Secreto del usuario, previamente almacenado.</param>
    /// <param name="codigoUsuario">Código de 6 dígitos que escribió el usuario.</param>
    /// <returns>true si el código es válido; false en caso contrario.</returns>
    public bool VerificarCodigo(string secretoBase32, string codigoUsuario)
    {
        if (string.IsNullOrWhiteSpace(secretoBase32) || string.IsNullOrWhiteSpace(codigoUsuario))
        {
            return false;
        }

        // El usuario puede haber escrito espacios o guiones: "123 456" o "123-456"
        var codigoLimpio = new string(codigoUsuario.Where(char.IsDigit).ToArray());
        if (codigoLimpio.Length != DigitosCodigo)
        {
            return false;
        }

        try
        {
            var bytesSecreto = Base32Encoding.ToBytes(secretoBase32);
            var totp = new Totp(bytesSecreto, step: PasoSegundos, totpSize: DigitosCodigo);

            // ✅ VerificationWindow permite ±1 paso para tolerar desfase de reloj.
            // Sin esto, un usuario con reloj atrasado 15 segundos NUNCA puede entrar.
            // No ampliar la ventana más allá de ±1: aumenta la ventana de ataque.
            return totp.VerifyTotp(codigoLimpio, out _, new VerificationWindow(previous: 1, future: 1));
        }
        catch (Exception ex)
        {
            // NO revelar detalles al usuario. Posibles causas: secreto corrupto,
            // Base32 malformado, etc. Registrar para diagnóstico interno.
            _logger.LogWarning(ex, "Falla al verificar código TOTP (causa interna).");
            return false;
        }
    }

    // =========================================================================
    // MÉTODO 5: Generar códigos de respaldo (recovery codes)
    // =========================================================================

    /// <summary>
    /// Genera un conjunto de códigos de respaldo de un solo uso.
    /// Se entregan al usuario UNA sola vez, después de activar MFA.
    /// Permiten recuperar acceso si el usuario pierde su teléfono.
    /// </summary>
    /// <param name="cantidad">Cantidad de códigos (8 es estándar).</param>
    public List<string> GenerarCodigosRespaldo(int cantidad = 8)
    {
        var codigos = new List<string>(cantidad);
        for (var i = 0; i < cantidad; i++)
        {
            // Formato legible: XXXX-XXXX (8 dígitos agrupados)
            var bytes = RandomNumberGenerator.GetBytes(5);
            var numero = BitConverter.ToUInt32(bytes, 0) % 100_000_000U;
            codigos.Add($"{numero / 10_000:D4}-{numero % 10_000:D4}");
        }
        return codigos;
    }
}
