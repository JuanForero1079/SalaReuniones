using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using SalaReuniones.Data;
using SalaReuniones.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;

namespace SalaReuniones.Controllers
{
    /// <summary>
    /// ============================================================
    /// CONTROLADOR DE REPORTES Y ESTADÍSTICAS
    /// ------------------------------------------------------------
    /// ✔ Dashboard administrativo
    /// ✔ Filtros por fechas
    /// ✔ API para Chart.js
    /// ✔ Exportación PDF profesional
    ///
    ///  SOLO Administradores
    /// ============================================================
    /// </summary>
    [Authorize(Roles = "Administrador")]
    public class ReportesController : Controller
    {
        //----------------------------------------------------------
        // DEPENDENCIAS
        //----------------------------------------------------------
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ReportesController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ============================================================
        // DASHBOARD ADMINISTRATIVO
        // ============================================================
        public async Task<IActionResult> Dashboard(int? dias, DateTime? fechaInicio, DateTime? fechaFin)
        {
            var query = _context.Reservas.AsQueryable();

            //----------------------------------------------------------
            // FILTROS
            //----------------------------------------------------------
            if (dias.HasValue)
            {
                var desde = DateTime.Today.AddDays(-dias.Value);
                query = query.Where(r => r.Fecha >= desde);
                ViewBag.FiltroActivo = $"Últimos {dias} días";
            }

            if (fechaInicio.HasValue && fechaFin.HasValue)
            {
                query = query.Where(r =>
                    r.Fecha >= fechaInicio.Value &&
                    r.Fecha <= fechaFin.Value);

                ViewBag.FiltroActivo = $"{fechaInicio:dd/MM/yyyy} - {fechaFin:dd/MM/yyyy}";
            }

            //----------------------------------------------------------
            // EJECUCIÓN
            //----------------------------------------------------------
            var reservas = await query.ToListAsync();
            var hoy = DateTime.Today;
            var ahora = DateTime.Now;

            //----------------------------------------------------------
            // KPIs
            //----------------------------------------------------------
            ViewBag.TotalReservas = reservas.Count;

            ViewBag.ReservasHoy =
                reservas.Count(r => r.Fecha.Date == hoy);

            ViewBag.TotalUsuarios =
                await _context.Users.CountAsync();

            //  LÓGICA REAL (NO depende de Estado)
            ViewBag.ReservasActivas =
                reservas.Count(r => r.Fecha.Date.Add(r.HoraFin) >= ahora);

            ViewBag.ReservasFinalizadas =
                reservas.Count(r => r.Fecha.Date.Add(r.HoraFin) < ahora);

            return View();
        }

        // ============================================================
        // API PARA GRÁFICAS (Chart.js)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> GetDashboardData(
            int? dias,
            DateTime? fechaInicio,
            DateTime? fechaFin)
        {
            var query = _context.Reservas
                .Include(r => r.Sala)
                .AsQueryable();

            //----------------------------------------------------------
            // FILTROS
            //----------------------------------------------------------
            if (dias.HasValue)
            {
                var desde = DateTime.Today.AddDays(-dias.Value);
                query = query.Where(r => r.Fecha >= desde);
            }

            if (fechaInicio.HasValue && fechaFin.HasValue)
            {
                query = query.Where(r =>
                    r.Fecha >= fechaInicio &&
                    r.Fecha <= fechaFin);
            }

            var ahora = DateTime.Now;

            //----------------------------------------------------------
            // RESERVAS ACTIVAS POR SALA
            //----------------------------------------------------------
            var reservasPorSala = await query
                .Where(r => r.Fecha.Date.Add(r.HoraFin) >= ahora)
                .GroupBy(r => r.Sala!.Nombre)
                .Select(g => new
                {
                    sala = g.Key,
                    total = g.Count()
                })
                .ToListAsync();

            //----------------------------------------------------------
            // RESERVAS POR DÍA (histórico completo)
            //----------------------------------------------------------
            var reservasPorDia = await query
                .GroupBy(r => r.Fecha.Date)
                .Select(g => new
                {
                    fecha = g.Key,
                    total = g.Count()
                })
                .OrderBy(x => x.fecha)
                .ToListAsync();

            return Json(new
            {
                reservasPorSala,
                reservasPorDia
            });
        }

