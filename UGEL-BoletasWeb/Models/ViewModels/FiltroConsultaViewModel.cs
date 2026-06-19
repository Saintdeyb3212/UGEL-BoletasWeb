using System.Collections.Generic;
using UGEL_BoletasWeb.Models.Entities;

namespace UGEL_BoletasWeb.Models.ViewModels
{
    public class FiltroConsultaViewModel
    {
        // Lo que el usuario escribe en la pantalla
        public string? DNI { get; set; }
        public string? Anio { get; set; }

        // Lo que el sistema devuelve (La lista de boletas encontradas)
        public List<BoletaCabecera> Resultados { get; set; } = new List<BoletaCabecera>();
    }
}