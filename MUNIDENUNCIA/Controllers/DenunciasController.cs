using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MUNIDENUNCIA.Data;
using MUNIDENUNCIA.Models;

namespace MUNIDENUNCIA.Controllers
{
    /// <summary>
    /// Controlador para gestión de denuncias ciudadanas
    /// ADVERTENCIA: Este controlador contiene VULNERABILIDADES INTENCIONALES
    /// Los estudiantes deben identificarlas y corregirlas como parte de la Tarea 3
    /// </summary>
    public class DenunciasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DenunciasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Denuncias
        public async Task<IActionResult> Index()
        {
            var denuncias = await _context.Denuncias
                .OrderByDescending(d => d.FechaCreacion)
                .ToListAsync();

            return View(denuncias);
        }

        // GET: Denuncias/Details/5
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

            return View(denuncia);
        }

        // GET: Denuncias/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Denuncias/Create
        // VULNERABILIDAD #1: Falta validación del lado servidor
        // TAREA: Agregar verificación de ModelState.IsValid
        [HttpPost]
        // VULNERABILIDAD #2: Falta protección CSRF
        // TAREA: Agregar [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DenunciaCiudadanaModel model)
        {
            // ❌ VULNERABILIDAD: NO se verifica ModelState.IsValid
            // El sistema acepta cualquier dato sin validación del lado servidor
            // TAREA: Agregar verificación aquí

            try
            {
                // Mapeo del ViewModel a la entidad
                var denuncia = new Denuncia
                {
                    Cedula = model.Cedula,
                    NombreCompleto = model.NombreCompleto,
                    Email = model.Email,
                    Telefono = model.Telefono,
                    Categoria = model.Categoria,
                    Ubicacion = model.Ubicacion,
                    // ❌ VULNERABILIDAD #3: NO se sanitiza la descripción
                    // Esto permite ataques XSS almacenados
                    // TAREA: Usar HtmlEncoder.Encode() antes de asignar
                    Descripcion = model.Descripcion,
                    Estado = "Pendiente",
                    FechaCreacion = DateTime.UtcNow
                };

                _context.Denuncias.Add(denuncia);
                await _context.SaveChangesAsync();

                TempData["Mensaje"] = "Denuncia registrada exitosamente";
                return RedirectToAction(nameof(Details), new { id = denuncia.Id });
            }
            catch (Exception ex)
            {
                // ❌ MALA PRÁCTICA: Exponer detalles de error al usuario
                // Esto puede revelar información sensible del sistema
                TempData["Error"] = $"Error al guardar: {ex.Message}";
                return View(model);
            }
        }

        // GET: Denuncias/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var denuncia = await _context.Denuncias
                .FirstOrDefaultAsync(d => d.Id == id);

            if (denuncia == null)
            {
                return NotFound();
            }

            // ⚠️ NOTA PEDAGÓGICA: En una aplicación real, verificaríamos que solo
            // denuncias en estado "Pendiente" puedan editarse

            // Mapear entidad a ViewModel
            var model = new DenunciaCiudadanaModel
            {
                Id = denuncia.Id,
                Cedula = denuncia.Cedula,
                NombreCompleto = denuncia.NombreCompleto,
                Email = denuncia.Email,
                Telefono = denuncia.Telefono,
                Categoria = denuncia.Categoria,
                Ubicacion = denuncia.Ubicacion,
                Descripcion = denuncia.Descripcion
            };

            return View(model);
        }

        // POST: Denuncias/Edit/5
        // VULNERABILIDAD #1: Falta validación del lado servidor
        // VULNERABILIDAD #2: Falta protección CSRF
        // TAREA: Agregar [ValidateAntiForgeryToken] y verificar ModelState.IsValid
        [HttpPost]
        // ❌ FALTA: [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DenunciaCiudadanaModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            // ❌ VULNERABILIDAD: NO se verifica ModelState.IsValid
            // TAREA: Agregar verificación aquí

            try
            {
                var denuncia = await _context.Denuncias.FindAsync(id);

                if (denuncia == null)
                {
                    return NotFound();
                }

                // Actualizar campos
                denuncia.Cedula = model.Cedula;
                denuncia.NombreCompleto = model.NombreCompleto;
                denuncia.Email = model.Email;
                denuncia.Telefono = model.Telefono;
                denuncia.Categoria = model.Categoria;
                denuncia.Ubicacion = model.Ubicacion;

                // ❌ VULNERABILIDAD #3: NO se sanitiza la descripción
                // TAREA: Usar HtmlEncoder.Encode() antes de asignar
                denuncia.Descripcion = model.Descripcion;

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
                else
                {
                    // ❌ MALA PRÁCTICA: Exponer detalles técnicos
                    TempData["Error"] = "Error de concurrencia al actualizar";
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                // ❌ MALA PRÁCTICA: Exponer mensaje de excepción
                TempData["Error"] = $"Error al actualizar: {ex.Message}";
                return View(model);
            }
        }

        // Método auxiliar
        private async Task<bool> DenunciaExists(int id)
        {
            return await _context.Denuncias.AnyAsync(e => e.Id == id);
        }

        // GET: Denuncias/Buscar
        public IActionResult Buscar()
        {
            return View();
        }

        // POST: Denuncias/BuscarPorCedula
        // VULNERABILIDAD #4: SQL Injection mediante concatenación
        // TAREA: Reemplazar con LINQ to Entities
        [HttpPost]
        public async Task<IActionResult> BuscarPorCedula(string cedula)
        {
            if (string.IsNullOrEmpty(cedula))
            {
                ViewBag.Mensaje = "Debe ingresar una cédula para buscar";
                return View("Buscar");
            }

            try
            {
                // ❌ VULNERABILIDAD CRÍTICA: Concatenación directa de SQL
                // Esto permite inyección de código SQL malicioso
                // TAREA: Reemplazar con consulta LINQ
                var sql = $"SELECT * FROM Denuncias WHERE Cedula = '{cedula}'";
                var denuncias = await _context.Denuncias
                    .FromSqlRaw(sql)
                    .ToListAsync();

                ViewBag.CriterioBusqueda = $"Cédula: {cedula}";
                ViewBag.CantidadResultados = denuncias.Count;

                return View("ResultadosBusqueda", denuncias);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error en búsqueda: {ex.Message}";
                return View("Buscar");
            }
        }

        // POST: Denuncias/BuscarPorCategoria
        // VULNERABILIDAD #5: SQL Injection con categoría
        // TAREA: Reemplazar con LINQ to Entities
        [HttpPost]
        public async Task<IActionResult> BuscarPorCategoria(string categoria)
        {
            if (string.IsNullOrEmpty(categoria))
            {
                ViewBag.Mensaje = "Debe seleccionar una categoría";
                return View("Buscar");
            }

            try
            {
                // ❌ VULNERABILIDAD CRÍTICA: Otra concatenación SQL
                // TAREA: Reemplazar con LINQ
                var sql = $"SELECT * FROM Denuncias WHERE Categoria = '{categoria}'";
                var denuncias = await _context.Denuncias
                    .FromSqlRaw(sql)
                    .OrderByDescending(d => d.FechaCreacion)
                    .ToListAsync();

                ViewBag.CriterioBusqueda = $"Categoría: {categoria}";
                ViewBag.CantidadResultados = denuncias.Count;

                return View("ResultadosBusqueda", denuncias);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error en búsqueda: {ex.Message}";
                return View("Buscar");
            }
        }

        // POST: Denuncias/ActualizarEstado
        // VULNERABILIDAD #6: Falta protección CSRF
        // TAREA: Agregar [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> ActualizarEstado(int id, string nuevoEstado, string observaciones)
        {
            // ❌ VULNERABILIDAD: Falta validación de entrada
            // TAREA: Validar que nuevoEstado sea un valor permitido

            var denuncia = await _context.Denuncias.FindAsync(id);
            
            if (denuncia == null)
            {
                return NotFound();
            }

            denuncia.Estado = nuevoEstado;
            
            // ❌ VULNERABILIDAD #7: NO se sanitizan las observaciones
            // TAREA: Usar HtmlEncoder.Encode() antes de asignar
            denuncia.Observaciones = observaciones;

            if (nuevoEstado == "Resuelta")
            {
                denuncia.FechaResolucion = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Estado actualizado correctamente";
            return RedirectToAction(nameof(Details), new { id = id });
        }

        [Authorize]
        [HttpGet]
        public IActionResult VerificarClaims()
        {
            var claims = User.Claims.Select(c => new
            {
                Type = c.Type,
                Value = c.Value
            }).ToList();

            return Json(claims);
        }
    }
}
