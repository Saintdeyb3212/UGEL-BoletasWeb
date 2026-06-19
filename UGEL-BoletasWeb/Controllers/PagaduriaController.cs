using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using UGEL_BoletasWeb.Data.Context;
using UGEL_BoletasWeb.Models.ViewModels;

namespace UGEL_BoletasWeb.Controllers
{
    public class PagaduriaController : Controller
    {
        private readonly UgelDbContext _context;

        // Inyectamos la base de datos
        public PagaduriaController(UgelDbContext context)
        {
            _context = context;
        }

        // 1. Mostrar la pantalla por primera vez (Vacía)
        [HttpGet]
        public IActionResult Consultar()
        {
            return View(new FiltroConsultaViewModel());
        }

        // 2. Ejecutar la búsqueda cuando la oficinista presiona "Buscar"
        [HttpPost]
        public async Task<IActionResult> Consultar(FiltroConsultaViewModel filtro)
        {
            // Validamos que no envíen campos vacíos
            if (string.IsNullOrEmpty(filtro.DNI) || string.IsNullOrEmpty(filtro.Anio))
            {
                ModelState.AddModelError("", "Por favor ingrese el DNI y el Año para buscar.");
                return View(filtro);
            }

            // Búsqueda eficiente aprovechando los Índices (IX_BoletaCabecera_DNI) que creamos
            var boletasEncontradas = await _context.BoletasCabecera
                .Include(b => b.Detalles) // Eager Loading: Traemos los montos asociados
                .Where(b => b.DNI == filtro.DNI && b.Anio == filtro.Anio)
                .OrderByDescending(b => b.Mes) // Ordenamos para que el mes más reciente salga primero
                .ToListAsync();

            filtro.Resultados = boletasEncontradas;

            if (!boletasEncontradas.Any())
            {
                ViewBag.MensajeAviso = $"No se encontraron boletas para el DNI {filtro.DNI} en el año {filtro.Anio}.";
            }

            return View(filtro);
        }
    }
}