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
    /// ============================================================
    /// CONTROLADOR DE RESERVAS
    /// ------------------------------------------------------------
    /// Funcionalidades:
    /// ✔ CRUD de reservas
    /// ✔ Validaciones de negocio
    /// ✔ Seguridad por roles
    /// ✔ Endpoint JSON para FullCalendar
    /// ✔ Bloqueo de reservas pasadas (no editables ni eliminables)
    /// ✔ Estado calculado dinámicamente (activa, finalizada, cancelada)
    ///
    /// ARQUITECTURA:
    /// - Backend → lógica de negocio + estado
    /// - Frontend → presentación visual (colores dinámicos)
    /// ============================================================
    /// </summary>
    [Authorize]
    public class ReservasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReservasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // LISTAR RESERVAS
        // ============================================================
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Usuario")]
        public async Task<IActionResult> Create(Reserva reserva)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            //----------------------------------------------------------
            // VALIDACIÓN 1: Horario lógico
            //----------------------------------------------------------
            if (reserva.HoraFin <= reserva.HoraInicio)
                ModelState.AddModelError("",
                    "La hora de fin debe ser mayor que la hora de inicio.");

            //----------------------------------------------------------
            // VALIDACIÓN 2: No permitir reservas en el pasado
            //----------------------------------------------------------
            var fechaHoraInicio = reserva.Fecha.Date.Add(reserva.HoraInicio);

            if (fechaHoraInicio < DateTime.Now)
                ModelState.AddModelError("",
                    "No puedes crear reservas en una hora pasada.");

            //----------------------------------------------------------
            // VALIDACIÓN 3: Cruce de reservas (solo activas)
            //----------------------------------------------------------
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

            //----------------------------------------------------------
            // SI HAY ERRORES → REGRESAR
            //----------------------------------------------------------
            if (!ModelState.IsValid)
            {
                ViewData["SalaId"] =
                    new SelectList(_context.Salas, "Id", "Nombre", reserva.SalaId);

                return View(reserva);
            }

            //----------------------------------------------------------
            // GUARDADO
            //----------------------------------------------------------
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

            //----------------------------------------------------------
            // SEGURIDAD: solo dueño o admin
            //----------------------------------------------------------
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

            //----------------------------------------------------------
            // BLOQUEO: reservas ya finalizadas NO se editan
            //----------------------------------------------------------
            var fechaFinOriginal =
                reservaOriginal.Fecha.Date.Add(reservaOriginal.HoraFin);

            if (fechaFinOriginal < DateTime.Now)
                return Forbid();

            //----------------------------------------------------------
            // SEGURIDAD
            //----------------------------------------------------------
            if (!User.IsInRole("Administrador") &&
                reservaOriginal.UsuarioId != User.FindFirstValue(ClaimTypes.NameIdentifier))
                return Forbid();

            //----------------------------------------------------------
            // CONSERVAR DATOS CRÍTICOS
            //----------------------------------------------------------
            reserva.UsuarioId = reservaOriginal.UsuarioId;
            reserva.Estado = reservaOriginal.Estado;

            //----------------------------------------------------------
            // VALIDACIONES
            //----------------------------------------------------------
            if (reserva.HoraFin <= reserva.HoraInicio)
                ModelState.AddModelError("",
                    "La hora de fin debe ser mayor que la hora de inicio.");

            var fechaHoraInicio = reserva.Fecha.Date.Add(reserva.HoraInicio);

            if (fechaHoraInicio < DateTime.Now)
                ModelState.AddModelError("",
                    "No puedes editar a una hora pasada.");

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

            //----------------------------------------------------------
            // SI FALLA → REGRESAR
            //----------------------------------------------------------
            if (!ModelState.IsValid)
            {
                ViewData["SalaId"] =
                    new SelectList(_context.Salas, "Id", "Nombre", reserva.SalaId);

                return View(reserva);
            }

            //----------------------------------------------------------
            // UPDATE
            //----------------------------------------------------------
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

            if (reserva == null)
                return NotFound();

            //----------------------------------------------------------
            // BLOQUEO: no eliminar reservas ya finalizadas
            //----------------------------------------------------------
            var fechaFin = reserva.Fecha.Date.Add(reserva.HoraFin);

            if (fechaFin < DateTime.Now)
                return Forbid();

            _context.Reservas.Remove(reserva);
            await _context.SaveChangesAsync();

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
        // CALENDARIO
        // ============================================================
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
        // API FULLCALENDAR
        // ============================================================
        [Authorize]
        public async Task<IActionResult> GetReservas(
            DateTime start,
            DateTime end,
            int? salaId)
        {
            //----------------------------------------------------------
            // QUERY BASE
            //----------------------------------------------------------
            var query = _context.Reservas
                .Include(r => r.Sala)
                .Include(r => r.Usuario)
                .Where(r =>
                    r.Fecha >= start.Date &&
                    r.Fecha <= end.Date &&
                    r.Estado != EstadoReserva.Cancelada
                );

            if (salaId.HasValue)
                query = query.Where(r => r.SalaId == salaId.Value);

            var reservas = await query.ToListAsync();

            //----------------------------------------------------------
            // MAPEO A EVENTOS (FullCalendar)
            //----------------------------------------------------------
            var eventos = reservas.Select(r =>
            {
                var fechaFin = r.Fecha.Date.Add(r.HoraFin);

                //------------------------------------------------------
                // ESTADO DINÁMICO
                //------------------------------------------------------
                string estado;

                if (r.Estado == EstadoReserva.Cancelada)
                    estado = "cancelada";
                else if (fechaFin < DateTime.Now)
                    estado = "finalizada";
                else
                    estado = "activa";

                //------------------------------------------------------
                // COLOR BASE (FRONT LO TRANSFORMA)
                //------------------------------------------------------
                var colorBase = r.Sala?.ColorHex ?? "#3788d8";

                return new
                {
                    id = r.Id,
                    title = string.IsNullOrWhiteSpace(r.Motivo)
                        ? $"Reserva - {r.Sala?.Nombre ?? "Sala"}"
                        : r.Motivo,

                    start = r.Fecha.Date.Add(r.HoraInicio),
                    end = r.Fecha.Date.Add(r.HoraFin),

                    backgroundColor = colorBase,
                    borderColor = colorBase,

                    estado = estado,

                    usuario = r.Usuario?.Email ?? "Sin usuario",
                    sala = r.Sala?.Nombre ?? "Sin sala"
                };
            });

            return Json(eventos);
        }
    }
}