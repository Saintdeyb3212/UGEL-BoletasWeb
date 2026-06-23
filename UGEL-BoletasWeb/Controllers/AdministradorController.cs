using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UGEL_BoletasWeb.Data.Context;
using UGEL_BoletasWeb.Models.Entities;
using UGEL_BoletasWeb.Services.Parser;

namespace UGEL_BoletasWeb.Controllers
{
    [Authorize(Roles = "Administrador")]
    [EnableRateLimiting("FiltroTraficoIP")] 
    public class AdministradorController : Controller
    {
        private readonly UgelDbContext _context;
        private readonly IMotorProcesadorLis _motorParser;

        public AdministradorController(UgelDbContext context, IMotorProcesadorLis motorParser)
        {
            _context = context;
            _motorParser = motorParser;
        }

        [HttpGet]
        public async Task<IActionResult> Ajustes()
        {
            var usuarios = await _context.UsuariosSistema.OrderBy(u => u.Username).ToListAsync();
            ViewBag.Usuarios = usuarios;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ProcesarArchivoLisSingle(IFormFile archivoLis)
        {
            if (archivoLis == null || archivoLis.Length == 0)
                return Json(new { success = false, message = "El archivo se encuentra vacío o corrupto." });

            using var transaccion = await _context.Database.BeginTransactionAsync();
            try
            {
                var boletasExtraidas = await _motorParser.ProcesarArchivoAsync(archivoLis, "AdminSistema");

                if (!boletasExtraidas.Any())
                {
                    return Json(new { success = false, message = "No se encontraron estructuras de boletas válidas." });
                }

                var mesPlanilla = boletasExtraidas[0].Mes;
                var anioPlanilla = boletasExtraidas[0].Anio;

                var llavesArchivo = boletasExtraidas.Select(b => $"{b.DNI}-{b.TipoPensionista}").Distinct().ToList();
                var llavesBD = await _context.BoletasCabecera
                    .Where(b => b.Mes == mesPlanilla && b.Anio == anioPlanilla)
                    .Select(b => b.DNI + "-" + b.TipoPensionista)
                    .ToListAsync();

                bool hayDuplicado = llavesArchivo.Any(llave => llavesBD.Contains(llave));
                if (hayDuplicado)
                {
                    return Json(new { success = false, message = $"El periodo {mesPlanilla}-{anioPlanilla} ya cuenta con registros activos." });
                }

                await _context.BoletasCabecera.AddRangeAsync(boletasExtraidas);
                await _context.SaveChangesAsync();
                await transaccion.CommitAsync();

                return Json(new { success = true, message = $"¡Éxito! {boletasExtraidas.Count} boletas integradas ({mesPlanilla}-{anioPlanilla})." });
            }
            catch (Exception ex)
            {
                await transaccion.RollbackAsync();
                return Json(new { success = false, message = $"Error de procesamiento: {ex.Message}" });
            }
        }

        // ======================================================================
        // CRUD ACCIONES: REFACTORIZACIÓN CON ENFOQUE RICH DOMAIN
        // ======================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearUsuario(string username, string password, string rol)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(rol))
            {
                TempData["ErrorUser"] = "Todos los campos, incluyendo la contraseña, son obligatorios.";
                return RedirectToAction(nameof(Ajustes));
            }

            try
            {
                bool existe = await _context.UsuariosSistema.AnyAsync(u => u.Username == username.Trim());
                if (existe)
                {
                    TempData["ErrorUser"] = "El nombre de usuario ya está registrado en el sistema.";
                    return RedirectToAction(nameof(Ajustes));
                }

                // 🚀 IMPLEMENTACIÓN DE HASH SEGURO (Nativo de .NET Core)
                var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<UsuarioSistema>();
                // Le pasamos null ya que en este punto no tenemos la instancia creada aún
                string secureHash = hasher.HashPassword(null!, password.Trim());

                // Creamos la instancia con la contraseña protegida herméticamente
                var nuevoUsuario = new UsuarioSistema(username.Trim(), secureHash, rol.Trim(), "AdminSistema");

                _context.UsuariosSistema.Add(nuevoUsuario);
                await _context.SaveChangesAsync();

                TempData["ExitoUser"] = "Usuario registrado con credenciales cifradas correctamente.";
            }
            catch (Exception ex)
            {
                TempData["ErrorUser"] = $"Error de dominio: {ex.Message}";
            }

            return RedirectToAction(nameof(Ajustes));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarUsuario(int id)
        {
            var user = await _context.UsuariosSistema.FindAsync(id);
            if (user != null)
            {
                _context.UsuariosSistema.Remove(user);
                await _context.SaveChangesAsync();
                TempData["ExitoUser"] = "Usuario eliminado del sistema.";
            }
            return RedirectToAction(nameof(Ajustes));
        }
    }
}