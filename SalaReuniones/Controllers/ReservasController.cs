using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using SalaReuniones.Data;
using SalaReuniones.Models;

namespace SalaReuniones.Controllers
{
    /// <summary>
    /// Controlador encargado de gestionar todas las operaciones
    /// relacionadas con las reservas de salas.
    /// 
    /// SOLO usuarios autenticados pueden acceder.
    /// </summary>
    [Authorize]
    public class ReservasController : Controller
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Inyección del contexto de base de datos (Entity Framework)
        /// </summary>
        public ReservasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // LISTAR RESERVAS
        // ============================================================
        /// <summary>
        /// Muestra todas las reservas ordenadas por fecha y hora.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var reservas = await _context.Reservas
                .Include(r => r.Sala)
                .Include(r => r.Usuario)
                .OrderByDescending(r => r.Fecha)
                .ThenBy(r => r.HoraInicio)
                .ToListAsync();

            return View(reservas);
        }

        // ============================================================
        // CREAR RESERVA (GET)
        // ============================================================
        /// <summary>
        /// Muestra el formulario para crear una reserva.
        /// </summary>
        [Authorize(Roles = "Administrador,Usuario")]
        public IActionResult Create()
        {
            ViewData["SalaId"] =
                new SelectList(_context.Salas, "Id", "Nombre");

            return View();
        }

        // ============================================================
        // CREAR RESERVA (POST)
        // ============================================================
        /// <summary>
        /// Guarda una nueva reserva validando:
        /// - Usuario autenticado
        /// - Horario válido
        /// - Cruce de reservas
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Usuario")]
        public async Task<IActionResult> Create(Reserva reserva)
        {
            // Obtener usuario logueado
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // Validación horario
            if (reserva.HoraFin <= reserva.HoraInicio)
                ModelState.AddModelError("",
                    "La hora de fin debe ser mayor que la hora de inicio.");

            // Validar cruces de horario
            bool existeCruce = await _context.Reservas.AnyAsync(r =>
                r.SalaId == reserva.SalaId &&
                r.Fecha.Date == reserva.Fecha.Date &&
                r.Estado == EstadoReserva.Activa &&
                r.HoraInicio < reserva.HoraFin &&
                r.HoraFin > reserva.HoraInicio
            );

            if (existeCruce)
                ModelState.AddModelError("",
                    "Ya existe una reserva en ese horario para esta sala.");

            if (!ModelState.IsValid)
            {
                ViewData["SalaId"] =
                    new SelectList(_context.Salas, "Id", "Nombre", reserva.SalaId);

                return View(reserva);
            }

            // Asignaciones automáticas
            reserva.UsuarioId = userId;
            reserva.Estado = EstadoReserva.Activa;

            _context.Add(reserva);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // EDITAR RESERVA (GET)
        // ============================================================
        [Authorize(Roles = "Administrador,Usuario")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var reserva = await _context.Reservas.FindAsync(id);
            if (reserva == null) return NotFound();

            // Solo dueño o admin puede editar
            if (!User.IsInRole("Administrador") &&
                reserva.UsuarioId != User.FindFirstValue(ClaimTypes.NameIdentifier))
                return Forbid();

            ViewData["SalaId"] =
                new SelectList(_context.Salas, "Id", "Nombre", reserva.SalaId);

            return View(reserva);
        }

        // ============================================================
        // EDITAR RESERVA (POST)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Usuario")]
        public async Task<IActionResult> Edit(int id, Reserva reserva)
        {
            if (id != reserva.Id)
                return NotFound();

            var reservaOriginal = await _context.Reservas
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservaOriginal == null)
                return NotFound();

            // Seguridad: solo dueño o admin
            if (!User.IsInRole("Administrador") &&
                reservaOriginal.UsuarioId != User.FindFirstValue(ClaimTypes.NameIdentifier))
                return Forbid();

            // Mantener datos protegidos
            reserva.UsuarioId = reservaOriginal.UsuarioId;
            reserva.Estado = reservaOriginal.Estado;

            // Validación horario
            if (reserva.HoraFin <= reserva.HoraInicio)
                ModelState.AddModelError("",
                    "La hora de fin debe ser mayor que la hora de inicio.");

            // Validar cruces
            bool existeCruce = await _context.Reservas.AnyAsync(r =>
                r.Id != reserva.Id &&
                r.SalaId == reserva.SalaId &&
                r.Fecha.Date == reserva.Fecha.Date &&
                r.Estado == EstadoReserva.Activa &&
                r.HoraInicio < reserva.HoraFin &&
                r.HoraFin > reserva.HoraInicio
            );

            if (existeCruce)
                ModelState.AddModelError("",
                    "Ya existe una reserva en ese horario.");

            if (!ModelState.IsValid)
            {
                ViewData["SalaId"] =
                    new SelectList(_context.Salas, "Id", "Nombre", reserva.SalaId);

                return View(reserva);
            }

            _context.Update(reserva);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // ELIMINAR RESERVA
        // ============================================================
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var reserva = await _context.Reservas
                .Include(r => r.Sala)
                .Include(r => r.Usuario)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (reserva == null) return NotFound();

            return View(reserva);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var reserva = await _context.Reservas.FindAsync(id);

            if (reserva != null)
            {
                _context.Reservas.Remove(reserva);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // DETALLES
        // ============================================================
        [Authorize(Roles = "Administrador,Usuario,Visualizador")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var reserva = await _context.Reservas
                .Include(r => r.Sala)
                .Include(r => r.Usuario)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (reserva == null) return NotFound();

            return View(reserva);
        }

        // ============================================================
        // VISTA CALENDARIO
        // ============================================================
        /// <summary>
        /// Carga la vista del calendario y lista de salas.
        /// </summary>
        public IActionResult Calendar()
        {
            ViewData["Salas"] = new SelectList(
                _context.Salas.OrderBy(s => s.Nombre),
                "Id",
                "Nombre"
            );

            return View();
        }

        // ============================================================
        // API PARA FULLCALENDAR
        // ============================================================
        /// <summary>
        /// Endpoint JSON consumido por FullCalendar.
        /// 
        /// IMPORTANTE:
        /// Aquí se envía el color de la sala para pintar eventos.
        /// </summary>
        [Authorize]
        public async Task<IActionResult> GetReservas(DateTime start, DateTime end, int? salaId)
        {
            var query = _context.Reservas
                .Include(r => r.Sala)
                .Include(r => r.Usuario)
                .Where(r =>
                    r.Fecha >= start.Date &&
                    r.Fecha <= end.Date &&
                    r.Estado == EstadoReserva.Activa
                );

            // Filtro opcional por sala
            if (salaId.HasValue)
                query = query.Where(r => r.SalaId == salaId.Value);

            var reservas = await query.ToListAsync();

            // Conversión al formato que FullCalendar entiende
            var eventos = reservas.Select(r => new
            {
                id = r.Id,

                // Texto mostrado en calendario
                title = string.IsNullOrEmpty(r.Motivo)
                    ? $"Reserva - {r.Sala!.Nombre}"
                    : r.Motivo,

                // Fechas completas
                start = r.Fecha.Date.Add(r.HoraInicio),
                end = r.Fecha.Date.Add(r.HoraFin),

                // ⭐ COLOR DINÁMICO DESDE BD (SOLUCIÓN)
                backgroundColor = r.Sala!.ColorHex ?? "#3788d8",
                borderColor = r.Sala!.ColorHex ?? "#3788d8",

                // Datos extra (tooltip / modal)
                usuario = r.Usuario?.Email ?? "Sin usuario",
                sala = r.Sala!.Nombre
            });

            return Json(eventos);
        }
    }
}