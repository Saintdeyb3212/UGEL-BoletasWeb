using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UGEL_BoletasWeb.Models.Entities;

namespace UGEL_Boletas.Services.Parser
{
    public interface IMotorProcesadorLis
    {
        Task<List<BoletaCabecera>> ProcesarArchivoAsync(IFormFile archivoLis, string usuarioQueSube);
    }

    // Enum para la Máquina de Estados
    public enum EstadoParser
    {
        BuscandoCabecera,
        LeyendoCabecera,
        LeyendoConceptos
    }

    public class MotorProcesadorLis : IMotorProcesadorLis
    {
        // EXPRESIONES REGULARES COMPILADAS (Rendimiento Extremo)
        // Busca Periodo: ABRIL - 2026
        private static readonly Regex RxPeriodo = new Regex(@"([A-Z]+)\s*-\s*(\d{4})", RegexOptions.Compiled);

        // Busca Inicio de Boleta
        private static readonly Regex RxApellidos = new Regex(@"Apellidos\s*:\s*(.+)", RegexOptions.Compiled);
        private static readonly Regex RxNombres = new Regex(@"Nombres\s*:\s*(.+)", RegexOptions.Compiled);

        // Atrapa exactamente 8 dígitos del DNI, saltándose textos como (Lib.Electoral o D.N.)
        private static readonly Regex RxDni = new Regex(@"Identidad.*?(?<!\d)(\d{8})(?!\d)", RegexOptions.Compiled);

        // Busca Ley / Pensión
        private static readonly Regex RxLey = new Regex(@"Tipo de Pension\s*:\s*(.+)", RegexOptions.Compiled);

        // El interruptor de estados
        private static readonly Regex RxSeparador = new Regex(@"={10,}", RegexOptions.Compiled);

        // El cazador de dinero: (+basica         50.00)
        // Grupo 1: Signo, Grupo 2: Código, Grupo 3: Monto exacto
        private static readonly Regex RxConcepto = new Regex(@"([+-])([a-zA-Z0-9_.-]+)\s+(\d+\.\d{2})", RegexOptions.Compiled);

