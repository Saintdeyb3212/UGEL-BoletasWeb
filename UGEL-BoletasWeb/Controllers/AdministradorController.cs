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

        // ======================================================================
        // MOTOR DE RESPALDO ATÓMICO (SQL SERVER .BAK)
        // ======================================================================
        // ======================================================================
        // MOTOR DE RESPALDO ATÓMICO (SQL SERVER .BAK) - FIX PERMISOS
        // ======================================================================
        [HttpGet]
        public async Task<IActionResult> DescargarBackupBaseDatos()
        {
            string connectionString = _context.Database.GetConnectionString() ?? "";
            var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
            string nombreBaseDatos = builder.InitialCatalog;

            if (string.IsNullOrEmpty(nombreBaseDatos))
            {
                return BadRequest("No se pudo identificar el catálogo inicial de la base de datos.");
            }

            // 🚀 FIX DE INFRAESTRUCTURA: Usamos la carpeta Pública de Windows. 
            // SQL Server (como servicio) SIEMPRE tiene permiso de escribir aquí.
            string directorioPublico = @"C:\Users\Public\Documents\UGEL_Backups";

            // Creamos la carpeta si no existe
            if (!System.IO.Directory.Exists(directorioPublico))
            {
                System.IO.Directory.CreateDirectory(directorioPublico);
            }

            string nombreArchivoBak = $"{nombreBaseDatos}_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
            string rutaFisicaCompleta = System.IO.Path.Combine(directorioPublico, nombreArchivoBak);

            try
            {
                // Ordenamos a SQL Server crear el backup en la zona pública
                FormattableString queryBackup = $"BACKUP DATABASE {nombreBaseDatos} TO DISK = {rutaFisicaCompleta} WITH FORMAT, MEDIANAME = 'UGEL_Backup', NAME = 'Full Backup of UGEL Boletas';";

                await _context.Database.ExecuteSqlInterpolatedAsync(queryBackup);

                // Leemos el archivo que SQL Server nos acaba de dejar
                byte[] bytesArchivo = await System.IO.File.ReadAllBytesAsync(rutaFisicaCompleta);

                // Borramos la evidencia para no ocupar disco duro
                if (System.IO.File.Exists(rutaFisicaCompleta))
                {
                    System.IO.File.Delete(rutaFisicaCompleta);
                }

                return File(bytesArchivo, "application/octet-stream", nombreArchivoBak);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error de permisos o motor SQL: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Ajustes()
        {
            var usuarios = await _context.UsuariosSistema.OrderBy(u => u.Username).ToListAsync();
            ViewBag.Usuarios = usuarios;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(104857600)] // Permite recibir archivos LIS de hasta 100 MB sin colapsar
        public async Task<IActionResult> ProcesarArchivoLisSingle(IFormFile archivoLis)
        {
            if (archivoLis == null || archivoLis.Length == 0)
                return Json(new { success = false, message = "El archivo se encuentra vacío o corrupto." });

            // 🚀 FIX MEMORY LEAK: Copiamos el archivo HTTP a la memoria RAM de forma segura y liberamos la red
            using var memoryStream = new System.IO.MemoryStream();
            await archivoLis.CopyToAsync(memoryStream);
            memoryStream.Position = 0; // Regresamos el cursor al inicio para leer

            // Creamos un FormFile "Falso" de memoria para dárselo a tu parser de forma segura
            var archivoSeguro = new FormFile(memoryStream, 0, memoryStream.Length, "archivoLis", archivoLis.FileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = archivoLis.ContentType
            };

            using var transaccion = await _context.Database.BeginTransactionAsync();
            try
            {
                var boletasExtraidas = await _motorParser.ProcesarArchivoAsync(archivoSeguro, User.Identity?.Name ?? "Sistema");

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
                // 🚀 BUG 3 RESUELTO: Escudo anti-suicidio de sistema. No puede borrarse a sí mismo.
                if (user.Username.Equals(User.Identity?.Name, StringComparison.OrdinalIgnoreCase))
                {
                    TempData["ErrorUser"] = "ALERTA CRÍTICA: No puede eliminar la cuenta con la que está actualmente conectado.";
                    return RedirectToAction(nameof(Ajustes));
                }

                _context.UsuariosSistema.Remove(user);
                await _context.SaveChangesAsync();
                TempData["ExitoUser"] = "Usuario eliminado del sistema.";
            }
            return RedirectToAction(nameof(Ajustes));
        }
    }
}