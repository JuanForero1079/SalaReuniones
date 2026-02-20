using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using SalaReuniones.Data;
using SalaReuniones.Models;

namespace SalaReuniones.Controllers
{
    [Authorize] // Solo usuarios logueados pueden acceder
    public class ReservasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReservasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // GET: Reservas
        // ============================================================
        public async Task<IActionResult> Index()
        {
            // Traer todas las reservas e incluir la Sala relacionada
            var reservasQuery = _context.Reservas.Include(r => r.Sala).AsQueryable();

            // Si el usuario es "Usuario" (no Admin), solo mostrar sus propias reservas
            if (User.IsInRole("Usuario"))
            {
                var userEmail = User.Identity!.Name!;
                reservasQuery = reservasQuery.Where(r => r.Nombre == userEmail);
            }

            var listaReservas = await reservasQuery.ToListAsync();
            return View(listaReservas);
        }

        // ============================================================
        // GET: Reservas/Details/5
        // ============================================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var reserva = await _context.Reservas
                .Include(r => r.Sala)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (reserva == null) return NotFound();

            return View(reserva);
        }

        // ============================================================
        // GET: Reservas/Create
        // ============================================================
        [Authorize(Roles = "Administrador,Usuario")]
        public IActionResult Create()
        {
            ViewData["SalaId"] = new SelectList(_context.Salas, "Id", "Nombre");
            return View();
        }

        // ============================================================
        // POST: Reservas/Create
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Usuario")]
        public async Task<IActionResult> Create([Bind("Id,Nombre,Fecha,HoraInicio,HoraFin,Motivo,Estado,SalaId")] Reserva reserva)
        {
            if (reserva.HoraFin <= reserva.HoraInicio)
                ModelState.AddModelError("", "La hora de fin debe ser mayor que la hora de inicio.");

            bool existeCruce = await _context.Reservas.AnyAsync(r =>
                r.SalaId == reserva.SalaId &&
                r.Fecha.Date == reserva.Fecha.Date &&
                r.Estado == "Activa" &&
                r.HoraInicio < reserva.HoraFin &&
                r.HoraFin > reserva.HoraInicio
            );

            if (existeCruce)
                ModelState.AddModelError("", "Ya existe una reserva en ese horario para esta sala.");

            if (ModelState.IsValid)
            {
                _context.Add(reserva);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["SalaId"] = new SelectList(_context.Salas, "Id", "Nombre", reserva.SalaId);
            return View(reserva);
        }

        // ============================================================
        // GET: Reservas/Edit/5
        // ============================================================
        [Authorize(Roles = "Administrador,Usuario")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var reserva = await _context.Reservas.FindAsync(id);
            if (reserva == null) return NotFound();

            // Solo Admin o Usuario dueño puede editar
            if (!User.IsInRole("Administrador") && !(User.IsInRole("Usuario") && reserva.Nombre == User.Identity!.Name))
                return Forbid();

            ViewData["SalaId"] = new SelectList(_context.Salas, "Id", "Nombre", reserva.SalaId);
            return View(reserva);
        }

        // ============================================================
        // POST: Reservas/Edit/5
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Usuario")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Fecha,HoraInicio,HoraFin,Motivo,Estado,SalaId")] Reserva reserva)
        {
            if (id != reserva.Id) return NotFound();

            if (reserva.HoraFin <= reserva.HoraInicio)
                ModelState.AddModelError("", "La hora de fin debe ser mayor que la hora de inicio.");

            bool existeCruce = await _context.Reservas.AnyAsync(r =>
                r.Id != reserva.Id &&
                r.SalaId == reserva.SalaId &&
                r.Fecha.Date == reserva.Fecha.Date &&
                r.Estado == "Activa" &&
                r.HoraInicio < reserva.HoraFin &&
                r.HoraFin > reserva.HoraInicio
            );

            if (existeCruce)
                ModelState.AddModelError("", "Ya existe una reserva en ese horario para esta sala.");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(reserva);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReservaExists(reserva.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["SalaId"] = new SelectList(_context.Salas, "Id", "Nombre", reserva.SalaId);
            return View(reserva);
        }

        // ============================================================
        // GET: Reservas/Delete/5
        // ============================================================
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var reserva = await _context.Reservas
                .Include(r => r.Sala)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (reserva == null) return NotFound();

            return View(reserva);
        }

        // ============================================================
        // POST: Reservas/Delete/5
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

        // ============================================================
        // Método auxiliar para verificar existencia
        // ============================================================
        private bool ReservaExists(int id) => _context.Reservas.Any(e => e.Id == id);
    }
}