        public async Task<List<BoletaCabecera>> ProcesarArchivoAsync(IFormFile archivoLis, string usuarioQueSube)
        {
            if (archivoLis == null || archivoLis.Length == 0)
                throw new ArgumentException("El archivo LIS está vacío o es nulo.");

            var boletasProcesadas = new List<BoletaCabecera>();

            // Contexto de Estado
            EstadoParser estadoActual = EstadoParser.BuscandoCabecera;
            string mesGlobal = "01";
            string anioGlobal = DateTime.Now.Year.ToString();

            // Variables de construcción en memoria
            string tempApellidos = ""; string tempNombres = "";
            string tempDni = ""; string tempLey = "";
            List<BoletaDetalle> tempDetalles = new List<BoletaDetalle>();

            // StreamReader: Lee línea por línea sin saturar la RAM
            using (var stream = archivoLis.OpenReadStream())
            using (var reader = new StreamReader(stream))
            {
                string linea;
                while ((linea = await reader.ReadLineAsync()) != null)
                {
                    linea = linea.Trim();
                    if (string.IsNullOrWhiteSpace(linea)) continue;

                    // 1. Extraer Periodo de las páginas
                    var matchPeriodo = RxPeriodo.Match(linea);
                    if (matchPeriodo.Success && (linea.Contains("CES/") || linea.Contains("ACT/")))
                    {
                        mesGlobal = ConvertirMesANumero(matchPeriodo.Groups[1].Value.Trim());
                        anioGlobal = matchPeriodo.Groups[2].Value.Trim();
                    }

                    // 2. Control de Estado: Fin e Inicio de nueva Boleta
                    var matchApellidos = RxApellidos.Match(linea);
                    if (matchApellidos.Success)
                    {
                        if (estadoActual == EstadoParser.LeyendoConceptos && !string.IsNullOrEmpty(tempDni))
                        {
                            GuardarBoletaTemporal(boletasProcesadas, tempDni, tempNombres, tempApellidos, mesGlobal, anioGlobal, tempLey, tempDetalles, usuarioQueSube);
                            tempDetalles = new List<BoletaDetalle>();
                            tempDni = ""; tempLey = "";
                        }

                        tempApellidos = matchApellidos.Groups[1].Value.Trim();
                        estadoActual = EstadoParser.LeyendoCabecera;
                        continue;
                    }

                    // 3. Control de Estado: Pasar de Cabecera a Conceptos
                    if (RxSeparador.IsMatch(linea) && estadoActual == EstadoParser.LeyendoCabecera)
                    {
                        estadoActual = EstadoParser.LeyendoConceptos;
                        continue;
                    }

                    // 4. Extracción según el estado de la máquina
                    if (estadoActual == EstadoParser.LeyendoCabecera)
                    {
                        var matchNombres = RxNombres.Match(linea);
                        if (matchNombres.Success) tempNombres = matchNombres.Groups[1].Value.Trim();

                        var matchDni = RxDni.Match(linea);
                        if (matchDni.Success) tempDni = matchDni.Groups[1].Value.Trim();

                        var matchLey = RxLey.Match(linea);
                        if (matchLey.Success) tempLey = matchLey.Groups[1].Value.Trim();
                    }
                    else if (estadoActual == EstadoParser.LeyendoConceptos)
                    {
                        // .Matches() captura n resultados en la misma fila
                        var matchesConceptos = RxConcepto.Matches(linea);
                        foreach (Match m in matchesConceptos)
                        {
                            string signo = m.Groups[1].Value;
                            string codigo = m.Groups[2].Value.ToUpper();
                            decimal monto = decimal.Parse(m.Groups[3].Value);

                            string tipo = signo == "+" ? "I" : "D";
                            tempDetalles.Add(new BoletaDetalle(codigo, codigo, tipo, monto, usuarioQueSube));
                        }
                    }
                }

                // Guardar la última boleta al llegar al final del .lis
                if (estadoActual == EstadoParser.LeyendoConceptos && !string.IsNullOrEmpty(tempDni))
                {
                    GuardarBoletaTemporal(boletasProcesadas, tempDni, tempNombres, tempApellidos, mesGlobal, anioGlobal, tempLey, tempDetalles, usuarioQueSube);
                }
            }

            return boletasProcesadas;
        }

        private void GuardarBoletaTemporal(List<BoletaCabecera> lista, string dni, string nombres, string apellidos, string mes, string anio, string ley, List<BoletaDetalle> detalles, string usuario)
        {
            // Fail-Safe: Calculamos los montos nosotros mismos. 
            // Nunca confíes en los totales impresos de un archivo de texto viejo.
            decimal totalIngresos = 0;
            decimal totalDescuentos = 0;

            foreach (var d in detalles)
            {
                if (d.TipoConcepto == "I") totalIngresos += d.Monto;
                if (d.TipoConcepto == "D") totalDescuentos += d.Monto;
            }

            decimal montoLiquido = totalIngresos - totalDescuentos;

            var nuevaBoleta = new BoletaCabecera(
                dni: dni,
                nombresApellidos: $"{apellidos} {nombres}",
                codigoModular: "00000000",
                mes: mes,
                anio: anio,
                totalIngresos: totalIngresos,
                totalDescuentos: totalDescuentos,
                montoLiquido: montoLiquido,
                usuarioCreacion: usuario
            );

            foreach (var d in detalles) nuevaBoleta.AgregarDetalle(d);

            lista.Add(nuevaBoleta);
        }

        private string ConvertirMesANumero(string mesTexto)
        {
            return mesTexto.ToUpper() switch
            {
                "ENERO" => "01",
                "FEBRERO" => "02",
                "MARZO" => "03",
                "ABRIL" => "04",
                "MAYO" => "05",
                "JUNIO" => "06",
                "JULIO" => "07",
                "AGOSTO" => "08",
                "SETIEMBRE" => "09",
                "OCTUBRE" => "10",
                "NOVIEMBRE" => "11",
                "DICIEMBRE" => "12",
                _ => "00"
            };
        }
    }
}