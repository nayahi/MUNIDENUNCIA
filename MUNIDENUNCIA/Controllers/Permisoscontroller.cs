using System;
using System.Linq;
using System.Reflection.Metadata;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MUNIDENUNCIA.Data;
using MUNIDENUNCIA.Models;
using MUNIDENUNCIA.ViewModels;

namespace MUNIDENUNCIA.Controllers
{
    /// <summary>
    /// CONTROLADOR SEGURO - Implementa todas las mejores prácticas de seguridad
    /// Este controlador corrige todas las vulnerabilidades del controlador anterior:
    /// 1. Validación obligatoria del lado servidor con ModelState
    /// 2. Prevención de SQL Injection usando LINQ to Entities
    /// 3. Prevención de XSS mediante sanitización con HtmlEncoder
    /// 4. Protección CSRF con ValidateAntiForgeryToken
    /// 
    /// Este es el código que debe usarse en producción
    /// </summary>
    public class PermisosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly HtmlEncoder _htmlEncoder;

        /// <summary>
        /// Constructor con inyección de dependencias para HtmlEncoder
        /// </summary>
        public PermisosController(
            ApplicationDbContext context,
            HtmlEncoder htmlEncoder)
        {
            _context = context;
            _htmlEncoder = htmlEncoder;
        }

        // GET: /Permisos
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

        // GET: /Permisos/Create
        /// <summary>
        /// Muestra formulario de creación de solicitud
        /// </summary>
        public IActionResult Create()
        {
            return View(new SolicitudPermisoValidadoViewModel());
        }

