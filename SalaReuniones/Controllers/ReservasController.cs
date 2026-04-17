using System.Text;
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
    /// ✔ Cancelación lógica de reservas
    /// ✔ Estado calculado dinámicamente (activa, finalizada, cancelada)
    ///
    /// ARQUITECTURA:
    /// Backend → lógica de negocio + estado
    /// Frontend → presentación visual (colores dinámicos)
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
        public async Task<IActionResult> Index(int pagina = 1)
        {
            int registrosPorPagina = 10;

            var query = _context.Reservas
                .Include(r => r.Sala)
                .Include(r => r.Usuario)
                .OrderByDescending(r => r.Fecha)
                .ThenBy(r => r.HoraInicio);

            int totalRegistros = await query.CountAsync();

            var reservas = await query
                .Skip((pagina - 1) * registrosPorPagina)
                .Take(registrosPorPagina)
                .ToListAsync();

            ViewBag.TotalPaginas = (int)Math.Ceiling((double)totalRegistros / registrosPorPagina);
            ViewBag.PaginaActual = pagina;

            return View(reservas);
        }

        // ============================================================
        // CREAR RESERVA (GET)
        // ============================================================
        [Authorize(Roles = "Administrador,Usuario")]
        public IActionResult Create()
        {
            ViewData["SalaId"] = new SelectList(_context.Salas, "Id", "Nombre");
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
                ModelState.AddModelError("", "La hora de fin debe ser mayor que la hora de inicio.");

            //----------------------------------------------------------
            // VALIDACIÓN 2: No permitir reservas en el pasado
            //----------------------------------------------------------

            var ahora = DateTime.Now;

            var fechaHoraInicio = reserva.Fecha.Date.Add(reserva.HoraInicio);

            if (reserva.Fecha.Date == ahora.Date && fechaHoraInicio < ahora)
            {
                ModelState.AddModelError("", "No puedes crear reservas en una hora pasada.");
            }

            if (reserva.Fecha.Date < ahora.Date)
            {
                ModelState.AddModelError("", "No puedes crear reservas en fechas pasadas.");
            }

            //----------------------------------------------------------
            // VALIDACIÓN 3: Cruce de reservas en la MISMA SALA
            //----------------------------------------------------------
            bool existeCruce = await _context.Reservas.AnyAsync(r =>
                r.SalaId == reserva.SalaId &&
                r.Fecha.Date == reserva.Fecha.Date &&
                r.Estado == EstadoReserva.Activa &&
                r.HoraInicio < reserva.HoraFin &&
                r.HoraFin > reserva.HoraInicio
            );

            //----------------------------------------------------------
            // VALIDACIÓN 4: Usuario no puede reservar dos salas
            // al mismo tiempo
            //----------------------------------------------------------
            bool usuarioOcupado = await _context.Reservas.AnyAsync(r =>
                r.UsuarioId == userId &&
                r.Fecha.Date == reserva.Fecha.Date &&
                r.Estado == EstadoReserva.Activa &&
                r.HoraInicio < reserva.HoraFin &&
                r.HoraFin > reserva.HoraInicio
            );

            if (usuarioOcupado)
                ModelState.AddModelError("", "Ya tienes otra reserva en ese horario.");

            if (existeCruce)
                ModelState.AddModelError("", "Ya existe una reserva en ese horario para esta sala.");

            //----------------------------------------------------------
            // SI HAY ERRORES
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
            if (id == null)
                return NotFound();

            var reserva = await _context.Reservas.FindAsync(id);

            if (reserva == null)
                return NotFound();

            //----------------------------------------------------------
            // SEGURIDAD
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
            // BLOQUEO: reservas finalizadas
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
            // CONSERVAR DATOS
            //----------------------------------------------------------
            reserva.UsuarioId = reservaOriginal.UsuarioId;
            reserva.Estado = reservaOriginal.Estado;

            //----------------------------------------------------------
            // VALIDACIÓN 1: horario lógico
            //----------------------------------------------------------
            if (reserva.HoraFin <= reserva.HoraInicio)
                ModelState.AddModelError("", "La hora de fin debe ser mayor que la hora de inicio.");

            //----------------------------------------------------------
            // VALIDACIÓN 2: no permitir editar al pasado
            //----------------------------------------------------------
            var ahora = DateTime.Now;
            var fechaHoraInicio = reserva.Fecha.Date.Add(reserva.HoraInicio);

            if (reserva.Fecha.Date == ahora.Date && fechaHoraInicio < ahora)
            {
                ModelState.AddModelError("", "No puedes editar a una hora pasada.");
            }

            if (reserva.Fecha.Date < ahora.Date)
            {
                ModelState.AddModelError("", "No puedes editar a fechas pasadas.");
            }

            //----------------------------------------------------------
            // VALIDACIÓN 3: cruce de reservas en la misma sala
            //----------------------------------------------------------
            bool existeCruce = await _context.Reservas.AnyAsync(r =>
                r.Id != reserva.Id &&
                r.SalaId == reserva.SalaId &&
                r.Fecha.Date == reserva.Fecha.Date &&
                r.Estado == EstadoReserva.Activa &&
                r.HoraInicio < reserva.HoraFin &&
                r.HoraFin > reserva.HoraInicio
            );

            //----------------------------------------------------------
            // VALIDACIÓN 4: usuario no puede tener otra reserva
            // en el mismo horario
            //----------------------------------------------------------
            bool usuarioOcupado = await _context.Reservas.AnyAsync(r =>
                r.Id != reserva.Id &&
                r.UsuarioId == reserva.UsuarioId &&
                r.Fecha.Date == reserva.Fecha.Date &&
                r.Estado == EstadoReserva.Activa &&
                r.HoraInicio < reserva.HoraFin &&
                r.HoraFin > reserva.HoraInicio
            );

            if (usuarioOcupado)
                ModelState.AddModelError("", "Ya tienes otra reserva en ese horario.");

            if (existeCruce)
                ModelState.AddModelError("", "Ya existe una reserva en ese horario.");

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
        // CANCELAR RESERVA (SOFT DELETE)
        // ============================================================
        [Authorize(Roles = "Administrador,Usuario")]
        public async Task<IActionResult> Cancelar(int id)
        {
            var reserva = await _context.Reservas.FindAsync(id);

            if (reserva == null)
                return NotFound();

            if (!User.IsInRole("Administrador") &&
                reserva.UsuarioId != User.FindFirstValue(ClaimTypes.NameIdentifier))
                return Forbid();

            var fechaFin = reserva.Fecha.Date.Add(reserva.HoraFin);

            if (fechaFin < DateTime.Now)
                return Forbid();

            reserva.Estado = EstadoReserva.Cancelada;

            _context.Update(reserva);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // ELIMINAR RESERVA
        // ============================================================
        //[Authorize(Roles = "Administrador")]
        //public async Task<IActionResult> Delete(int? id)
        //{
          //  if (id == null)
            //    return NotFound();

            //var reserva = await _context.Reservas
               // .Include(r => r.Sala)
                //.Include(r => r.Usuario)
                //.FirstOrDefaultAsync(m => m.Id == id);
                
            //if (reserva == null)
              //  return NotFound();

        //    return View(reserva);
        //}

        //[HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        //[Authorize(Roles = "Administrador")]
        //public async Task<IActionResult> DeleteConfirmed(int id)
        //{
            //var reserva = await _context.Reservas.FindAsync(id);

           // if (reserva == null)
             //   return NotFound();

           // var fechaFin = reserva.Fecha.Date.Add(reserva.HoraFin);

         //   if (fechaFin < DateTime.Now)
        //        return Forbid();

        //   _context.Reservas.Remove(reserva);
        //    await _context.SaveChangesAsync();

        //    return RedirectToAction(nameof(Index));
        //}

        // ============================================================
        // DETALLES
        // ============================================================
        [Authorize(Roles = "Administrador,Usuario,Visualizador")]
        public async Task<IActionResult> Details(int? id)
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

            var eventos = reservas.Select(r =>
            {
                var fechaFin = r.Fecha.Date.Add(r.HoraFin);

                string estado;

                if (r.Estado == EstadoReserva.Cancelada)
                    estado = "cancelada";
                else if (fechaFin < DateTime.Now)
                    estado = "finalizada";
                else
                    estado = "activa";

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
                    usuario = r.Usuario?.UserName ?? "Sin usuario",
                    sala = r.Sala?.Nombre ?? "Sin sala"
                };
            });

            return Json(eventos);
        }

        // ============================================================
        // GENERAR ARCHIVO ICS
        // ============================================================
        private byte[] GenerarICS(Reserva reserva)
        {
            var inicio = reserva.Fecha.Date.Add(reserva.HoraInicio)
                .ToUniversalTime()
                .ToString("yyyyMMddTHHmmssZ");

            var fin = reserva.Fecha.Date.Add(reserva.HoraFin)
                .ToUniversalTime()
                .ToString("yyyyMMddTHHmmssZ");

            var uid = Guid.NewGuid().ToString();

            var ics = $@"BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//Sistema Salas//ES
BEGIN:VEVENT
UID:{uid}
SUMMARY:Reserva - {reserva.Sala?.Nombre}
DESCRIPTION:{reserva.Motivo}
DTSTART:{inicio}
DTEND:{fin}
LOCATION:{reserva.Sala?.Nombre}
STATUS:CONFIRMED

BEGIN:VALARM
TRIGGER:-PT15M
ACTION:DISPLAY
DESCRIPTION:Recordatorio reunión
END:VALARM

END:VEVENT
END:VCALENDAR";

            return Encoding.UTF8.GetBytes(ics);
        }

        // ============================================================
        // DESCARGAR EVENTO CALENDARIO (.ICS)
        // ============================================================
        [Authorize]
        public async Task<IActionResult> DescargarICS(int id)
        {
            var reserva = await _context.Reservas
                .Include(r => r.Sala)
                .Include(r => r.Usuario)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reserva == null)
                return NotFound();

            //----------------------------------------------------------
            // VALIDACIÓN DE SEGURIDAD
            // Solo el usuario que creó la reserva puede descargar
            // su evento de calendario.
            //----------------------------------------------------------

            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (reserva.UsuarioId != usuarioId)
                return Forbid();

            var archivo = GenerarICS(reserva);

            return File(
                archivo,
                "text/calendar",
                $"reserva_{reserva.Id}.ics"
            );

        }
    }

}
