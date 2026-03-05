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
    /// Funcionalidades:
    /// - Dashboard administrativo
    /// - Filtros por rango de fechas
    /// - API JSON para Chart.js
    /// - Exportación a PDF profesional con logo
    ///
    /// Seguridad:
    /// SOLO Administradores
    /// ============================================================
    /// </summary>
    [Authorize(Roles = "Administrador")]
    public class ReportesController : Controller
    {
        //----------------------------------------------------------
        // INYECCIÓN DE DEPENDENCIAS
        //----------------------------------------------------------
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ReportesController(
            ApplicationDbContext context,
            IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ============================================================
        // DASHBOARD ADMINISTRATIVO
        // URL: /Reportes/Dashboard
        // ============================================================
        public async Task<IActionResult> Dashboard(
            int? dias,
            DateTime? fechaInicio,
            DateTime? fechaFin)
        {
            //----------------------------------------------------------
            // QUERY BASE
            //----------------------------------------------------------
            var query = _context.Reservas.AsQueryable();

            //----------------------------------------------------------
            // FILTRO POR ÚLTIMOS DÍAS
            //----------------------------------------------------------
            if (dias.HasValue)
            {
                var desde = DateTime.Today.AddDays(-dias.Value);
                query = query.Where(r => r.Fecha >= desde);

                ViewBag.FiltroActivo = $"Últimos {dias} días";
            }

            //----------------------------------------------------------
            // FILTRO POR RANGO DE FECHAS
            //----------------------------------------------------------
            if (fechaInicio.HasValue && fechaFin.HasValue)
            {
                query = query.Where(r =>
                    r.Fecha >= fechaInicio.Value &&
                    r.Fecha <= fechaFin.Value);

                ViewBag.FiltroActivo =
                    $"{fechaInicio:dd/MM/yyyy} - {fechaFin:dd/MM/yyyy}";
            }

            //----------------------------------------------------------
            // EJECUTAR CONSULTA
            //----------------------------------------------------------
            var reservas = await query.ToListAsync();
            var hoy = DateTime.Today;

            //----------------------------------------------------------
            // KPIs PARA EL DASHBOARD
            //----------------------------------------------------------
            ViewBag.TotalReservas = reservas.Count;

            ViewBag.ReservasHoy =
                reservas.Count(r => r.Fecha.Date == hoy);

            ViewBag.TotalUsuarios =
                await _context.Users.CountAsync();

            ViewBag.ReservasActivas =
                reservas.Count(r => r.Estado == EstadoReserva.Activa);

            return View();
        }

        // ============================================================
        // API JSON PARA GRÁFICAS (Chart.js)
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
            // APLICAR FILTROS
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

            //----------------------------------------------------------
            // RESERVAS ACTIVAS POR SALA
            //----------------------------------------------------------
            var reservasPorSala = await query
                .Where(r => r.Estado == EstadoReserva.Activa)
                .GroupBy(r => r.Sala!.Nombre)
                .Select(g => new
                {
                    sala = g.Key,
                    total = g.Count()
                })
                .ToListAsync();

            //----------------------------------------------------------
            // RESERVAS POR DÍA
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
        // EXPORTAR REPORTE A PDF
        // ============================================================
        public async Task<IActionResult> ExportarPDF(
            int? dias,
            DateTime? fechaInicio,
            DateTime? fechaFin)
        {
            //----------------------------------------------------------
            // QUERY BASE + FILTROS
            //----------------------------------------------------------
            var query = _context.Reservas
                .Include(r => r.Sala)
                .AsQueryable();

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

            //----------------------------------------------------------
            // CARGAR LOGO DESDE wwwroot/images
            //----------------------------------------------------------
            var logoPath = Path.Combine(
                _env.WebRootPath,
                "images",
                "HaloLogoBlue.png");

            byte[]? logoBytes = null;

            if (System.IO.File.Exists(logoPath))
                logoBytes = System.IO.File.ReadAllBytes(logoPath);

            //----------------------------------------------------------
            // CREAR DOCUMENTO PDF
            //----------------------------------------------------------
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);

                    // ==================================================
                    // HEADER
                    // ==================================================
                    page.Header()
                        .Height(90)
                        .Column(col =>
                        {
                            col.Item().Row(row =>
                            {
                                //--------------------------------------------------
                                // LOGO
                                //--------------------------------------------------
                                if (logoBytes != null)
                                {
                                    row.ConstantItem(80)
                                       .Image(logoBytes)
                                       .FitArea();
                                }

                                //--------------------------------------------------
                                // TITULOS
                                //--------------------------------------------------
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("SISTEMA SALA DE REUNIONES")
                                        .FontSize(18)
                                        .Bold();

                                    c.Item().Text("Reporte Administrativo")
                                        .FontSize(12);

                                    c.Item().Text(
                                        $"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}")
                                        .FontSize(10);
                                });
                            });

                            //--------------------------------------------------
                            // LÍNEA CORPORATIVA
                            //--------------------------------------------------
                            col.Item()
                               .PaddingTop(5)
                               .LineHorizontal(1);
                        });

                    // ==================================================
                    // CONTENIDO
                    // ==================================================
                    page.Content().PaddingVertical(20).Column(col =>
                    {
                        //--------------------------------------------------
                        // KPIs
                        //--------------------------------------------------
                        col.Item().Row(row =>
                        {
                            row.RelativeItem()
                                .Background(Colors.Grey.Lighten3)
                                .Padding(10)
                                .Column(kpi =>
                                {
                                    kpi.Item().Text("Total Reservas").Bold();
                                    kpi.Item().Text(reservas.Count.ToString())
                                        .FontSize(16).Bold();
                                });

                            row.RelativeItem()
                                .Background(Colors.Grey.Lighten3)
                                .Padding(10)
                                .Column(kpi =>
                                {
                                    kpi.Item().Text("Reservas Activas").Bold();
                                    kpi.Item().Text(
                                        reservas.Count(r => r.Estado == EstadoReserva.Activa).ToString())
                                        .FontSize(16).Bold();
                                });

                            row.RelativeItem()
                                .Background(Colors.Grey.Lighten3)
                                .Padding(10)
                                .Column(kpi =>
                                {
                                    kpi.Item().Text("Reservas Hoy").Bold();
                                    kpi.Item().Text(
                                        reservas.Count(r => r.Fecha.Date == DateTime.Today).ToString())
                                        .FontSize(16).Bold();
                                });
                        });

                        col.Item().PaddingTop(20);

                        //--------------------------------------------------
                        // TABLA DE RESERVAS
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

                            //--------------------------------------------------
                            // HEADER TABLA
                            //--------------------------------------------------
                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Blue.Lighten2).Padding(5).Text("Fecha").Bold();
                                header.Cell().Background(Colors.Blue.Lighten2).Padding(5).Text("Sala").Bold();
                                header.Cell().Background(Colors.Blue.Lighten2).Padding(5).Text("Estado").Bold();
                                header.Cell().Background(Colors.Blue.Lighten2).Padding(5).Text("Hora").Bold();
                            });

                            //--------------------------------------------------
                            // FILAS
                            //--------------------------------------------------
                            foreach (var r in reservas)
                            {
                                table.Cell().Padding(5).Text(r.Fecha.ToString("dd/MM/yyyy"));
                                table.Cell().Padding(5).Text(r.Sala?.Nombre ?? "");
                                table.Cell().Padding(5).Text(r.Estado.ToString());
                                table.Cell().Padding(5).Text($"{r.HoraInicio} - {r.HoraFin}");
                            }
                        });
                    });

                    // ==================================================
                    // FOOTER
                    // ==================================================
                    page.Footer()
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.DefaultTextStyle(x => x.FontSize(9));


                            text.Span("Sistema desarrollado en .NET 8 | Página ");
                            text.CurrentPageNumber();
                            text.Span(" de ");
                            text.TotalPages();
                        });
                });
            });

            //----------------------------------------------------------
            // GENERAR PDF
            //----------------------------------------------------------
            var pdfBytes = document.GeneratePdf();

            return File(
                pdfBytes,
                "application/pdf",
                "ReporteReservas.pdf");
        }
    }
}