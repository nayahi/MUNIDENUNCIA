using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MUNIDENUNCIA.Data;
using MUNIDENUNCIA.Models;
using MUNIDENUNCIA.ViewModels;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MUNIDENUNCIA.Controllers
{
    /// <summary>
    /// CONTROLADOR VULNERABLE - SOLO PARA DEMOSTRACIÓN DE RIESGOS
    /// SEMANA 4: Este controlador contiene múltiples vulnerabilidades intencionales
    /// 
    /// VULNERABILIDADES DEMOSTRADAS:
    /// 1. Sin autorización basada en roles - Cualquier usuario puede acceder
    /// 2. Sin cifrado de datos sensibles - Cédula, teléfono y email en texto plano
    /// 3. Sin protección CSRF en formularios POST
    /// 4. Sin validación segura de archivos - Acepta cualquier tipo de archivo
    /// 5. Nombres de archivo predecibles - Usa nombres originales del usuario
    /// 6. Sin validación del lado servidor - Confía en validación del cliente
    /// 
    /// ADVERTENCIA: NO USAR ESTE CÓDIGO EN PRODUCCIÓN
    /// Este código existe únicamente para demostrar los riesgos de seguridad
    /// </summary>
    public class DenunciasCla4VulnerableController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DenunciasCla4VulnerableController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ====================================================================
        // VULNERABILIDAD #1: SIN AUTORIZACIÓN
        // Cualquier usuario (incluso anónimo) puede ver todas las denuncias
        // ====================================================================
        
        // GET: /DenunciasVulnerable
        public async Task<IActionResult> Index()
        {
            // ❌ PROBLEMA: No hay verificación de roles ni permisos
            // Un ciudadano podría ver denuncias de otros ciudadanos
            // Información sensible expuesta a usuarios no autorizados
            
            var denuncias = await _context.Denuncias
                .OrderByDescending(d => d.FechaCreacion)
                .ToListAsync();

            // ❌ PROBLEMA: Los datos sensibles están en texto plano en la BD
            // Si alguien accede a la BD, verá cédulas, teléfonos y emails sin cifrar
            
            return View(denuncias);
        }

        // ====================================================================
        // VULNERABILIDAD #2: DETALLES SIN CONTROL DE ACCESO
        // ====================================================================
        
        // GET: /DenunciasVulnerable/Details/5
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

            // ❌ PROBLEMA: No verifica que el usuario actual sea el dueño de la denuncia
            // Cualquiera puede ver detalles de cualquier denuncia adivinando el ID
            
            return View(denuncia);
        }

        // ====================================================================
        // VULNERABILIDAD #3: CREACIÓN SIN PROTECCIONES
        // ====================================================================
        
        // GET: /DenunciasVulnerable/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /DenunciasVulnerable/Create
        [HttpPost]
        // ❌ VULNERABILIDAD: Falta [ValidateAntiForgeryToken]
        // Permite ataques CSRF - Un sitio malicioso puede crear denuncias falsas
        public async Task<IActionResult> Create(CrearDenunciaViewModel model)
        {
            // ❌ VULNERABILIDAD: NO verifica ModelState.IsValid
            // Acepta datos inválidos sin validación del lado servidor
            
            try
            {
                var denuncia = new Denuncia
                {
                    // ❌ VULNERABILIDAD CRÍTICA: Datos sensibles SIN CIFRAR
                    // Se guardan en texto plano en la base de datos
                    Cedula = model.Cedula,              // Texto plano en BD
                    Telefono = model.Telefono,          // Texto plano en BD
                    Email = model.Email,                // Texto plano en BD
                    
                    NombreCompleto = model.NombreCompleto,
                    Categoria = model.Categoria,
                    Ubicacion = model.Ubicacion,
                    Descripcion = model.Descripcion,
                    Estado = "Recibida",
                    FechaCreacion = DateTime.UtcNow
                };

                // ❌ VULNERABILIDAD: Manejo inseguro de archivos
                if (model.ArchivoPdf != null)
                {
                    // PROBLEMA 1: Sin validación de tipo de archivo
                    // Un atacante podría subir un ejecutable renombrado como .pdf
                    
                    // PROBLEMA 2: Sin validación de tamaño
                    // Podrían subir archivos enormes y llenar el disco
                    
                    // PROBLEMA 3: Usa el nombre original del archivo
                    // Vulnerable a directory traversal: ../../etc/passwd.pdf
                    string fileName = model.ArchivoPdf.FileName;
                    
                    // PROBLEMA 4: Guarda en ubicación predecible
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
                    
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    
                    // PROBLEMA 5: Path.Combine no previene directory traversal
                    string filePath = Path.Combine(uploadsFolder, fileName);
                    
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ArchivoPdf.CopyToAsync(stream);
                    }
                    
                    denuncia.ArchivoNombreOriginal = fileName;
                    denuncia.ArchivoNombreServidor = fileName; // ❌ Mismo nombre = predecible
                    denuncia.ArchivoRuta = filePath;
                    denuncia.ArchivoTamanoBytes = model.ArchivoPdf.Length;
                    denuncia.ArchivoTipoMime = model.ArchivoPdf.ContentType; // ❌ Confía en el cliente
                    denuncia.ArchivoFechaSubida = DateTime.UtcNow;
                }

                _context.Denuncias.Add(denuncia);
                await _context.SaveChangesAsync();

                TempData["Mensaje"] = "Denuncia registrada exitosamente";
                return RedirectToAction(nameof(Details), new { id = denuncia.Id });
            }
            catch (Exception ex)
            {
                // ❌ MALA PRÁCTICA: Expone detalles técnicos al usuario
                TempData["Error"] = $"Error al guardar: {ex.Message}";
                return View(model);
            }
        }

        // ====================================================================
        // VULNERABILIDAD #4: DESCARGA DE ARCHIVOS SIN AUTORIZACIÓN
        // ====================================================================
        
        // GET: /DenunciasVulnerable/DescargarArchivo/5
        public async Task<IActionResult> DescargarArchivo(int id)
        {
            var denuncia = await _context.Denuncias.FindAsync(id);
            
            if (denuncia == null || string.IsNullOrEmpty(denuncia.ArchivoRuta))
            {
                return NotFound();
            }

            // ❌ PROBLEMA: No verifica autorización para descargar
            // Cualquiera puede descargar el archivo de cualquier denuncia
            
            if (!System.IO.File.Exists(denuncia.ArchivoRuta))
            {
                return NotFound();
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(denuncia.ArchivoRuta, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            return File(memory, denuncia.ArchivoTipoMime, denuncia.ArchivoNombreOriginal);
        }

        // ====================================================================
        // VULNERABILIDAD #5: CAMBIO DE ESTADO SIN AUTORIZACIÓN
        // ====================================================================
        
        // GET: /DenunciasVulnerable/CambiarEstado/5
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

            // ❌ PROBLEMA: Cualquier usuario puede cambiar el estado
            // No hay verificación de rol de Funcionario

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

        // POST: /DenunciasVulnerable/CambiarEstado
        [HttpPost]
        // ❌ VULNERABILIDAD: Falta [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(CambiarEstadoDenunciaViewModel model)
        {
            // ❌ VULNERABILIDAD: No verifica ModelState
            // ❌ VULNERABILIDAD: No verifica rol de usuario
            
            var denuncia = await _context.Denuncias.FindAsync(model.DenunciaId);
            
            if (denuncia == null)
            {
                return NotFound();
            }

            denuncia.Estado = model.NuevoEstado;
            denuncia.Observaciones = model.Observaciones;
            denuncia.FechaActualizacion = DateTime.UtcNow;

            if (model.NuevoEstado == "Resuelta")
            {
                denuncia.FechaResolucion = DateTime.UtcNow;
            }

            _context.Update(denuncia);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Estado actualizado exitosamente";
            return RedirectToAction(nameof(Details), new { id = denuncia.Id });
        }

        // ====================================================================
        // VULNERABILIDAD #6: ELIMINACIÓN SIN AUTORIZACIÓN
        // ====================================================================
        
        // GET: /DenunciasVulnerable/Delete/5
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

            // ❌ PROBLEMA: Cualquiera puede eliminar cualquier denuncia
            // No hay verificación de rol de Administrador

            return View(denuncia);
        }

        // POST: /DenunciasVulnerable/Delete/5
        [HttpPost, ActionName("Delete")]
        // ❌ VULNERABILIDAD: Falta [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var denuncia = await _context.Denuncias.FindAsync(id);
            
            if (denuncia != null)
            {
                // Eliminar archivo si existe
                if (!string.IsNullOrEmpty(denuncia.ArchivoRuta) && 
                    System.IO.File.Exists(denuncia.ArchivoRuta))
                {
                    System.IO.File.Delete(denuncia.ArchivoRuta);
                }

                _context.Denuncias.Remove(denuncia);
                await _context.SaveChangesAsync();
            }

            TempData["Mensaje"] = "Denuncia eliminada exitosamente";
            return RedirectToAction(nameof(Index));
        }

        // ====================================================================
        // BÚSQUEDA CON SQL INJECTION VULNERABLE
        // ====================================================================
        
        // GET: /DenunciasVulnerable/Buscar
        public IActionResult Buscar()
        {
            return View();
        }

        // POST: /DenunciasVulnerable/BuscarPorCedula
        [HttpPost]
        public async Task<IActionResult> BuscarPorCedula(string cedula)
        {
            if (string.IsNullOrEmpty(cedula))
            {
                return View("Buscar");
            }

            // ❌ VULNERABILIDAD CRÍTICA: SQL Injection
            // Concatenación directa permite ataques SQL
            var query = $"SELECT * FROM Denuncias WHERE Cedula = '{cedula}'";
            
            // NOTA PEDAGÓGICA: Este código no compilará porque FromSqlRaw 
            // requiere el uso de parámetros. Se deja como ejemplo de lo que 
            // NO se debe hacer. En la versión segura usaremos LINQ.
            
            // var denuncias = await _context.Denuncias
            //     .FromSqlRaw(query)
            //     .ToListAsync();

            // Versión alternativa vulnerable pero funcional:
            var denuncias = await _context.Denuncias
                .Where(d => d.Cedula == cedula) // Esto es seguro con LINQ
                .ToListAsync();

            return View("Index", denuncias);
        }
    }
}

