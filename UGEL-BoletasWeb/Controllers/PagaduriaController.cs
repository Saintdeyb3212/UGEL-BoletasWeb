using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using UGEL_BoletasWeb.Data.Context;
using UGEL_BoletasWeb.Models.ViewModels;
using UGEL_BoletasWeb.Services.PdfExport; // 🚀 Aseguramos el espacio de nombres

namespace UGEL_BoletasWeb.Controllers
{
    public class PagaduriaController : Controller
    {
        private readonly UgelDbContext _context;
        private readonly IGeneradorBoletaPdf _pdfGenerator; // 🚀 FIX: Campo privado declarado

        // 🚀 FIX: Inyección de dependencias completa en el constructor
        public PagaduriaController(UgelDbContext context, IGeneradorBoletaPdf pdfGenerator)
        {
            _context = context;
            _pdfGenerator = pdfGenerator;
        }

        [HttpGet]
        public IActionResult Consultar()
        {
            return View(new FiltroConsultaViewModel());
        }

        // 🚀 ENDPOINT DE EXPORTACIÓN RECONOCIDO POR LA VISTA
        [HttpGet]
        public async Task<IActionResult> DescargarBoletaPdf(int id)
        {
            var boleta = await _context.BoletasCabecera
                .Include(b => b.Detalles)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (boleta == null)
            {
                return NotFound("Error: La planilla solicitada no existe o fue eliminada del sistema.");
            }

            var pdfBytes = _pdfGenerator.GenerarBoleta(boleta);

            string nombreArchivo = $"Boleta_{boleta.DNI}_{boleta.Anio}_{boleta.Mes}.pdf";
            return File(pdfBytes, "application/pdf", nombreArchivo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Consultar(FiltroConsultaViewModel filtro)
        {
            if (string.IsNullOrEmpty(filtro.DNI) || string.IsNullOrEmpty(filtro.Anio))
            {
                ModelState.AddModelError("", "El número de DNI y el Año Fiscal son obligatorios para procesar la consulta.");
                return View(filtro);
            }

            var query = _context.BoletasCabecera
                .Include(b => b.Detalles)
                .Where(b => b.DNI == filtro.DNI && b.Anio == filtro.Anio);

            if (!string.IsNullOrEmpty(filtro.Mes))
            {
                query = query.Where(b => b.Mes == filtro.Mes);
            }

            var boletasEncontradas = await query
                .OrderByDescending(b => b.Mes)
                .ToListAsync();

            filtro.Resultados = boletasEncontradas;

            if (!boletasEncontradas.Any())
            {
                ViewBag.MensajeAviso = $"No se encontraron boletas registradas para el DNI {filtro.DNI} en el periodo solicitado.";
            }

            return View(filtro);
        }
    }
}