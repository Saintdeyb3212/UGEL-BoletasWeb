using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting; // 🚀 Requerido para el blindaje anti-bucle
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using UGEL_BoletasWeb.Data.Context;
using UGEL_BoletasWeb.Models.Entities;
using UGEL_BoletasWeb.Models.ViewModels;

namespace UGEL_BoletasWeb.Controllers
{
    [EnableRateLimiting("FiltroTraficoIP")] // 🚀 Blindamos todo el controlador contra ataques DoS/Fuerza Bruta
    public class AutenticacionController : Controller
    {
        private readonly UgelDbContext _context;

        public AutenticacionController(UgelDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        // 🚀 ESCUDO 1 (ANTI-CACHÉ): Bloquea que el navegador guarde la página. Soluciona el error del botón "Atrás".
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> IniciarSesion()
        {
            // 🚀 ESCUDO 2 (RE-SALTO): Si hay una cookie residual, la destruye y hace una redirección 
            // limpia hacia sí mismo para generar un Token Antifalsificación nuevo y anónimo.
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("IniciarSesion");
            }

            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IniciarSesion(LoginViewModel model)
        {
            // 1. Escudo contra nulos desde la red (evita NullReferenceException)
            if (model == null || string.IsNullOrWhiteSpace(model.Username) || string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError(string.Empty, "Usuario y contraseña son obligatorios.");
                return View(model ?? new LoginViewModel());
            }

            if (!ModelState.IsValid) return View(model);

            try
            {
                // Ahora sí es 100% seguro aplicar Trim()
                var username = model.Username.Trim();
                var password = model.Password.Trim();

                // 2. Buscamos al usuario
                var usuario = await _context.UsuariosSistema
                    .FirstOrDefaultAsync(u => u.Username == username && u.EstadoActivo);

                // 3. Estándar de Seguridad: Jamás revelar qué falló exactamente
                if (usuario == null)
                {
                    ModelState.AddModelError(string.Empty, "Usuario y/o contraseña incorrectas.");
                    return View(model);
                }

                // 4. Evitar colapso si la BD tiene una contraseña nula o corrupta
                if (string.IsNullOrEmpty(usuario.PasswordHash))
                {
                    ModelState.AddModelError(string.Empty, "Cuenta corrupta. Solicite regeneración de credenciales al administrador.");
                    return View(model);
                }

                // 5. Verificación Criptográfica
                var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<UsuarioSistema>();
                var resultadoVerificacion = hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, password);

                if (resultadoVerificacion == Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed)
                {
                    ModelState.AddModelError(string.Empty, "Usuario y/o contraseña incorrectas.");
                    return View(model);
                }

                // 6. Generación de Cookies
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, usuario.Username),
                    new Claim(ClaimTypes.Role, usuario.Rol)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity));

                // 7. Enrutamiento RBAC
                if (usuario.Rol == "Administrador")
                    return RedirectToAction("Ajustes", "Administrador");
                else
                    return RedirectToAction("Consultar", "Pagaduria");
            }
            catch (Exception)
            {
                // 🚀 RED DE SEGURIDAD ABSOLUTA: 
                // Cualquier falla de formato, base de datos caída o excepción no controlada
                // cae aquí, mandando el error a la interfaz SIN tumbar el servidor.
                ModelState.AddModelError(string.Empty, "Alerta: Error interno de seguridad al validar la cuenta.");
                return View(model);
            }
        }

        [HttpGet]
        // 🚀 ESCUDO 3: El logout también prohíbe el caché.
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> CerrarSesion()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("IniciarSesion");
        }
    }
}