// ============================================================================
// RESUMEN DE VULNERABILIDADES DEMOSTRADAS
// ============================================================================
//
// 1. SIN AUTORIZACIÓN BASADA EN ROLES
//    - Cualquier usuario puede acceder a todas las funciones
//    - No hay distinción entre Ciudadano, Funcionario y Administrador
//
// 2. DATOS SENSIBLES SIN CIFRAR
//    - Cédula, teléfono y email en texto plano en la base de datos
//    - Violación de privacidad y posible incumplimiento de Ley 8968
//
// 3. SIN PROTECCIÓN CSRF
//    - Formularios sin tokens anti-falsificación
//    - Vulnerable a ataques de sitios cruzados
//
// 4. VALIDACIÓN DE ARCHIVOS INSEGURA
//    - Acepta cualquier tipo de archivo
//    - Sin validación de tamaño
//    - Sin validación de firma de archivo (magic numbers)
//    - Vulnerable a upload de ejecutables maliciosos
//
// 5. NOMBRES DE ARCHIVO PREDECIBLES
//    - Usa nombres originales del usuario
//    - Vulnerable a directory traversal
//    - Vulnerable a colisiones de nombres
//
// 6. SIN VALIDACIÓN DEL LADO SERVIDOR
//    - Confía en validación del cliente (fácil de bypassear)
//    - No verifica ModelState.IsValid
//
// 7. MENSAJES DE ERROR DETALLADOS
//    - Expone información técnica en mensajes de error
//    - Ayuda a atacantes a entender la estructura del sistema
//
// ============================================================================
