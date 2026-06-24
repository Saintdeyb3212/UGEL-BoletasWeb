using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using UGEL_BoletasWeb.Data.Context;
using UGEL_BoletasWeb.Models.ViewModels;
using UGEL_BoletasWeb.Services.PdfExport;

namespace UGEL_BoletasWeb.Controllers
{
    [Authorize(Roles = "Administrador,Pagaduria")]
    public class PagaduriaController : Controller
    {
        private readonly UgelDbContext _context;
        private readonly IGeneradorBoletaPdf _pdfGenerator;

        public PagaduriaController(UgelDbContext context, IGeneradorBoletaPdf pdfGenerator)
        {
            _context = context;
            _pdfGenerator = pdfGenerator;
        }

        // ====================================================================
        // 1. MOTOR DE BÚSQUEDA GET (Blindado, sin límites de memoria)
        // ====================================================================
        [HttpGet]
        public async Task<IActionResult> Consultar(FiltroConsultaViewModel filtro)
        {
            // 🚀 BLINDAJE 1: Limpieza absoluta si la petición GET viene vacía o incompleta
            if (string.IsNullOrWhiteSpace(filtro.DNI) || string.IsNullOrWhiteSpace(filtro.Anio))
            {
                // Limpia la pantalla para que el F5 o el botón "Limpiar" no arrastre basura
                ModelState.Clear();
                return View(new FiltroConsultaViewModel());
            }

            try
            {
                var query = _context.BoletasCabecera
                    .Include(b => b.Detalles)
                    .Where(b => b.DNI == filtro.DNI.Trim() && b.Anio == filtro.Anio.Trim());

                if (!string.IsNullOrWhiteSpace(filtro.Mes))
                {
                    query = query.Where(b => b.Mes == filtro.Mes.Trim());
                }

                // 🚀 BLINDAJE 2: Escudo Anti-RAM (Take 50). Evita que un DNI corrupto sature el servidor.
                filtro.Resultados = await query
                    .OrderByDescending(b => b.Mes)
                    .Take(50)
                    .ToListAsync();

                if (!filtro.Resultados.Any())
                {
                    ViewBag.MensajeAviso = $"No se encontraron boletas registradas para el DNI {filtro.DNI} en el periodo solicitado.";
                }
            }
            catch (System.Exception)
            {
                ViewBag.MensajeAviso = "Alerta: No se pudo conectar a la Base de Datos. Notifique a soporte técnico.";
            }

            return View(filtro);
        }

        // ====================================================================
        // 2. EXPORTACIÓN A PDF
        // ====================================================================
        [HttpGet]
        public async Task<IActionResult> DescargarBoletaPdf(int id)
        {
            var boleta = await _context.BoletasCabecera
                .Include(b => b.Detalles)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (boleta == null) return NotFound("La planilla solicitada no existe o fue purgada.");

            var pdfBytes = _pdfGenerator.GenerarBoleta(boleta);
            string nombreArchivo = $"Boleta_{boleta.DNI}_{boleta.Anio}_{boleta.Mes}.pdf";
            return File(pdfBytes, "application/pdf", nombreArchivo);
        }
    }
}