        // ============================================================
        // EXPORTAR PDF
        // ============================================================
        public async Task<IActionResult> ExportarPDF(
            int? dias,
            DateTime? fechaInicio,
            DateTime? fechaFin)
        {
            var query = _context.Reservas
                .Include(r => r.Sala)
                .AsQueryable();

            //----------------------------------------------------------
            // FILTROS
            //----------------------------------------------------------
            if (dias.HasValue)
            {
                var desde = DateTime.Today.AddDays(-dias.Value);
                query = query.Where(r => r.Fecha >= desde);
            }

            if (fechaInicio.HasValue && fechaFin.HasValue)
            {
                query = query.Where(r =>
                    r.Fecha >= fechaInicio &&
                    r.Fecha <= fechaFin);
            }

            var reservas = await query.ToListAsync();
            var ahora = DateTime.Now;

            //----------------------------------------------------------
            // LOGO
            //----------------------------------------------------------
            var logoPath = Path.Combine(_env.WebRootPath, "images", "HaloLogoBlue.png");

            byte[]? logoBytes = null;

            if (System.IO.File.Exists(logoPath))
                logoBytes = System.IO.File.ReadAllBytes(logoPath);

            //----------------------------------------------------------
            // PDF
            //----------------------------------------------------------
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);

                    // HEADER
                    page.Header().Height(90).Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            if (logoBytes != null)
                                row.ConstantItem(80).Image(logoBytes).FitArea();

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("SISTEMA SALA DE REUNIONES").FontSize(18).Bold();
                                c.Item().Text("Reporte Administrativo").FontSize(12);
                                c.Item().Text($"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(10);
                            });
                        });

                        col.Item().PaddingTop(5).LineHorizontal(1);
                    });

                    // CONTENIDO
                    page.Content().PaddingVertical(20).Column(col =>
                    {
                        //--------------------------------------------------
                        // KPIs
                        //--------------------------------------------------
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Background(Colors.Grey.Lighten3).Padding(10).Column(kpi =>
                            {
                                kpi.Item().Text("Total Reservas").Bold();
                                kpi.Item().Text(reservas.Count.ToString()).FontSize(16).Bold();
                            });

                            row.RelativeItem().Background(Colors.Grey.Lighten3).Padding(10).Column(kpi =>
                            {
                                kpi.Item().Text("Reservas Activas").Bold();
                                kpi.Item().Text(
                                    reservas.Count(r => r.Fecha.Date.Add(r.HoraFin) >= ahora).ToString()
                                ).FontSize(16).Bold();
                            });

                            row.RelativeItem().Background(Colors.Grey.Lighten3).Padding(10).Column(kpi =>
                            {
                                kpi.Item().Text("Reservas Hoy").Bold();
                                kpi.Item().Text(
                                    reservas.Count(r => r.Fecha.Date == DateTime.Today).ToString()
                                ).FontSize(16).Bold();
                            });
                        });

                        col.Item().PaddingTop(20);

                        //--------------------------------------------------
                        // TABLA
                        //--------------------------------------------------
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Blue.Lighten2).Padding(5).Text("Fecha").Bold();
                                header.Cell().Background(Colors.Blue.Lighten2).Padding(5).Text("Sala").Bold();
                                header.Cell().Background(Colors.Blue.Lighten2).Padding(5).Text("Estado").Bold();
                                header.Cell().Background(Colors.Blue.Lighten2).Padding(5).Text("Hora").Bold();
                            });

                            foreach (var r in reservas)
                            {
                                var fechaFin = r.Fecha.Date.Add(r.HoraFin);

                                string estado =
                                    r.Estado == EstadoReserva.Cancelada ? "Cancelada" :
                                    fechaFin < ahora ? "Finalizada" :
                                    "Activa";

                                table.Cell().Padding(5).Text(r.Fecha.ToString("dd/MM/yyyy"));
                                table.Cell().Padding(5).Text(r.Sala?.Nombre ?? "");
                                table.Cell().Padding(5).Text(estado);
                                table.Cell().Padding(5).Text($"{r.HoraInicio} - {r.HoraFin}");
                            }
                        });
                    });

                    // FOOTER
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.DefaultTextStyle(x => x.FontSize(9));
                        text.Span("Sistema desarrollado en .NET 8 | Página ");
                        text.CurrentPageNumber();
                        text.Span(" de ");
                        text.TotalPages();
                    });
                });
            });

            var pdfBytes = document.GeneratePdf();

            return File(pdfBytes, "application/pdf", "ReporteReservas.pdf");
        }
    }
}