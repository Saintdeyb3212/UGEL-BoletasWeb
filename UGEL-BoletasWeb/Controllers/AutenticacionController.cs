using Microsoft.AspNetCore.Mvc;
using UGEL_BoletasWeb.Models.ViewModels;

namespace UGEL_BoletasWeb.Controllers
{
    public class AutenticacionController : Controller
    {
        [HttpGet]
        public IActionResult IniciarSesion()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult IniciarSesion(LoginViewModel modelo)
        {
            if (!ModelState.IsValid) return View(modelo);

            // TODO: Aquí implementaremos la conexión con la tabla UsuarioSistema
            // Por ahora, simularemos que el login es exitoso y lo mandaremos a Pagaduría

            return RedirectToAction("Consultar", "Pagaduria");
        }

        public IActionResult CerrarSesion()
        {
            // TODO: Lógica para destruir las cookies de seguridad
            return RedirectToAction(nameof(IniciarSesion));
        }
    }
}