using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MUNIDENUNCIA.Data;
using MUNIDENUNCIA.Models;
using MUNIDENUNCIA.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MUNIDENUNCIA.Controllers
{
    /// <summary>
    /// CONTROLADOR VULNERABLE - SOLO PARA DEMOSTRACIÓN DE RIESGOS
    /// Este controlador contiene múltiples vulnerabilidades intencionales:
    /// 1. Sin validación del lado servidor (no verifica ModelState)
    /// 2. SQL Injection mediante concatenación directa de strings
    /// 3. XSS almacenado al no sanitizar comentarios
    /// 4. Sin protección CSRF (falta ValidateAntiForgeryToken)
    /// 
    /// ADVERTENCIA: NO USAR ESTE CÓDIGO EN PRODUCCIÓN
    /// </summary>
    public class PermisosVulnerableController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PermisosVulnerableController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /PermisosVulnerable
        /// <summary>
        /// Lista todas las solicitudes de permisos
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var solicitudes = await _context.SolicitudesPermisos
                .OrderByDescending(s => s.FechaSolicitud)
                .ToListAsync();

            return View(solicitudes);
        }

        // GET: /PermisosVulnerable/Create
        /// <summary>
        /// Muestra formulario de creación de solicitud
        /// </summary>
        public IActionResult Create()
        {
            return View(new SolicitudPermisoViewModel());
        }

        // POST: /PermisosVulnerable/Create
        /// <summary>
        /// VULNERABILIDAD #1: SIN VALIDACIÓN DEL LADO SERVIDOR
        /// Este método NO verifica ModelState.IsValid, permitiendo el envío
        /// de datos inválidos, incompletos o maliciosos
        /// </summary>
        [HttpPost]
        // VULNERABILIDAD #4: Falta [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SolicitudPermisoViewModel model)
        {
            // ❌ PELIGRO: No se verifica ModelState.IsValid
            // Cualquier dato puede ser enviado y será procesado

            try
            {
                // Mapeo directo del ViewModel a la entidad sin validación
                var solicitud = new SolicitudPermiso
                {
                    CedulaPropietario = model.CedulaPropietario,
                    NombreCompletoPropietario = model.NombreCompletoPropietario,
                    EmailPropietario = model.EmailPropietario,
                    TelefonoPropietario = model.TelefonoPropietario,
                    Distrito = model.Distrito,
                    DireccionCompleta = model.DireccionCompleta,
                    PlanoCatastrado = model.PlanoCatastrado,
                    TipoConstruccion = model.TipoConstruccion,
                    AreaConstruccionM2 = model.AreaConstruccionM2,
                    NumeroPlantas = model.NumeroPlantas,
                    DescripcionProyecto = model.DescripcionProyecto,
                    PresupuestoEstimado = model.PresupuestoEstimado,
                    Estado = EstadoSolicitud.Pendiente,
                    FechaSolicitud = DateTime.UtcNow
                };

                _context.SolicitudesPermisos.Add(solicitud);
                await _context.SaveChangesAsync();

                TempData["Mensaje"] = "Solicitud creada exitosamente";
                return RedirectToAction(nameof(Details), new { id = solicitud.Id });
            }
            catch (Exception ex)
            {
                // ❌ PELIGRO: Exposición de información sensible en mensajes de error
                TempData["Error"] = $"Error al crear solicitud: {ex.Message}";
                return View(model);
            }
        }

        // GET: /PermisosVulnerable/Details/5
        /// <summary>
        /// Muestra detalles de una solicitud específica
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            var solicitud = await _context.SolicitudesPermisos
                .Include(s => s.Comentarios)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (solicitud == null)
            {
                return NotFound();
            }

            return View(solicitud);
        }

        // GET: /PermisosVulnerable/Buscar
        /// <summary>
        /// Muestra formulario de búsqueda
        /// </summary>
        public IActionResult Buscar()
        {
            return View(new BusquedaSolicitudViewModel());
        }

        // POST: /PermisosVulnerable/Buscar
        /// <summary>
        /// VULNERABILIDAD #2: SQL INJECTION
        /// Este método construye consultas SQL usando concatenación directa,
        /// permitiendo a atacantes inyectar código SQL malicioso
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Buscar(BusquedaSolicitudViewModel model)
        {
            try
            {
                // ❌ PELIGRO CRÍTICO: Borrado de tablas
                // Si model.PlanoCatastrado contiene: ; DROP TABLE dbo.SolicitudPruebas; --
                // Se borrar la tabla ; DROP TABLE dbo.SolicitudPruebas;
                if (!string.IsNullOrEmpty(model.PlanoCatastrado))
                {
                    // ❌ Otra consulta vulnerable con concatenación
                    var sql = $"SELECT * FROM SolicitudesPermisos WHERE PlanoCatastrado = '{model.PlanoCatastrado}'";

                    var resultados = await _context.SolicitudesPermisos
                        .FromSqlRaw(sql)
                        .ToListAsync();

                    ViewBag.CriterioBusqueda = $"Plano: {model.PlanoCatastrado}";
                    return View("ResultadosBusqueda", resultados);
                }

                // ❌ PELIGRO CRÍTICO: Concatenación directa de SQL
                // Si model.CedulaBuscada contiene: ' OR '1'='1' --
                // La consulta retornará TODAS las solicitudes

                if (!string.IsNullOrEmpty(model.CedulaBuscada))
                {
                    // Construcción vulnerable de SQL mediante interpolación
                    var sql = $"SELECT * FROM SolicitudesPermisos WHERE CedulaPropietario = '{model.CedulaBuscada}'";

                    var resultados = await _context.SolicitudesPermisos
                        .FromSqlRaw(sql)
                        .ToListAsync();

                    ViewBag.CriterioBusqueda = $"Cédula: {model.CedulaBuscada}";
                    return View("ResultadosBusqueda", resultados);
                }

                // Búsqueda por tipo de construcción (esta es segura porque usa LINQ)
                var query = _context.SolicitudesPermisos.AsQueryable();

                if (model.TipoConstruccion.HasValue)
                {
                    query = query.Where(s => s.TipoConstruccion == model.TipoConstruccion.Value);
                }

                if (model.Estado.HasValue)
                {
                    query = query.Where(s => s.Estado == model.Estado.Value);
                }

                var solicitudes = await query
                    .OrderByDescending(s => s.FechaSolicitud)
                    .ToListAsync();

                ViewBag.CriterioBusqueda = "Criterios múltiples";
                return View("ResultadosBusqueda", solicitudes);
            }
            catch (Exception ex)
            {
                // ❌ PELIGRO: Exposición de stack trace completo
                ViewBag.Error = $"Error en búsqueda: {ex.Message}\n{ex.StackTrace}";
                return View(model);
            }
        }

        // POST: /PermisosVulnerable/AgregarComentario
        /// <summary>
        /// VULNERABILIDAD #3: XSS ALMACENADO
        /// Este método NO sanitiza el comentario antes de guardarlo,
        /// permitiendo almacenar código JavaScript malicioso
        /// </summary>
        [HttpPost]
        // VULNERABILIDAD #4: Falta [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarComentario(ComentarioViewModel model)
        {
            // ❌ PELIGRO: No se verifica ModelState
            // ❌ PELIGRO: No se sanitiza el TextoComentario

            try
            {
                var comentario = new Comentario
                {
                    SolicitudPermisoId = model.SolicitudPermisoId,
                    NombreFuncionario = model.NombreFuncionario,
                    CargoFuncionario = model.CargoFuncionario,
                    TextoComentario = model.TextoComentario, // ❌ Sin sanitización
                    EsAprobacion = model.EsAprobacion,
                    EsRechazo = model.EsRechazo,
                    FechaComentario = DateTime.UtcNow
                };

                _context.Comentarios.Add(comentario);

                // Actualizar estado de la solicitud si es aprobación o rechazo
                if (model.EsAprobacion || model.EsRechazo)
                {
                    var solicitud = await _context.SolicitudesPermisos
                        .FindAsync(model.SolicitudPermisoId);

                    if (solicitud != null)
                    {
                        solicitud.Estado = model.EsAprobacion
                            ? EstadoSolicitud.Aprobada
                            : EstadoSolicitud.Denegada;
                        solicitud.FechaRevision = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();

                TempData["Mensaje"] = "Comentario agregado exitosamente";
                return RedirectToAction(nameof(Details), new { id = model.SolicitudPermisoId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al agregar comentario: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id = model.SolicitudPermisoId });
            }
        }

        // GET: /PermisosVulnerable/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var solicitud = await _context.SolicitudesPermisos.FindAsync(id);

            if (solicitud == null)
            {
                return NotFound();
            }

            // Mapear a ViewModel sin validación
            var model = new SolicitudPermisoViewModel
            {
                CedulaPropietario = solicitud.CedulaPropietario,
                NombreCompletoPropietario = solicitud.NombreCompletoPropietario,
                EmailPropietario = solicitud.EmailPropietario,
                TelefonoPropietario = solicitud.TelefonoPropietario,
                Distrito = solicitud.Distrito,
                DireccionCompleta = solicitud.DireccionCompleta,
                PlanoCatastrado = solicitud.PlanoCatastrado,
                TipoConstruccion = solicitud.TipoConstruccion,
                AreaConstruccionM2 = solicitud.AreaConstruccionM2,
                NumeroPlantas = solicitud.NumeroPlantas,
                DescripcionProyecto = solicitud.DescripcionProyecto,
                PresupuestoEstimado = solicitud.PresupuestoEstimado
            };

            ViewBag.SolicitudId = id;
            return View(model);
        }

        // POST: /PermisosVulnerable/Edit/5
        /// <summary>
        /// VULNERABILIDAD: Sin validación en actualización
        /// </summary>
        [HttpPost]
        // VULNERABILIDAD: Falta [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SolicitudPermisoViewModel model)
        {
            // ❌ PELIGRO: No se verifica ModelState

            try
            {
                var solicitud = await _context.SolicitudesPermisos.FindAsync(id);

                if (solicitud == null)
                {
                    return NotFound();
                }

                // Actualizar sin validación
                solicitud.NombreCompletoPropietario = model.NombreCompletoPropietario;
                solicitud.EmailPropietario = model.EmailPropietario;
                solicitud.TelefonoPropietario = model.TelefonoPropietario;
                solicitud.DireccionCompleta = model.DireccionCompleta;
                solicitud.DescripcionProyecto = model.DescripcionProyecto;
                solicitud.PresupuestoEstimado = model.PresupuestoEstimado;

                await _context.SaveChangesAsync();

                TempData["Mensaje"] = "Solicitud actualizada exitosamente";
                return RedirectToAction(nameof(Details), new { id = id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al actualizar: {ex.Message}";
                ViewBag.SolicitudId = id;
                return View(model);
            }
        }
    }
}
