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
    //  Solo usuarios autenticados pueden acceder al controlador
    [Authorize]
    public class ReservasController : Controller
    {
        private readonly ApplicationDbContext _context;

        //  Inyección de dependencia del contexto de base de datos
        public ReservasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ============================================================
        //  LISTAR RESERVAS
        // ============================================================
        public async Task<IActionResult> Index()
        {
            /*
                Incluye relaciones:
                - Sala
                - Usuario que creó la reserva
            */
            var reservas = await _context.Reservas
                .Include(r => r.Sala)
                .Include(r => r.Usuario)
                .OrderByDescending(r => r.Fecha)
                .ThenBy(r => r.HoraInicio)
                .ToListAsync();

            return View(reservas);
        }

        // ============================================================
        //  GET: CREAR RESERVA
        // ============================================================
        [Authorize(Roles = "Administrador,Usuario")]
        public IActionResult Create()
        {
            // 🔹 Carga lista de salas en el dropdown
            ViewData["SalaId"] = new SelectList(_context.Salas, "Id", "Nombre");

            return View();
        }

        // ============================================================
        //  POST: CREAR RESERVA
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken] // 🔐 Protección CSRF
        [Authorize(Roles = "Administrador,Usuario")]
        public async Task<IActionResult> Create(Reserva reserva)
        {
            //  Obtener ID del usuario logueado
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // ================================
            // VALIDACIÓN 1: Hora correcta
            // ================================
            if (reserva.HoraFin <= reserva.HoraInicio)
            {
                ModelState.AddModelError("",
                    "La hora de fin debe ser mayor que la hora de inicio.");
            }

            // ================================
            // VALIDACIÓN 2: Cruce de horarios
            // ================================
            bool existeCruce = await _context.Reservas.AnyAsync(r =>
                r.SalaId == reserva.SalaId &&
                r.Fecha.Date == reserva.Fecha.Date &&
                r.Estado == EstadoReserva.Activa &&
                r.HoraInicio < reserva.HoraFin &&
                r.HoraFin > reserva.HoraInicio
            );

            if (existeCruce)
            {
                ModelState.AddModelError("",
                    "Ya existe una reserva en ese horario para esta sala.");
            }

            // ================================
            // SI HAY ERRORES → REGRESAR VISTA
            // ================================
            if (!ModelState.IsValid)
            {
                // 🔁 Volver a cargar dropdown
                ViewData["SalaId"] = new SelectList(_context.Salas,
                    "Id",
                    "Nombre",
                    reserva.SalaId);

                return View(reserva);
            }

            // ================================
            // ASIGNAR DATOS AUTOMÁTICOS
            // ================================
            reserva.UsuarioId = userId;            // Usuario actual
            reserva.Estado = EstadoReserva.Activa; // Estado inicial

            // ================================
            // GUARDAR EN BASE DE DATOS
            // ================================
            _context.Add(reserva);
            await _context.SaveChangesAsync();

            //  Redirigir al listado
            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        //  GET: EDITAR
        // ============================================================
        [Authorize(Roles = "Administrador,Usuario")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var reserva = await _context.Reservas.FindAsync(id);

            if (reserva == null)
                return NotFound();

            //  Solo el dueño o administrador puede editar
            if (!User.IsInRole("Administrador") &&
                reserva.UsuarioId != User.FindFirstValue(ClaimTypes.NameIdentifier))
                return Forbid();

            ViewData["SalaId"] = new SelectList(_context.Salas,
                "Id",
                "Nombre",
                reserva.SalaId);

            return View(reserva);
        }

        // ============================================================
        // POST: EDITAR
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

            // Validar permisos
            if (!User.IsInRole("Administrador") &&
                reservaOriginal.UsuarioId != User.FindFirstValue(ClaimTypes.NameIdentifier))
                return Forbid();

            // Mantener datos críticos
            reserva.UsuarioId = reservaOriginal.UsuarioId;
            reserva.Estado = reservaOriginal.Estado;

            // Validaciones iguales que en Create
            if (reserva.HoraFin <= reserva.HoraInicio)
                ModelState.AddModelError("",
                    "La hora de fin debe ser mayor que la hora de inicio.");

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
                ViewData["SalaId"] = new SelectList(_context.Salas,
                    "Id",
                    "Nombre",
                    reserva.SalaId);

                return View(reserva);
            }

            try
            {
                _context.Update(reserva);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Reservas.Any(e => e.Id == reserva.Id))
                    return NotFound();
                else
                    throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        //  GET: DELETE
        // ============================================================
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var reserva = await _context.Reservas
                .Include(r => r.Sala)
                .Include(r => r.Usuario)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (reserva == null)
                return NotFound();

            return View(reserva);
        }

        // ============================================================
        //  POST: DELETE
        // ============================================================
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
    }
}