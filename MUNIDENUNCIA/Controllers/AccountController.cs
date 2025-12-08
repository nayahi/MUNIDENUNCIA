using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Win32;
using MUNIDENUNCIA.Data;
using MUNIDENUNCIA.Models;
using MUNIDENUNCIA.Services;
using MUNIDENUNCIA.ViewModels;
using Serilog;

namespace MUNIDENUNCIA.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            IEmailService emailService,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        #region Login
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                ?? "Unknown";
            //La captura de dirección IP y user agent proporciona información valiosa para análisis de seguridad.
            //Si se detectan múltiples intentos fallidos desde diferentes IPs, podría indicar un ataque distribuido.
            //Si todos los intentos vienen de la misma IP, podría ser un ataque dirigido o simplemente un usuario legítimo
            //que olvidó su contraseña.

            var userAgent = Request.Headers["User-Agent"].ToString();

            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                isPersistent: model.RememberMe,
                lockoutOnFailure: true);
            //El parámetro lockoutOnFailure en PasswordSignInAsync es crucial porque habilita el conteo automático de
            //intentos fallidos y el bloqueo de cuenta. Sin este parámetro, el bloqueo configurado en Program.cs
            //no tendría efecto.

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (result.Succeeded)
            {
                if (user != null)
                {
                    user.UltimoAcceso = DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);

                    await RegistrarEventoAuditoria(
                        user.Id,
                        "Login",
                        $"Inicio de sesión exitoso para {model.Email}",
                        ipAddress,
                        userAgent,
                        true);
                }

                _logger.LogInformation(
                    "Usuario {Email} inició sesión desde {IP}",
                    model.Email,
                    ipAddress);

                return RedirectToLocal(model.ReturnUrl);
            }

            if (result.IsLockedOut)
            {
                if (user != null)
                {
                    await RegistrarEventoAuditoria(
                        user.Id,
                        "LoginBlocked",
                        $"Intento de acceso a cuenta bloqueada {model.Email}",
                        ipAddress,
                        userAgent,
                        false);
                }

                _logger.LogWarning(
                    "Intento de acceso a cuenta bloqueada: {Email} desde {IP}",
                    model.Email,
                    ipAddress);

                return View("Lockout");
            }

            if (user != null)
            {
                await RegistrarEventoAuditoria(
                    user.Id,
                    "LoginFailed",
                    $"Intento fallido para {model.Email}",
                    ipAddress,
                    userAgent,
                    false);

                var accessFailedCount = await _userManager
                    .GetAccessFailedCountAsync(user);
                var maxAttempts = _userManager.Options.Lockout
                    .MaxFailedAccessAttempts;
                var remainingAttempts = maxAttempts - accessFailedCount;

                _logger.LogWarning(
                    "Login fallido para {Email} desde {IP}. Restantes: {Remaining}",
                    model.Email,
                    ipAddress,
                    remainingAttempts);

                if (remainingAttempts <= 2)
                {
                    ModelState.AddModelError(string.Empty,
                        $"Credenciales inválidas. Quedan {remainingAttempts} " +
                        $"intentos antes del bloqueo.");
                }
                else
                {
                    ModelState.AddModelError(string.Empty,
                        "Credenciales inválidas.");
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Credenciales inválidas.");
            }

            return View(model);
        }
        #endregion Login

        #region Register
        /*
         * Authorize con Roles restringe el acceso al método solo a usuarios autenticados que tengan el rol especificado. 
         * Esta es una forma de autorización declarativa muy clara y fácil de mantener
         * */
        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                NombreCompleto = model.NombreCompleto,
                Cedula = model.Cedula,
                Departamento = model.Departamento,
                FechaRegistro = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, model.Role);

                //El token de confirmación de email es generado por Identity usando un algoritmo criptográfico
                //que asegura que solo el enlace enviado al email del usuario puede confirmar esa cuenta específica.
                //El token tiene una validez temporal limitada, típicamente 24 horas, después de las cuales expira
                //por seguridad.
                var token = await _userManager
                    .GenerateEmailConfirmationTokenAsync(user);
                var callbackUrl = Url.Action(
                    "ConfirmEmail",
                    "Account",
                    new { userId = user.Id, token = token },
                    protocol: Request.Scheme);

                await _emailService.SendEmailAsync(
                    model.Email,
                    "Confirmación de cuenta - MUNIDENUNCIA",
                    $"Confirme su cuenta: {callbackUrl}");

                var currentUser = await _userManager.GetUserAsync(User);
                await RegistrarEventoAuditoria(
                    currentUser!.Id,
                    "UserCreated",
                    $"Usuario {model.Email} creado con rol {model.Role}",
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                    Request.Headers["User-Agent"].ToString(),
                    true);

                _logger.LogInformation(
                    "Usuario {Email} registrado por {AdminEmail}",
                    model.Email,
                    currentUser.Email);

                TempData["SuccessMessage"] =
                    "Usuario registrado. Email de confirmación enviado.";
                return RedirectToAction("Register");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }
        #endregion Register

        #region AccesoDenegado
        [HttpGet]
        public async Task<IActionResult> AccessDenied(string? returnUrl = null)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                await RegistrarEventoAuditoria(
                    user.Id,
                    "AccessDenied",
                    $"Intento de acceso denegado a recurso: {returnUrl ?? "desconocido"}",
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                    Request.Headers["User-Agent"].ToString(),
                    false);

                _logger.LogWarning(
                    "Acceso denegado para usuario {Email} a recurso {Resource}",
                    user.Email,
                    returnUrl ?? "desconocido");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }
        #endregion AccesoDenegado

        #region Utilitarios
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"No se encontró usuario con ID '{userId}'.");
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (result.Succeeded)
            {
                await RegistrarEventoAuditoria(
                    user.Id,
                    "EmailConfirmed",
                    $"Email confirmado para {user.Email}",
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                    Request.Headers["User-Agent"].ToString(),
                    true);
            }

            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                await RegistrarEventoAuditoria(
                    user.Id,
                    "Logout",
                    $"Cierre de sesión para {user.Email}",
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                    Request.Headers["User-Agent"].ToString(),
                    true);

                _logger.LogInformation("Usuario {Email} cerró sesión", user.Email);
            }

            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        /*
         * El método RedirectToLocal es un control de seguridad importante que previene ataques de open redirect 
         * donde un atacante podría crear un enlace malicioso que aparenta ser del sitio legítimo pero redirige 
         * a un sitio de phishing después del login.
         * */
        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        private async Task RegistrarEventoAuditoria(
            string userId,
            string eventType,
            string description,
            string ipAddress,
            string userAgent,
            bool success)
        {
            var log = new AuditLog
            {
                UserId = userId,
                EventType = eventType,
                Description = description,
                Timestamp = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Success = success
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        #endregion Utilitarios privados
    }
}

//UserManager proporciona métodos para gestionar usuarios como creación, actualización y búsqueda.
//SignInManager maneja las operaciones de inicio y cierre de sesión.
//ApplicationDbContext permite acceso directo a la base de datos cuando se necesita,
////como al registrar eventos de auditoría