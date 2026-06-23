using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;
using UGEL_BoletasWeb.Data.Context;
using UGEL_BoletasWeb.Models.ViewModels;
using UGEL_BoletasWeb.Services.PdfExport;

namespace UGEL_BoletasWeb.Controllers
{
    // 🚀 REGLA DE DOMINIO EN INTERFAZ: Ambos roles tienen autorización estricta para consultar boletas
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
        // 1. MÉTODO GET (Renderizado seguro y limpio contra F5)
        // ====================================================================
        [HttpGet]
        public IActionResult Consultar()
        {
            var modelo = new FiltroConsultaViewModel();

            // Patrón PRG: Recuperamos los resultados desde TempData si existen
            if (TempData["ResultadosBusqueda"] != null)
            {
                try
                {
                    modelo = JsonConvert.DeserializeObject<FiltroConsultaViewModel>(TempData["ResultadosBusqueda"]!.ToString()!);
                }
                catch (System.Exception)
                {
                    modelo = new FiltroConsultaViewModel();
                }

                if (modelo!.Resultados == null || !modelo.Resultados.Any())
                {
                    ViewBag.MensajeAviso = $"No se encontraron boletas registradas para el DNI {modelo.DNI} en el periodo solicitado.";
                }
            }

            return View(modelo);
        }

        // ====================================================================
        // 2. MÉTODO POST (Procesamiento atómico, redirección obligatoria)
        // ====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcesarConsulta(FiltroConsultaViewModel filtro) // 🚀 Asegúrate de que tenga 1 sola 's'
        {
            // Evitamos excepciones de referencia nula (NullReferenceException)
            if (filtro == null || string.IsNullOrEmpty(filtro.DNI) || string.IsNullOrEmpty(filtro.Anio))
            {
                return RedirectToAction(nameof(Consultar));
            }

            try
            {
                // Query optimizado con Eager Loading para los conceptos financieros
                var query = _context.BoletasCabecera
                    .Include(b => b.Detalles)
                    .Where(b => b.DNI == filtro.DNI.Trim() && b.Anio == filtro.Anio.Trim());

                if (!string.IsNullOrEmpty(filtro.Mes))
                {
                    query = query.Where(b => b.Mes == filtro.Mes);
                }

                var boletasEncontradas = await query
                    .OrderByDescending(b => b.Mes)
                    .ToListAsync();

                filtro.Resultados = boletasEncontradas;

                // Serializamos de forma segura previniendo ciclos infinitos en Entity Framework
                TempData["ResultadosBusqueda"] = JsonConvert.SerializeObject(filtro, new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
            }
            catch (System.Exception)
            {
                // Si la base de datos falla, enviamos un modelo limpio para que el summary lo maneje en la UI
                TempData["ResultadosBusqueda"] = JsonConvert.SerializeObject(new FiltroConsultaViewModel { DNI = filtro.DNI, Anio = filtro.Anio });
            }

            // Saltamos al GET para limpiar el pipeline de la petición HTTP
            return RedirectToAction(nameof(Consultar));
        }

        // ====================================================================
        // 3. EXPORTACIÓN EN TIEMPO REAL A HOJA A4
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