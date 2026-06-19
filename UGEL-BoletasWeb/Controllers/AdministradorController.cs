using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using UGEL_BoletasWeb.Data.Context;
using UGEL_BoletasWeb.Services.Parser;

namespace UGEL_BoletasWeb.Controllers
{
    public class AdministradorController : Controller
    {
        private readonly IMotorProcesadorLis _motorParser;
        private readonly UgelDbContext _context;

        public AdministradorController(IMotorProcesadorLis motorParser, UgelDbContext context)
        {
            _motorParser = motorParser;
            _context = context;
        }

        [HttpGet]
        public IActionResult SubirArchivoLis()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcesarArchivoLis(IFormFile archivoLis)
        {
            try
            {
                if (archivoLis == null || archivoLis.Length == 0)
                {
                    TempData["Error"] = "Por favor, seleccione un archivo .lis válido.";
                    return RedirectToAction(nameof(SubirArchivoLis));
                }

                // 1. El Cerebro lee el archivo en memoria RAM
                var boletasExtraidas = await _motorParser.ProcesarArchivoAsync(archivoLis, "AdminSistema");

                if (boletasExtraidas.Count == 0)
                {
                    TempData["Error"] = "El archivo se leyó, pero no se encontró ninguna boleta válida.";
                    return RedirectToAction(nameof(SubirArchivoLis));
                }

                // --- 🚀 VALIDACIÓN QUIRÚRGICA ANTI-DUPLICADOS ---
                var mesPlanilla = boletasExtraidas[0].Mes;
                var anioPlanilla = boletasExtraidas[0].Anio;

                // Extraemos todos los DNIs que vienen en este archivo que intentan subir
                var dnisEnArchivo = boletasExtraidas.Select(b => b.DNI).Distinct().ToList();

                // Le preguntamos a SQL Server si ya tiene ALGUNO de esos DNIs específicos en ese Mes y Año
                var dnisDuplicados = await _context.BoletasCabecera
                    .Where(b => b.Mes == mesPlanilla && b.Anio == anioPlanilla && dnisEnArchivo.Contains(b.DNI))
                    .Select(b => b.DNI)
                    .ToListAsync();

                if (dnisDuplicados.Any())
                {
                    // ¡Freno de emergencia! Detectó que al menos un DNI de ese archivo ya se había subido.
                    TempData["Error"] = $"ALERTA DE SEGURIDAD: El archivo que intenta subir contiene boletas que ya fueron registradas para el periodo {mesPlanilla}-{anioPlanilla}. No se procesó ningún dato para evitar duplicidad financiera.";
                    return RedirectToAction(nameof(SubirArchivoLis));
                }
                // --------------------------------------------------

                // 2. Si pasó el escáner de seguridad, se guarda todo masivamente
                await _context.BoletasCabecera.AddRangeAsync(boletasExtraidas);
                await _context.SaveChangesAsync();

                TempData["Exito"] = $"¡Excelente! Se procesaron y registraron {boletasExtraidas.Count} boletas del periodo {mesPlanilla}-{anioPlanilla}.";
                return RedirectToAction(nameof(SubirArchivoLis));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error crítico del sistema: {ex.Message}";
                return RedirectToAction(nameof(SubirArchivoLis));
            }
        }
    }
}