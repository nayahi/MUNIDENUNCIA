using Microsoft.AspNetCore.Identity;
using MUNIDENUNCIA.Models;
using System.Security.Claims;

namespace MUNIDENUNCIA.Data
{
    /// <summary>
    /// Clase estática para inicializar roles y usuarios de prueba para el sistema MUNIDENUNCIA.
    /// Se ejecuta automáticamente al iniciar la aplicación desde Program.cs.
    /// </summary>
    public static class DbInitializer
    {
        /// <summary>
        /// Método principal que inicializa roles y usuarios de prueba.
        /// Debe llamarse desde Program.cs después de configurar la aplicación.
        /// </summary>
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                // Paso 1: Crear roles si no existen
                await CrearRolesAsync(roleManager, logger);

                // Paso 2: Crear usuarios de prueba con sus roles
                await CrearUsuariosPruebaAsync(userManager, logger);

                logger.LogInformation("=== Inicialización de base de datos completada exitosamente ===");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error durante la inicialización de la base de datos");
                throw;
            }
        }

        /// <summary>
        /// Crea los tres roles necesarios para el sistema de denuncias.
        /// </summary>
        private static async Task CrearRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
        {
            string[] roleNames = { "Administrador", "Funcionario", "Ciudadano" };

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                    if (result.Succeeded)
                    {
                        logger.LogInformation("✅ Rol '{RoleName}' creado exitosamente", roleName);
                    }
                    else
                    {
                        logger.LogError("❌ Error al crear rol '{RoleName}': {Errors}", 
                            roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    logger.LogInformation("ℹ️  Rol '{RoleName}' ya existe", roleName);
                }
            }
        }

        /// <summary>
        /// Crea tres usuarios de prueba: Administrador, Funcionario y Ciudadano.
        /// </summary>
        private static async Task CrearUsuariosPruebaAsync(UserManager<ApplicationUser> userManager, ILogger logger)
        {
            // Usuario 1: Administrador
            await CrearUsuarioConRolAsync(
                userManager,
                logger,
                email: "admin@sumunicipalidad.go.cr",
                password: "Admin@MUNIDENUNCIA2025!",
                nombreCompleto: "Carlos Administrador González",
                cedula: "1-0111-0111",
                departamento: "Tecnologías de Información",
                rol: "Administrador"
            );

            // Usuario 2: Funcionario
            await CrearUsuarioConRolAsync(
                userManager,
                logger,
                email: "funcionario@sumunicipalidad.go.cr",
                password: "Func@MUNIDENUNCIA2025!",
                nombreCompleto: "María Funcionaria Ramírez",
                cedula: "1-0222-0222",
                departamento: "Servicios Municipales",
                rol: "Funcionario"
            );

            // Usuario 3: Ciudadano
            await CrearUsuarioConRolAsync(
                userManager,
                logger,
                email: "ciudadano@email.com",
                password: "Ciuda@MUNIDENUNCIA2025!",
                nombreCompleto: "Juan Ciudadano Mora",
                cedula: "1-0333-0333",
                departamento: "N/A",
                rol: "Ciudadano"
            );
        }

        /// <summary>
        /// Método auxiliar para crear un usuario con su rol y claims personalizados.
        /// </summary>
        private static async Task CrearUsuarioConRolAsync(
            UserManager<ApplicationUser> userManager,
            ILogger logger,
            string email,
            string password,
            string nombreCompleto,
            string cedula,
            string departamento,
            string rol)
        {
            var usuarioExistente = await userManager.FindByEmailAsync(email);
            
            if (usuarioExistente == null)
            {
                var nuevoUsuario = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    NombreCompleto = nombreCompleto,
                    Departamento = departamento,
                    FechaRegistro = DateTime.UtcNow
                };

                var resultado = await userManager.CreateAsync(nuevoUsuario, password);

                if (resultado.Succeeded)
                {
                    // Asignar rol
                    await userManager.AddToRoleAsync(nuevoUsuario, rol);

                    // Agregar claims personalizados
                    var claims = new List<Claim>
                    {
                        new Claim("NombreCompleto", nombreCompleto),
                        new Claim("Cedula", cedula)
                    };
                    await userManager.AddClaimsAsync(nuevoUsuario, claims);

                    logger.LogInformation("✅ Usuario '{Email}' creado exitosamente con rol '{Rol}'", 
                        email, rol);
                }
                else
                {
                    logger.LogError("❌ Error al crear usuario '{Email}': {Errors}", 
                        email, string.Join(", ", resultado.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                logger.LogInformation("ℹ️  Usuario '{Email}' ya existe", email);
            }
        }
    }
}

