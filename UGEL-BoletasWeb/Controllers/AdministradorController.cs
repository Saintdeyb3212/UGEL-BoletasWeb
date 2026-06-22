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

                var boletasExtraidas = await _motorParser.ProcesarArchivoAsync(archivoLis, "AdminSistema");

                if (boletasExtraidas.Count == 0)
                {
                    TempData["Error"] = "El archivo se leyó, pero no se encontró ninguna boleta válida.";
                    return RedirectToAction(nameof(SubirArchivoLis));
                }

                // 🚀 FIX: VALIDACIÓN COMPUESTA (DNI + TIPO PENSIONISTA)
                var mesPlanilla = boletasExtraidas[0].Mes;
                var anioPlanilla = boletasExtraidas[0].Anio;

                // Creamos llaves únicas en memoria (Ej: "20402663-SOBREVIVIENTE")
                var llavesArchivo = boletasExtraidas.Select(b => $"{b.DNI}-{b.TipoPensionista}").Distinct().ToList();

                // Traemos solo las llaves de ese mes/año de la BD
                var llavesBD = await _context.BoletasCabecera
                    .Where(b => b.Mes == mesPlanilla && b.Anio == anioPlanilla)
                    .Select(b => b.DNI + "-" + b.TipoPensionista)
                    .ToListAsync();

                // Comprobamos si hay choque de llaves
                bool hayDuplicado = llavesArchivo.Any(llave => llavesBD.Contains(llave));

                if (hayDuplicado)
                {
                    TempData["Error"] = $"ALERTA DE SEGURIDAD: El archivo contiene boletas (del mismo tipo y DNI) que ya fueron registradas para el periodo {mesPlanilla}-{anioPlanilla}.";
                    return RedirectToAction(nameof(SubirArchivoLis));
                }
                // --------------------------------------------------

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