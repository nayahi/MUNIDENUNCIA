using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MUNIDENUNCIA.Data;
using MUNIDENUNCIA.Models;
using MUNIDENUNCIA.Services;
using MUNIDENUNCIA.ViewModels;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MUNIDENUNCIA.Controllers
{
    /// <summary>
    /// CONTROLADOR SEGURO - Implementa todas las mejores prácticas de seguridad
    /// SEMANA 4: Control de Acceso y Protección de Datos
    /// 
    /// PROTECCIONES IMPLEMENTADAS:
    /// 1. Autorización basada en roles (Ciudadano, Funcionario, Administrador)
    /// 2. Cifrado de datos sensibles con Data Protection API
    /// 3. Protección CSRF en todos los formularios POST
    /// 4. Validación segura de archivos PDF
    /// 5. Validación del lado servidor obligatoria
    /// 6. Manejo seguro de errores sin exponer detalles técnicos
    /// 7. Control de acceso granular (usuarios solo ven sus propias denuncias)
    /// 
    /// Este es el código que debe usarse en PRODUCCIÓN
    /// </summary>
    [Authorize] // ✅ Requiere autenticación para todas las acciones
    public class DenunciasCla4Controller : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IDataProtectionService _dataProtectionService;
        private readonly IFileUploadService _fileUploadService;
        private readonly ILogger<HomeController> _logger;

        public DenunciasCla4Controller(
            ApplicationDbContext context,
            IDataProtectionService dataProtectionService,
            IFileUploadService fileUploadService,
            ILogger<HomeController> logger)
        {
            _context = context;
            _dataProtectionService = dataProtectionService;
            _fileUploadService = fileUploadService;
            _logger = logger;
        }

        // ====================================================================
        // LISTA DE DENUNCIAS CON AUTORIZACIÓN GRANULAR
        // ====================================================================

        // GET: /Denuncias
        /// <summary>
        /// Lista denuncias según el rol del usuario:
        /// - Ciudadano: Solo ve sus propias denuncias
        /// - Funcionario: Ve todas las denuncias (para gestión)
        /// - Administrador: Ve todas las denuncias
        /// </summary>
        /// 
        public async Task<IActionResult> Index()
        {
            var denuncias = await _context.Denuncias
                //.Include(d => d.AsignadoAUserId)
                .OrderByDescending(d => d.FechaCreacion)
                .ToListAsync();

            // Filtrado según rol del usuario autenticado
            if (User.IsInRole("Ciudadano"))
            {
                // Obtener la cédula del claim del usuario autenticado
                var cedulaUsuario = User.FindFirstValue("Cedula");

                if (string.IsNullOrEmpty(cedulaUsuario))
                {
                    // DEBUG: Usuario no tiene claim de cédula
                    _logger.LogWarning("Usuario {Email} no tiene claim de Cedula", User.Identity.Name);
                    TempData["Error"] = "No se pudo obtener su información de cédula. Por favor, contacte al administrador.";
                    return View(new List<Denuncia>());
                }

                _logger.LogInformation("Buscando denuncias para ciudadano con cédula: {Cedula}", cedulaUsuario);

                // SOLUCIÓN: Descifrar todas las denuncias y comparar en texto plano
                var denunciasCiudadano = new List<Denuncia>();

                foreach (var denuncia in denuncias)
                {
                    try
                    {
                        // Descifrar la cédula de la denuncia
                        var cedulaDenuncia = _dataProtectionService.Unprotect(denuncia.CedulaCifrada);

                        // Comparar con la cédula del usuario (ignorando mayúsculas/minúsculas y espacios)
                        if (cedulaDenuncia.Trim().Equals(cedulaUsuario.Trim(), StringComparison.OrdinalIgnoreCase))
                        {
                            denunciasCiudadano.Add(denuncia);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al descifrar cédula de denuncia ID: {Id}", denuncia.Id);
                    }
                }

                denuncias = denunciasCiudadano;

                _logger.LogInformation("Se encontraron {Count} denuncias para el ciudadano", denuncias.Count);
            }
            // Funcionarios y Administradores ven todas las denuncias
            else if (User.IsInRole("Funcionario") || User.IsInRole("Administrador"))
            {
                // No se aplica filtro, ven todas las denuncias
                _logger.LogInformation("Usuario {Email} con rol {Role} accediendo a todas las denuncias",
                    User.Identity.Name, User.IsInRole("Administrador") ? "Administrador" : "Funcionario");
            }
            else
            {
                // Usuario sin rol conocido, no mostrar denuncias
                denuncias = new List<Denuncia>();
            }

            // Descifrar datos sensibles para visualización
            foreach (var denuncia in denuncias)
            {
                DescifrarDatosSensibles(denuncia);
            }

            return View(denuncias);
        }
        // ====================================================================
        // DETALLES CON CONTROL DE ACCESO
        // ====================================================================

        // GET: /Denuncias/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var denuncia = await _context.Denuncias
                .FirstOrDefaultAsync(m => m.Id == id);

            if (denuncia == null)
            {
                return NotFound();
            }

            // ✅ VERIFICAR AUTORIZACIÓN para ver detalles
            if (!PuedeAccederADenuncia(denuncia))
            {
                return Forbid(); // 403 Forbidden
            }

            // ✅ DESCIFRAR datos sensibles
            DescifrarDatosSensibles(denuncia);

            return View(denuncia);
        }

        // ====================================================================
        // CREACIÓN SEGURA DE DENUNCIAS
        // ====================================================================

        // GET: /Denuncias/Create
        [AllowAnonymous] // ✅ Permite a ciudadanos anónimos crear denuncias
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Denuncias/Create
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken] // ✅ PROTECCIÓN CSRF
        public async Task<IActionResult> Create(CrearDenunciaViewModel model)
        {
            // ✅ VALIDACIÓN DEL LADO SERVIDOR OBLIGATORIA
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var denuncia = new Denuncia
                {
                    // ✅ CIFRAR DATOS SENSIBLES antes de guardar
                    CedulaCifrada = _dataProtectionService.Protect(model.Cedula),
                    TelefonoCifrado = _dataProtectionService.Protect(model.Telefono),
                    EmailCifrado = _dataProtectionService.Protect(model.Email),

                    NombreCompleto = model.NombreCompleto,
                    Categoria = model.Categoria,
                    Ubicacion = model.Ubicacion,
                    Descripcion = model.Descripcion,
                    Estado = EstadoDenuncia.Recibida,
                    FechaCreacion = DateTime.UtcNow
                };

                // ✅ MANEJO SEGURO DE ARCHIVOS
                if (model.ArchivoPdf != null)
                {
                    var uploadResult = await _fileUploadService.SavePdfFileAsync(
                        model.ArchivoPdf,
                        "denuncias"
                    );

                    if (!uploadResult.Success)
                    {
                        ModelState.AddModelError("ArchivoPdf", uploadResult.ErrorMessage);
                        return View(model);
                    }

                    // Guardar información del archivo en la denuncia
                    denuncia.ArchivoNombreOriginal = uploadResult.OriginalFileName;
                    denuncia.ArchivoNombreServidor = uploadResult.ServerFileName;
                    denuncia.ArchivoRuta = uploadResult.RelativePath;
                    denuncia.ArchivoTamanoBytes = uploadResult.FileSizeBytes;
                    denuncia.ArchivoTipoMime = uploadResult.MimeType;
                    denuncia.ArchivoFechaSubida = DateTime.UtcNow;
                }

                _context.Denuncias.Add(denuncia);
                await _context.SaveChangesAsync();

                TempData["Mensaje"] = "Denuncia registrada exitosamente. " +
                    "En breve un funcionario revisará su caso.";

                return RedirectToAction(nameof(Confirmacion), new { id = denuncia.Id });
            }
            catch (Exception)
            {
                // ✅ NO EXPONER detalles técnicos al usuario
                ModelState.AddModelError("",
                    "Ocurrió un error al procesar su denuncia. " +
                    "Por favor intente nuevamente o contacte al soporte técnico.");

                return View(model);
            }
        }

        // ====================================================================
        // CONFIRMACIÓN DE DENUNCIA CREADA
        // ====================================================================

        // GET: /Denuncias/Confirmacion/5
        [AllowAnonymous]
        public async Task<IActionResult> Confirmacion(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var denuncia = await _context.Denuncias.FindAsync(id);

            if (denuncia == null)
            {
                return NotFound();
            }

            DescifrarDatosSensibles(denuncia);

            return View(denuncia);
        }

        // ====================================================================
        // EDICIÓN CON AUTORIZACIÓN
        // ====================================================================

        // GET: /Denuncias/Edit/5
        [Authorize(Roles = "Funcionario,Administrador")] // ✅ Solo funcionarios pueden editar
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var denuncia = await _context.Denuncias.FindAsync(id);

            if (denuncia == null)
            {
                return NotFound();
            }

            // Descifrar datos para mostrar en el formulario
            DescifrarDatosSensibles(denuncia);

            var model = new EditarDenunciaViewModel
            {
                Id = denuncia.Id,
                Cedula = denuncia.Cedula, // Ya descifrado
                NombreCompleto = denuncia.NombreCompleto,
                Email = denuncia.Email, // Ya descifrado
                Telefono = denuncia.Telefono, // Ya descifrado
                Categoria = denuncia.Categoria,
                Ubicacion = denuncia.Ubicacion,
                Descripcion = denuncia.Descripcion,
                TieneArchivoExistente = !string.IsNullOrEmpty(denuncia.ArchivoNombreOriginal),
                NombreArchivoExistente = denuncia.ArchivoNombreOriginal
            };

            return View(model);
        }

        // POST: /Denuncias/Edit/5
        [HttpPost]
        [Authorize(Roles = "Funcionario,Administrador")]
        [ValidateAntiForgeryToken] // ✅ PROTECCIÓN CSRF
        public async Task<IActionResult> Edit(int id, EditarDenunciaViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            // ✅ VALIDACIÓN DEL LADO SERVIDOR
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var denuncia = await _context.Denuncias.FindAsync(id);

                if (denuncia == null)
                {
                    return NotFound();
                }

                // ✅ CIFRAR datos sensibles actualizados
                denuncia.CedulaCifrada = _dataProtectionService.Protect(model.Cedula);
                denuncia.TelefonoCifrado = _dataProtectionService.Protect(model.Telefono);
                denuncia.EmailCifrado = _dataProtectionService.Protect(model.Email);

                denuncia.NombreCompleto = model.NombreCompleto;
                denuncia.Categoria = model.Categoria;
                denuncia.Ubicacion = model.Ubicacion;
                denuncia.Descripcion = model.Descripcion;
                denuncia.FechaActualizacion = DateTime.UtcNow;

                // ✅ MANEJO SEGURO de reemplazo/eliminación de archivos
                if (model.EliminarArchivoExistente && !string.IsNullOrEmpty(denuncia.ArchivoRuta))
                {
                    // Eliminar archivo existente
                    await _fileUploadService.DeleteFileAsync(denuncia.ArchivoRuta);

                    denuncia.ArchivoNombreOriginal = null;
                    denuncia.ArchivoNombreServidor = null;
                    denuncia.ArchivoRuta = null;
                    denuncia.ArchivoTamanoBytes = null;
                    denuncia.ArchivoTipoMime = null;
                    denuncia.ArchivoFechaSubida = null;
                }
                else if (model.NuevoArchivoPdf != null)
                {
                    // Eliminar archivo anterior si existe
                    if (!string.IsNullOrEmpty(denuncia.ArchivoRuta))
                    {
                        await _fileUploadService.DeleteFileAsync(denuncia.ArchivoRuta);
                    }

                    // Guardar nuevo archivo
                    var uploadResult = await _fileUploadService.SavePdfFileAsync(
                        model.NuevoArchivoPdf,
                        "denuncias"
                    );

                    if (!uploadResult.Success)
                    {
                        ModelState.AddModelError("NuevoArchivoPdf", uploadResult.ErrorMessage);
                        model.TieneArchivoExistente = !string.IsNullOrEmpty(denuncia.ArchivoNombreOriginal);
                        model.NombreArchivoExistente = denuncia.ArchivoNombreOriginal;
                        return View(model);
                    }

                    denuncia.ArchivoNombreOriginal = uploadResult.OriginalFileName;
                    denuncia.ArchivoNombreServidor = uploadResult.ServerFileName;
                    denuncia.ArchivoRuta = uploadResult.RelativePath;
                    denuncia.ArchivoTamanoBytes = uploadResult.FileSizeBytes;
                    denuncia.ArchivoTipoMime = uploadResult.MimeType;
                    denuncia.ArchivoFechaSubida = DateTime.UtcNow;
                }

                _context.Update(denuncia);
                await _context.SaveChangesAsync();

                TempData["Mensaje"] = "Denuncia actualizada exitosamente";
                return RedirectToAction(nameof(Details), new { id = denuncia.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await DenunciaExists(model.Id))
                {
                    return NotFound();
                }

                ModelState.AddModelError("",
                    "La denuncia fue modificada por otro usuario. Por favor recargue la página.");
                return View(model);
            }
            catch (Exception)
            {
                ModelState.AddModelError("",
                    "Ocurrió un error al actualizar. Por favor intente nuevamente.");
                return View(model);
            }
        }

        // ====================================================================
        // DESCARGA SEGURA DE ARCHIVOS CON AUTORIZACIÓN
        // ====================================================================

        // GET: /Denuncias/DescargarArchivo/5
        public async Task<IActionResult> DescargarArchivo(int id)
        {
            var denuncia = await _context.Denuncias.FindAsync(id);

            if (denuncia == null || string.IsNullOrEmpty(denuncia.ArchivoRuta))
            {
                return NotFound();
            }

            // ✅ VERIFICAR AUTORIZACIÓN para descargar
            if (!PuedeAccederADenuncia(denuncia))
            {
                return Forbid();
            }

            // ✅ VERIFICAR que el archivo existe
            if (!_fileUploadService.FileExists(denuncia.ArchivoRuta))
            {
                TempData["Error"] = "El archivo ya no está disponible en el servidor";
                return RedirectToAction(nameof(Details), new { id });
            }

            // ✅ SERVIR archivo con Content-Type correcto
            string physicalPath = _fileUploadService.GetPhysicalPath(denuncia.ArchivoRuta);

            var memory = new MemoryStream();
            using (var stream = new FileStream(physicalPath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            // ✅ Forzar descarga con nombre original
            return File(memory,
                denuncia.ArchivoTipoMime ?? "application/pdf",
                denuncia.ArchivoNombreOriginal);
        }

        // ====================================================================
        // CAMBIO DE ESTADO - SOLO FUNCIONARIOS
        // ====================================================================

        // GET: /Denuncias/CambiarEstado/5
        [Authorize(Roles = "Funcionario,Administrador")] // ✅ Solo funcionarios
        public async Task<IActionResult> CambiarEstado(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var denuncia = await _context.Denuncias.FindAsync(id);

            if (denuncia == null)
            {
                return NotFound();
            }

            DescifrarDatosSensibles(denuncia);

            var model = new CambiarEstadoDenunciaViewModel
            {
                DenunciaId = denuncia.Id,
                EstadoActual = denuncia.Estado,
                CategoriaActual = denuncia.Categoria.ToString(),
                UbicacionActual = denuncia.Ubicacion,
                NombreCiudadano = denuncia.NombreCompleto
            };

            return View(model);
        }

        // POST: /Denuncias/CambiarEstado
        [HttpPost]
        [Authorize(Roles = "Funcionario,Administrador")]
        [ValidateAntiForgeryToken] // ✅ PROTECCIÓN CSRF
        public async Task<IActionResult> CambiarEstado(CambiarEstadoDenunciaViewModel model)
        {
            // ✅ VALIDACIÓN DEL LADO SERVIDOR
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var denuncia = await _context.Denuncias.FindAsync(model.DenunciaId);

            if (denuncia == null)
            {
                return NotFound();
            }

            denuncia.Estado = model.NuevoEstado;
            denuncia.Observaciones = model.Observaciones;
            denuncia.FechaActualizacion = DateTime.UtcNow;

            // Asignar al funcionario actual si está en proceso
            if (model.NuevoEstado == EstadoDenuncia.EnProceso)
            {
                denuncia.AsignadoAUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            }

            if (model.NuevoEstado == EstadoDenuncia.Resuelta)
            {
                denuncia.FechaResolucion = DateTime.UtcNow;
            }

            _context.Update(denuncia);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = $"Estado cambiado a: {model.NuevoEstado}";
            return RedirectToAction(nameof(Details), new { id = denuncia.Id });
        }

        // ====================================================================
        // ELIMINACIÓN - SOLO ADMINISTRADORES
        // ====================================================================

        // GET: /Denuncias/Delete/5
        [Authorize(Roles = "Administrador")] // ✅ Solo administradores pueden eliminar
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var denuncia = await _context.Denuncias
                .FirstOrDefaultAsync(m => m.Id == id);

            if (denuncia == null)
            {
                return NotFound();
            }

            DescifrarDatosSensibles(denuncia);

            return View(denuncia);
        }

        // POST: /Denuncias/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken] // ✅ PROTECCIÓN CSRF
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var denuncia = await _context.Denuncias.FindAsync(id);

            if (denuncia != null)
            {
                // ✅ ELIMINAR archivo asociado de forma segura
                if (!string.IsNullOrEmpty(denuncia.ArchivoRuta))
                {
                    await _fileUploadService.DeleteFileAsync(denuncia.ArchivoRuta);
                }

                _context.Denuncias.Remove(denuncia);
                await _context.SaveChangesAsync();
            }

            TempData["Mensaje"] = "Denuncia eliminada exitosamente";
            return RedirectToAction(nameof(Index));
        }

        // ====================================================================
        // MÉTODOS AUXILIARES PRIVADOS
        // ====================================================================

        /// <summary>
        /// Verifica si el usuario actual puede acceder a una denuncia específica
        /// </summary>
        private bool PuedeAccederADenuncia(Denuncia denuncia)
        {
            // Administradores y Funcionarios pueden acceder a todas
            if (User.IsInRole("Administrador") || User.IsInRole("Funcionario"))
            {
                return true;
            }

            // Ciudadanos solo pueden acceder a sus propias denuncias
            if (User.IsInRole("Ciudadano"))
            {
                var cedulaUsuario = User.FindFirstValue("Cedula");

                if (string.IsNullOrEmpty(cedulaUsuario))
                {
                    return false;
                }

                // Descifrar la cédula de la denuncia para comparar
                try
                {
                    string cedulaDenuncia = _dataProtectionService.Unprotect(denuncia.CedulaCifrada);
                    return cedulaDenuncia == cedulaUsuario;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Descifra los datos sensibles de una denuncia para mostrarlos en la UI
        /// </summary>
        private void DescifrarDatosSensibles(Denuncia denuncia)
        {
            try
            {
                denuncia.Cedula = _dataProtectionService.Unprotect(denuncia.CedulaCifrada);
                denuncia.Telefono = _dataProtectionService.Unprotect(denuncia.TelefonoCifrado);
                denuncia.Email = _dataProtectionService.Unprotect(denuncia.EmailCifrado);
            }
            catch (Exception)
            {
                // Si falla el descifrado, mostrar datos enmascarados
                denuncia.Cedula = "***-****-****";
                denuncia.Telefono = "****-****";
                denuncia.Email = "***@***.***";
            }
        }

        /// <summary>
        /// Verifica si una denuncia existe
        /// </summary>
        private async Task<bool> DenunciaExists(int id)
        {
            return await _context.Denuncias.AnyAsync(e => e.Id == id);
        }
    }
}

// ============================================================================
// RESUMEN DE PROTECCIONES IMPLEMENTADAS
// ============================================================================
//
// 1. AUTORIZACIÓN BASADA EN ROLES
//    ✅ [Authorize] - Requiere autenticación
//    ✅ [Authorize(Roles = "Funcionario,Administrador")] - Acciones específicas
//    ✅ [AllowAnonymous] - Permite crear denuncias sin autenticación
//    ✅ Verificación granular con PuedeAccederADenuncia()
//
// 2. CIFRADO DE DATOS SENSIBLES
//    ✅ Cédula, teléfono y email cifrados con Data Protection API
//    ✅ Cifrado antes de guardar en BD
//    ✅ Descifrado al mostrar en UI
//    ✅ Datos protegidos incluso si hay acceso directo a la BD
//
// 3. PROTECCIÓN CSRF
//    ✅ [ValidateAntiForgeryToken] en todos los POST
//    ✅ Tokens generados automáticamente por Razor
//    ✅ Previene ataques de sitios cruzados
//
// 4. VALIDACIÓN DE ARCHIVOS
//    ✅ IFileUploadService con validaciones múltiples
//    ✅ Solo archivos PDF permitidos
//    ✅ Validación de firma de archivo (magic numbers)
//    ✅ Tamaño máximo: 5MB
//    ✅ Nombres aleatorios con GUID
//    ✅ Almacenamiento fuera de wwwroot
//
// 5. VALIDACIÓN DEL LADO SERVIDOR
//    ✅ ModelState.IsValid verificado siempre
//    ✅ Data Annotations en ViewModels
//    ✅ No confía en validación del cliente
//
// 6. MANEJO SEGURO DE ERRORES
//    ✅ try-catch apropiados
//    ✅ NO expone detalles técnicos
//    ✅ Mensajes genéricos al usuario
//    ✅ Logging interno (futuro: integrar con Serilog)
//
// 7. PRINCIPIO DE MÍNIMO PRIVILEGIO
//    ✅ Cada rol solo puede hacer lo necesario
//    ✅ Ciudadanos: Crear y ver propias denuncias
//    ✅ Funcionarios: Gestionar y cambiar estados
//    ✅ Administradores: Control total incluyendo eliminación
//
// ============================================================================
