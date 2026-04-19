// =============================================================================
// ManageController.cs
// Semana 3 - A07: Gestión de MFA para el usuario autenticado
// Ubicación: Controllers/ManageController.cs
// =============================================================================
// PROPÓSITO EDUCATIVO
// Este controlador expone los flujos de MFA del usuario final:
//   - GET  /Manage/ConfigurarMfa   → muestra el QR y pide primer código
//   - POST /Manage/ConfigurarMfa   → verifica el código, activa MFA,
//                                     genera códigos de respaldo
//   - GET  /Manage/CodigosRespaldo → muestra los códigos (solo una vez)
//   - POST /Manage/DeshabilitarMfa → desactiva MFA (auditado)
//
// NOTA DE IMPLEMENTACIÓN
// Para mantener el código conciso y pedagógico, este controlador usa
// ASP.NET Core Identity directamente (UserManager<ApplicationUser>).
// ApplicationUser se asume extendido con las propiedades:
//   - string? TotpSecret              (Base32, encriptado con Data Protection)
//   - bool    TotpEnabled
//   - string? RecoveryCodesHash       (JSON de hashes SHA-256 de los códigos)
//   - DateTime? MfaEnabledOn
//
// Esas propiedades deben agregarse en la clase ApplicationUser y migrarse
// con EF Core Migrations ANTES de que este controlador funcione.
// El caso de demo en clase incluirá esa migración.
// =============================================================================

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MUNIDENUNCIA.Data;
using MUNIDENUNCIA.Models;         // ApplicationUser
using MUNIDENUNCIA.Services;
using MUNIDENUNCIA.ViewModels;

namespace MUNIDENUNCIA.Controllers;

