using System.Collections.Generic;
using UGEL_BoletasWeb.Models.Entities;

namespace UGEL_BoletasWeb.Models.ViewModels
{
    public class FiltroConsultaViewModel
    {
        public string? DNI { get; set; }

        // Heurística de UX: Inicializamos por defecto en el año actual de trabajo
        public string? Anio { get; set; } = "2026";

        // Nuevo campo para búsquedas específicas por mes
        public string? Mes { get; set; }

        public List<BoletaCabecera> Resultados { get; set; } = new List<BoletaCabecera>();
    }
}