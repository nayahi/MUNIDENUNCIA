// =============================================================================
// TwoFactorViewModels.cs
// Semana 3 - A07: ViewModels para los flujos de MFA
// Ubicación: ViewModels/TwoFactorViewModels.cs
// =============================================================================

using System.ComponentModel.DataAnnotations;

namespace MUNIDENUNCIA.ViewModels;

/// <summary>
/// Modelo para la vista de configuración inicial de MFA.
/// Muestra el QR al usuario y captura el primer código de verificación
/// para confirmar que el authenticator fue configurado correctamente.
/// </summary>
public class ConfigurarMfaViewModel
{
    /// <summary>Secreto TOTP en Base32 (mostrado también como texto para
    /// usuarios cuyo escáner QR no funciona).</summary>
    public string Secreto { get; set; } = string.Empty;

    /// <summary>Data URI del PNG del QR code para embebir en &lt;img src&gt;.</summary>
    public string QrCodeDataUri { get; set; } = string.Empty;

    /// <summary>Código de 6 dígitos que el usuario escribe desde su app.</summary>
    [Required(ErrorMessage = "Debe ingresar el código de 6 dígitos.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "El código debe ser exactamente 6 dígitos.")]
    [Display(Name = "Código de verificación")]
    public string Codigo { get; set; } = string.Empty;
}

/// <summary>
/// Modelo para ingresar el código TOTP durante el login.
/// Aparece como segundo paso después de usuario/contraseña.
/// </summary>
public class LoginMfaViewModel
{
    [Required(ErrorMessage = "Debe ingresar su código de autenticación.")]
    [RegularExpression(@"^(\d{6}|\d{4}-\d{4})$",
        ErrorMessage = "Ingrese un código de 6 dígitos o un código de respaldo XXXX-XXXX.")]
    [Display(Name = "Código de autenticación")]
    public string Codigo { get; set; } = string.Empty;

    /// <summary>true si el usuario marca "recordar este dispositivo por 30 días".</summary>
    [Display(Name = "Confiar en este navegador por 30 días")]
    public bool RecordarDispositivo { get; set; }

    /// <summary>URL de retorno después de login exitoso (validada contra open redirect).</summary>
    public string? ReturnUrl { get; set; }
}

/// <summary>
/// Modelo para mostrar los códigos de respaldo UNA sola vez después
/// de activar MFA. El usuario debe guardarlos en lugar seguro.
/// </summary>
public class CodigosRespaldoViewModel
{
    public List<string> Codigos { get; set; } = new();
    public DateTime GeneradosEn { get; set; } = DateTime.UtcNow;
}
