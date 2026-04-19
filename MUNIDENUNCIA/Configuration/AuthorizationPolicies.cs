// =============================================================================
// AuthorizationPolicies.cs
// Semana 2 - MEJORA: Políticas de Autorización Centralizadas
// Ubicación sugerida: Configuration/AuthorizationPolicies.cs
// =============================================================================
// CONTEXTO PEDAGÓGICO:
// MuniDenuncia actualmente usa [Authorize] a nivel de clase y
// User.IsInRole("Ciudadano") inline en cada método. Esto funciona, pero
// tiene dos desventajas:
//   1. La lógica de autorización está dispersa en los controladores.
//   2. Cambiar un rol requiere modificar múltiples archivos.
//
// La práctica recomendada en ASP.NET Core 8 es CENTRALIZAR las reglas
// de autorización en políticas nombradas, registradas en Program.cs
// y aplicadas con [Authorize(Policy = "NombrePolitica")].
//
// Esta clase es una extensión que organiza el registro de políticas
// fuera de Program.cs para mantener el archivo principal legible.
// =============================================================================

using Microsoft.AspNetCore.Authorization;

namespace MUNIDENUNCIA.Configuration;

/// <summary>
/// Extensiones para registrar políticas de autorización de MuniDenuncia.
/// Centraliza todas las reglas de acceso por rol en un solo lugar.
/// </summary>
public static class AuthorizationPoliciesExtensions
{
    // =========================================================================
    // Nombres de políticas (constantes para evitar errores de tipeo)
    // =========================================================================
    public const string RequiereAdmin       = "RequiereAdmin";
    public const string RequiereFuncionario = "RequiereFuncionario";
    public const string RequiereCiudadano   = "RequiereCiudadano";
    public const string PersonalInterno     = "PersonalInterno";

    /// <summary>
    /// Registra todas las políticas de autorización de MuniDenuncia.
    /// Llamar desde Program.cs antes de builder.Build().
    /// </summary>
    public static IServiceCollection AddMuniDenunciaAuthorizationPolicies(
        this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // ─────────────────────────────────────────────────────────────
            // Política: Solo Administradores
            // Usado en: gestión de usuarios, configuración del sistema,
            //          reportes globales, limpieza de datos.
            // ─────────────────────────────────────────────────────────────
            options.AddPolicy(RequiereAdmin, policy =>
                policy.RequireRole("Administrador"));

            // ─────────────────────────────────────────────────────────────
            // Política: Solo Funcionarios (o Administradores)
            // Usado en: ver todas las denuncias, descifrar datos sensibles,
            //          aprobar/rechazar solicitudes de permisos.
            // RequireRole con múltiples roles funciona como OR lógico.
            // ─────────────────────────────────────────────────────────────
            options.AddPolicy(RequiereFuncionario, policy =>
                policy.RequireRole("Funcionario", "Administrador"));

            // ─────────────────────────────────────────────────────────────
            // Política: Solo Ciudadanos autenticados
            // Usado en: crear denuncias, ver denuncias propias,
            //          actualizar datos de contacto propios.
            // ─────────────────────────────────────────────────────────────
            options.AddPolicy(RequiereCiudadano, policy =>
                policy.RequireRole("Ciudadano"));

            // ─────────────────────────────────────────────────────────────
            // Política avanzada: Personal Interno con claim adicional
            // Demuestra composición con RequireAssertion para lógica
            // más compleja que un simple role check.
            // Ejemplo: solo funcionarios del departamento de "Obras"
            // con claim "Departamento" específico.
            // ─────────────────────────────────────────────────────────────
            options.AddPolicy(PersonalInterno, policy =>
                policy.RequireAssertion(context =>
                    context.User.IsInRole("Funcionario") ||
                    context.User.IsInRole("Administrador")));
        });

        return services;
    }
}

// =============================================================================
// USO EN CONTROLADORES (ejemplos de aplicación):
// =============================================================================
//
// // Nivel de controlador completo:
// [Authorize(Policy = AuthorizationPoliciesExtensions.RequiereFuncionario)]
// public class GestionDenunciasController : Controller { ... }
//
// // Nivel de método específico:
// [HttpPost]
// [Authorize(Policy = AuthorizationPoliciesExtensions.RequiereAdmin)]
// public IActionResult EliminarDenuncia(int id) { ... }
//
// // Combinado con verificación inline (defensa en profundidad):
// [Authorize(Policy = AuthorizationPoliciesExtensions.RequiereCiudadano)]
// public async Task<IActionResult> Details(int? id)
// {
//     var denuncia = await _context.Denuncias.FindAsync(id);
//     // Aun con la política, verificamos propiedad del recurso (anti-IDOR):
//     var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
//     if (denuncia.UsuarioId != userId) return Forbid();
//     return View(denuncia);
// }
//
// =============================================================================