        // POST: /Permisos/Create
        /// <summary>
        /// CORRECCIÓN #1: Validación obligatoria del lado servidor
        /// Este método verifica ModelState.IsValid antes de procesar datos
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken] // CORRECCIÓN #4: Protección CSRF
        public async Task<IActionResult> Create(SolicitudPermisoValidadoViewModel model)
        {
            // ✅ SEGURO: Verificación obligatoria de ModelState
            if (!ModelState.IsValid)
            {
                // Retornar vista con mensajes de error de validación
                return View(model);
            }

            // Validaciones adicionales de negocio
            var existeSolicitudPendiente = await _context.SolicitudesPermisos
                .AnyAsync(s => s.PlanoCatastrado == model.PlanoCatastrado
                            && s.Estado == EstadoSolicitud.Pendiente);

            if (existeSolicitudPendiente)
            {
                ModelState.AddModelError("PlanoCatastrado",
                    "Ya existe una solicitud pendiente para este plano catastrado");
                return View(model);
            }

            try
            {
                // ✅ SEGURO: Mapeo de datos ya validados
                var solicitud = new SolicitudPermiso
                {
                    CedulaPropietario = model.CedulaPropietario.Trim(),
                    NombreCompletoPropietario = model.NombreCompletoPropietario.Trim(),
                    EmailPropietario = model.EmailPropietario.Trim().ToLower(),
                    TelefonoPropietario = model.TelefonoPropietario.Trim(),
                    Distrito = model.Distrito.Trim(),
                    DireccionCompleta = model.DireccionCompleta.Trim(),
                    PlanoCatastrado = model.PlanoCatastrado.Trim().ToUpper(),
                    TipoConstruccion = model.TipoConstruccion,
                    AreaConstruccionM2 = model.AreaConstruccionM2,
                    NumeroPlantas = model.NumeroPlantas,
                    // ✅ CORRECCIÓN #3: Sanitización de la descripción
                    DescripcionProyecto = _htmlEncoder.Encode(model.DescripcionProyecto),
                    PresupuestoEstimado = model.PresupuestoEstimado,
                    Estado = EstadoSolicitud.Pendiente,
                    FechaSolicitud = DateTime.UtcNow
                };

                _context.SolicitudesPermisos.Add(solicitud);
                await _context.SaveChangesAsync();

                TempData["Mensaje"] = "Solicitud creada exitosamente. Número de referencia: " + solicitud.Id;
                return RedirectToAction(nameof(Details), new { id = solicitud.Id });
            }
            catch (DbUpdateException)
            {
                // ✅ SEGURO: Mensaje de error genérico sin detalles técnicos
                ModelState.AddModelError("",
                    "Error al guardar la solicitud. Por favor verifique los datos e intente nuevamente.");
                return View(model);
            }
        }

        // GET: /Permisos/Details/5
        /// <summary>
        /// Muestra detalles de una solicitud específica
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            // ✅ SEGURO: Consulta parametrizada automáticamente con LINQ
            var solicitud = await _context.SolicitudesPermisos
                .Include(s => s.Comentarios)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (solicitud == null)
            {
                return NotFound();
            }

            return View(solicitud);
        }

        // GET: /Permisos/Buscar
        /// <summary>
        /// Muestra formulario de búsqueda
        /// </summary>
        public IActionResult Buscar()
        {
            return View(new BusquedaSolicitudValidadaViewModel());
        }

        // POST: /Permisos/Buscar
        /// <summary>
        /// CORRECCIÓN #2: Prevención de SQL Injection con LINQ to Entities
        /// Este método usa exclusivamente LINQ que genera SQL parametrizado automáticamente
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Buscar(BusquedaSolicitudValidadaViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Validar rango de fechas si aplica
            if (!model.ValidarRangoFechas())
            {
                ModelState.AddModelError("",
                    "La fecha final debe ser mayor o igual a la fecha inicial");
                return View(model);
            }

            try
            {
                // ✅ SEGURO: Construcción de consulta con LINQ to Entities
                // Entity Framework Core genera SQL parametrizado automáticamente
                var query = _context.SolicitudesPermisos.AsQueryable();

                // Búsqueda por cédula
                if (!string.IsNullOrWhiteSpace(model.CedulaBuscada))
                {
                    // ✅ SEGURO: LINQ parametriza automáticamente
                    query = query.Where(s => s.CedulaPropietario == model.CedulaBuscada.Trim());
                }

                // Búsqueda por plano catastrado
                if (!string.IsNullOrWhiteSpace(model.PlanoCatastrado))
                {
                    // ✅ SEGURO: LINQ parametriza automáticamente
                    var planoUpper = model.PlanoCatastrado.Trim().ToUpper();
                    query = query.Where(s => s.PlanoCatastrado == planoUpper);
                }

                // Filtro por tipo de construcción
                if (model.TipoConstruccion.HasValue)
                {
                    query = query.Where(s => s.TipoConstruccion == model.TipoConstruccion.Value);
                }

                // Filtro por estado
                if (model.Estado.HasValue)
                {
                    query = query.Where(s => s.Estado == model.Estado.Value);
                }

                // Filtro por rango de fechas
                if (model.FechaDesde.HasValue)
                {
                    query = query.Where(s => s.FechaSolicitud >= model.FechaDesde.Value);
                }

                if (model.FechaHasta.HasValue)
                {
                    var fechaHastaFinal = model.FechaHasta.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(s => s.FechaSolicitud <= fechaHastaFinal);
                }

                var resultados = await query
                    .OrderByDescending(s => s.FechaSolicitud)
                    .ToListAsync();

                ViewBag.CantidadResultados = resultados.Count;
                ViewBag.ModeloBusqueda = model;

                return View("ResultadosBusqueda", resultados);
            }
            catch (Exception)
            {
                // ✅ SEGURO: Mensaje genérico sin detalles técnicos
                ModelState.AddModelError("",
                    "Error al realizar la búsqueda. Por favor intente nuevamente.");
                return View(model);
            }
        }

        // Método alternativo usando FromSqlInterpolated para demostración
        /// <summary>
        /// Búsqueda segura usando SQL directo con FromSqlInterpolated
        /// Solo usar cuando LINQ no pueda expresar la consulta necesaria
        /// </summary>
        public async Task<IActionResult> BuscarConSQLParametrizado(string cedula)
        {
            if (string.IsNullOrWhiteSpace(cedula))
            {
                return BadRequest("Cédula es requerida");
            }

            // ✅ SEGURO: FromSqlInterpolated parametriza automáticamente
            // El $ antes de la cadena activa interpolación segura
            var resultados = await _context.SolicitudesPermisos
                .FromSqlInterpolated($@"
                    SELECT * FROM SolicitudesPermisos 
                    WHERE CedulaPropietario = {cedula}
                    ORDER BY FechaSolicitud DESC")
                .ToListAsync();

            return View("ResultadosBusqueda", resultados);
        }

        // POST: /Permisos/AgregarComentario
        /// <summary>
        /// CORRECCIÓN #3: Sanitización de entrada con HtmlEncoder
        /// Este método sanitiza el comentario antes de guardarlo
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken] // CORRECCIÓN #4: Protección CSRF
        public async Task<IActionResult> AgregarComentario(ComentarioValidadoViewModel model)
        {
            // ✅ SEGURO: Verificación obligatoria de validación
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Por favor complete todos los campos correctamente";
                return RedirectToAction(nameof(Details), new { id = model.SolicitudPermisoId });
            }

            // Validar que la solicitud existe
            var solicitudExiste = await _context.SolicitudesPermisos
                .AnyAsync(s => s.Id == model.SolicitudPermisoId);

            if (!solicitudExiste)
            {
                return NotFound();
            }

            try
            {
                var comentario = new Comentario
                {
                    SolicitudPermisoId = model.SolicitudPermisoId,
                    NombreFuncionario = model.NombreFuncionario.Trim(),
                    CargoFuncionario = model.CargoFuncionario.Trim(),
                    // ✅ CORRECCIÓN #3: Sanitización con HtmlEncoder
                    TextoComentario = _htmlEncoder.Encode(model.TextoComentario),
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
                        solicitud.RevisadoPor = model.NombreFuncionario;

                        if (model.EsAprobacion)
                        {
                            solicitud.FechaAprobacion = DateTime.UtcNow;
                        }
                    }
                }

                await _context.SaveChangesAsync();

                TempData["Mensaje"] = "Comentario agregado exitosamente";
                return RedirectToAction(nameof(Details), new { id = model.SolicitudPermisoId });
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Error al guardar el comentario. Por favor intente nuevamente.";
                return RedirectToAction(nameof(Details), new { id = model.SolicitudPermisoId });
            }
        }

        // GET: /Permisos/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var solicitud = await _context.SolicitudesPermisos.FindAsync(id);

            if (solicitud == null)
            {
                return NotFound();
            }

            // No permitir editar solicitudes aprobadas o denegadas
            if (solicitud.Estado == EstadoSolicitud.Aprobada ||
                solicitud.Estado == EstadoSolicitud.Denegada)
            {
                TempData["Error"] = "No se pueden editar solicitudes aprobadas o denegadas";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            // Mapear a ViewModel validado
            var model = new SolicitudPermisoValidadoViewModel
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

        // POST: /Permisos/Edit/5
        /// <summary>
        /// Actualización segura con validación completa
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken] // CORRECCIÓN #4: Protección CSRF
        public async Task<IActionResult> Edit(int id, SolicitudPermisoValidadoViewModel model)
        {
            // ✅ SEGURO: Validación obligatoria
            if (!ModelState.IsValid)
            {
                ViewBag.SolicitudId = id;
                return View(model);
            }

            try
            {
                var solicitud = await _context.SolicitudesPermisos.FindAsync(id);

                if (solicitud == null)
                {
                    return NotFound();
                }

                // Verificar que la solicitud puede ser editada
                if (solicitud.Estado == EstadoSolicitud.Aprobada ||
                    solicitud.Estado == EstadoSolicitud.Denegada)
                {
                    ModelState.AddModelError("",
                        "No se pueden editar solicitudes aprobadas o denegadas");
                    ViewBag.SolicitudId = id;
                    return View(model);
                }

                // ✅ SEGURO: Actualización con datos validados y sanitizados
                solicitud.NombreCompletoPropietario = model.NombreCompletoPropietario.Trim();
                solicitud.EmailPropietario = model.EmailPropietario.Trim().ToLower();
                solicitud.TelefonoPropietario = model.TelefonoPropietario.Trim();
                solicitud.DireccionCompleta = model.DireccionCompleta.Trim();
                solicitud.DescripcionProyecto = _htmlEncoder.Encode(model.DescripcionProyecto);
                solicitud.PresupuestoEstimado = model.PresupuestoEstimado;

                await _context.SaveChangesAsync();

                TempData["Mensaje"] = "Solicitud actualizada exitosamente";
                return RedirectToAction(nameof(Details), new { id = id });
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("",
                    "Error al actualizar la solicitud. Por favor intente nuevamente.");
                ViewBag.SolicitudId = id;
                return View(model);
            }
        }
    }
}