// ============================================================================
// NOTAS PEDAGÓGICAS SOBRE ROLES Y AUTORIZACIÓN
// ============================================================================
//
// 1. ¿QUÉ ES UN ROL?
//    Un rol es una etiqueta que agrupa permisos y define qué puede hacer
//    un usuario en el sistema. Es más simple que gestionar permisos individuales.
//
//    Ejemplo:
//    - Rol "Funcionario" incluye: Ver denuncias, Cambiar estados
//    - Rol "Administrador" incluye: Todo lo anterior + Eliminar denuncias
//
// 2. ¿POR QUÉ USAR ROLES EN LUGAR DE PERMISOS INDIVIDUALES?
//    - Simplicidad: Más fácil de gestionar y entender
//    - Escalabilidad: Fácil agregar nuevos usuarios al sistema
//    - Mantenimiento: Cambiar permisos de un rol afecta a todos los usuarios
//
//    Para sistemas pequeños/medianos, roles son suficientes.
//    Para sistemas grandes/complejos, considerar políticas de autorización.
//
// 3. ROLES VS. CLAIMS VS. POLICIES
//    
//    ROLES:
//    - Simple: Usuario tiene o no tiene un rol
//    - Ejemplo: User.IsInRole("Administrador")
//    - Ideal para: Permisos amplios y generales
//
//    CLAIMS:
//    - Flexible: Pares clave-valor asociados al usuario
//    - Ejemplo: Claim("Departamento", "IT")
//    - Ideal para: Información adicional del usuario
//
//    POLICIES:
//    - Avanzado: Reglas complejas de autorización
//    - Ejemplo: Requiere Rol + Claim + Lógica custom
//    - Ideal para: Reglas de negocio complejas
//
// 4. ESTRUCTURA DE ROLES EN MUNIDENUNCIA
//
//    CIUDADANO:
//    - Crear denuncias (incluso sin autenticación)
//    - Ver solo SUS propias denuncias
//    - NO puede cambiar estados
//    - NO puede eliminar
//
//    FUNCIONARIO:
//    - Ver TODAS las denuncias
//    - Cambiar estados de denuncias
//    - Editar información de denuncias
//    - NO puede eliminar
//
//    ADMINISTRADOR:
//    - Todo lo anterior
//    - Eliminar denuncias
//    - Gestionar usuarios
//    - Acceso total al sistema
//
// 5. USUARIOS DE PRUEBA CREADOS
//
//    Administrador:
//    - Email: admin@sumunicipalidad.go.cr
//    - Password: Admin@MUNIDENUNCIA2025!
//    - Cédula: 1-0111-0111
//
//    Funcionario:
//    - Email: funcionario@sumunicipalidad.go.cr
//    - Password: Func123!
//    - Cédula: 1-0222-0222
//
//    Ciudadano:
//    - Email: ciudadano@email.com
//    - Password: Ciud123!
//    - Cédula: 1-0333-0333
//
// 6. CLAIMS PERSONALIZADOS
//    Agregamos claims de NombreCompleto y Cedula a cada usuario.
//    
//    Beneficios:
//    - Evita joins adicionales a la BD
//    - Información disponible en User.Claims
//    - Útil para autorización granular
//    - Ejemplo: Ciudadanos solo ven denuncias de su cédula
//
// 7. CONSIDERACIONES DE SEGURIDAD
//
//    ✅ CORRECTO:
//    - Contraseñas fuertes (Admin@MUNIDENUNCIA2025!, etc.)
//    - EmailConfirmed = true (solo para demo)
//    - Roles definidos claramente
//    - Principio de mínimo privilegio
//
//    ⚠️ IMPORTANTE PARA PRODUCCIÓN:
//    - NO crear usuarios hardcodeados
//    - Usar proceso de registro normal
//    - Requerir confirmación de email real
//    - Implementar recuperación de contraseña
//    - Auditar cambios de roles
//    - Implementar MFA para administradores
//
// 8. EXTENSIBILIDAD FUTURA
//
//    Si el sistema crece, considerar:
//    
//    - Más roles granulares:
//      * FuncionarioJunior (solo lectura)
//      * FuncionarioSenior (puede cambiar estados)
//      * Supervisor (puede reasignar denuncias)
//    
//    - Políticas personalizadas:
//      * [Authorize(Policy = "PuedeCambiarEstado")]
//      * Policy verifica: Rol + Departamento + Antigüedad
//    
//    - Permisos por módulo:
//      * Permisos de construcción
//      * Sistema de denuncias
//      * Gestión de impuestos
//
// ============================================================================