[Authorize]
public class ManageController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TwoFactorAuthService         _mfaService;
    private readonly IDataProtector               _dataProtector;
    private readonly ILogger<ManageController>    _logger;
    private readonly ApplicationDbContext         _db;

    public ManageController(
        UserManager<ApplicationUser>    userManager,
        TwoFactorAuthService            mfaService,
        IDataProtectionProvider         dataProtectionProvider,
        ILogger<ManageController>       logger,
        ApplicationDbContext            db)
    {
        _userManager   = userManager;
        _mfaService    = mfaService;
        _logger        = logger;
        _db            = db;

        // ✅ Usamos Data Protection para cifrar el secreto TOTP en BD.
        // Si la BD se filtra, el secreto no es directamente utilizable sin
        // la key ring del Data Protection API.
        _dataProtector = dataProtectionProvider.CreateProtector(
            "MuniDenuncia.Mfa.TotpSecret.v1");
    }

    // =========================================================================
    // GET /Manage/ConfigurarMfa — Mostrar QR y pedir primer código
    // =========================================================================
    [HttpGet]
    public async Task<IActionResult> ConfigurarMfa()
    {
        var usuario = await _userManager.GetUserAsync(User);
        if (usuario is null) return Challenge();

        if (usuario.TotpEnabled)
        {
            TempData["Mensaje"] = "El MFA ya está activo en su cuenta.";
            return RedirectToAction(nameof(Index));
        }

        // Generar un secreto NUEVO en cada visita a la vista de setup.
        // Si el usuario abandona el flujo sin confirmar, el secreto se descarta.
        var secreto = _mfaService.GenerarSecreto();
        var uri     = _mfaService.ConstruirUriProvisionamiento(usuario.Email!, secreto);
        var qrPng   = _mfaService.GenerarQrPng(uri);

        // Guardamos el secreto PENDIENTE (encriptado) en TempData para
        // recuperarlo al hacer POST sin persistir aún en BD.
        TempData["SecretoPendiente"] = _dataProtector.Protect(secreto);

        var modelo = new ConfigurarMfaViewModel
        {
            Secreto       = FormatearSecretoParaLectura(secreto),
            QrCodeDataUri = $"data:image/png;base64,{Convert.ToBase64String(qrPng)}"
        };

        return View(modelo);
    }

    // =========================================================================
    // POST /Manage/ConfigurarMfa — Verificar primer código y activar MFA
    // =========================================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfigurarMfa(ConfigurarMfaViewModel modelo)
    {
        if (!ModelState.IsValid) return View(modelo);

        var usuario = await _userManager.GetUserAsync(User);
        if (usuario is null) return Challenge();

        // Recuperar el secreto generado en el GET
        if (TempData["SecretoPendiente"] is not string secretoProtegido)
        {
            TempData["Error"] = "Sesión de configuración expirada. Intente de nuevo.";
            return RedirectToAction(nameof(ConfigurarMfa));
        }

        string secreto;
        try
        {
            secreto = _dataProtector.Unprotect(secretoProtegido);
        }
        catch (CryptographicException)
        {
            // Token inválido, reutilizado, o corrupto
            TempData["Error"] = "Sesión de configuración inválida. Intente de nuevo.";
            return RedirectToAction(nameof(ConfigurarMfa));
        }

        // ✅ Verificar el código antes de persistir
        if (!_mfaService.VerificarCodigo(secreto, modelo.Codigo))
        {
            ModelState.AddModelError(nameof(modelo.Codigo),
                "El código no es válido. Verifique la hora de su teléfono y vuelva a intentar.");

            // Re-generar la vista con el MISMO secreto (sino rompemos el flujo)
            var uri   = _mfaService.ConstruirUriProvisionamiento(usuario.Email!, secreto);
            var qrPng = _mfaService.GenerarQrPng(uri);
            TempData["SecretoPendiente"] = _dataProtector.Protect(secreto);
            modelo.Secreto       = FormatearSecretoParaLectura(secreto);
            modelo.QrCodeDataUri = $"data:image/png;base64,{Convert.ToBase64String(qrPng)}";
            return View(modelo);
        }

        // Generar códigos de respaldo
        var codigosPlanos = _mfaService.GenerarCodigosRespaldo();
        var hashesCodigos = codigosPlanos.Select(HashCodigoRespaldo).ToList();

        // Persistir TODO en una sola transacción
        usuario.TotpSecret         = _dataProtector.Protect(secreto);
        usuario.TotpEnabled        = true;
        usuario.RecoveryCodesHash  = JsonSerializer.Serialize(hashesCodigos);
        usuario.MfaEnabledOn       = DateTime.UtcNow;

        await _userManager.UpdateAsync(usuario);

        // Registrar en la tabla de auditoría (la misma que consumirá el dashboard A09)
        _db.AuditLogs.Add(new AuditLog
        {
            EventType   = "MFA_ENABLED",
            Description = $"MFA activado por {usuario.Email}",
            IpAddress   = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
            Success     = true,
            Timestamp   = DateTime.UtcNow,
            UserId      = usuario.Id
        });
        await _db.SaveChangesAsync();

        _logger.LogInformation("MFA activado para usuario {Email}", usuario.Email);

        // Pasar los códigos en claro a la siguiente vista (ÚNICA VEZ que se muestran)
        TempData["CodigosRespaldo"] = JsonSerializer.Serialize(codigosPlanos);
        return RedirectToAction(nameof(CodigosRespaldo));
    }

    // =========================================================================
    // GET /Manage/CodigosRespaldo — Mostrar códigos una sola vez
    // =========================================================================
    [HttpGet]
    public IActionResult CodigosRespaldo()
    {
        if (TempData["CodigosRespaldo"] is not string json)
        {
            TempData["Error"] = "Los códigos de respaldo solo se muestran una vez. " +
                                "Si los perdió, regenere en Configuración de MFA.";
            return RedirectToAction(nameof(Index));
        }

        var codigos = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        return View(new CodigosRespaldoViewModel { Codigos = codigos });
    }

    // =========================================================================
    // POST /Manage/DeshabilitarMfa — Requiere confirmación y registra auditoría
    // =========================================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeshabilitarMfa()
    {
        var usuario = await _userManager.GetUserAsync(User);
        if (usuario is null) return Challenge();

        usuario.TotpSecret         = null;
        usuario.TotpEnabled        = false;
        usuario.RecoveryCodesHash  = null;
        usuario.MfaEnabledOn       = null;

        await _userManager.UpdateAsync(usuario);

        _db.AuditLogs.Add(new AuditLog
        {
            EventType   = "MFA_DISABLED",
            Description = $"MFA desactivado por {usuario.Email}",
            IpAddress   = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
            Success     = true,
            Timestamp   = DateTime.UtcNow,
            UserId      = usuario.Id
        });
        await _db.SaveChangesAsync();

        _logger.LogWarning("MFA DESACTIVADO para usuario {Email}", usuario.Email);

        TempData["Mensaje"] = "El MFA fue desactivado. Se recomienda reactivarlo a la brevedad.";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Index() => View();

    // =========================================================================
    // Helpers privados
    // =========================================================================

    /// <summary>Formatea el secreto en grupos de 4 para lectura humana.</summary>
    private static string FormatearSecretoParaLectura(string secreto)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < secreto.Length; i += 4)
        {
            if (i > 0) sb.Append(' ');
            sb.Append(secreto.AsSpan(i, Math.Min(4, secreto.Length - i)));
        }
        return sb.ToString();
    }

    /// <summary>
    /// Hashea un código de respaldo con SHA-256. Los códigos son de alta entropía,
    /// por lo que SHA-256 sin sal es aceptable (a diferencia de las contraseñas).
    /// NUNCA guardar los códigos en texto plano.
    /// </summary>
    private static string HashCodigoRespaldo(string codigo)
    {
        var bytes = Encoding.UTF8.GetBytes(codigo);
        var hash  = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
