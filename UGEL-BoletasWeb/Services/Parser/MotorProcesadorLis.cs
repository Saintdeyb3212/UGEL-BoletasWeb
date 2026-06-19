using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using UGEL_BoletasWeb.Models.Entities;

namespace UGEL_BoletasWeb.Services.Parser
{
    public interface IMotorProcesadorLis
    {
        Task<List<BoletaCabecera>> ProcesarArchivoAsync(IFormFile archivoLis, string usuarioQueSube);
    }

    public enum EstadoParser
    {
        BuscandoCabecera,
        LeyendoCabecera,
        LeyendoConceptos
    }

    public class MotorProcesadorLis : IMotorProcesadorLis
    {
        private static readonly Regex RxPeriodo = new Regex(@"([A-Z]+)\s*-\s*(\d{4})", RegexOptions.Compiled);
        private static readonly Regex RxApellidos = new Regex(@"Apellidos\s*:\s*(.+)", RegexOptions.Compiled);
        private static readonly Regex RxNombres = new Regex(@"Nombres\s*:\s*(.+)", RegexOptions.Compiled);
        private static readonly Regex RxDni = new Regex(@"Identidad.*?(?<!\d)(\d{8})(?!\d)", RegexOptions.Compiled);
        private static readonly Regex RxCargo = new Regex(@"Cargo\s*:\s*(.+)", RegexOptions.Compiled);
        private static readonly Regex RxTipoPensionista = new Regex(@"Tipo de Pensionista\s*:\s*(.+)", RegexOptions.Compiled);
        private static readonly Regex RxTipoPension = new Regex(@"Tipo de Pension\s*:\s*(.+)", RegexOptions.Compiled);
        private static readonly Regex RxCuenta = new Regex(@"Cta\. TeleAhorro.*:\s*(.+)", RegexOptions.Compiled);
        private static readonly Regex RxSeparador = new Regex(@"={10,}", RegexOptions.Compiled);

        // 🚀 AQUÍ ESTÁ EL EXTRACTOR DEL MONTO IMPONIBLE
        private static readonly Regex RxMontoImponible = new Regex(@"MImponible\s*:?\s*([\d,]+\.\d{2})", RegexOptions.Compiled);

        private static readonly Regex RxConcepto = new Regex(@"(?<=^|\s)([+-])([a-zA-Z0-9_.-]+)\s+(\d+\.\d{2})", RegexOptions.Compiled);

        public async Task<List<BoletaCabecera>> ProcesarArchivoAsync(IFormFile archivoLis, string usuarioQueSube)
        {
            if (archivoLis == null || archivoLis.Length == 0)
                throw new ArgumentException("El archivo LIS está vacío o es nulo.");

            var boletasProcesadas = new List<BoletaCabecera>();
            EstadoParser estadoActual = EstadoParser.BuscandoCabecera;
            string mesGlobal = "01";
            string anioGlobal = DateTime.Now.Year.ToString();

            string tempApellidos = "", tempNombres = "", tempDni = "", tempCargo = "";
            string tempTipoPensionista = "", tempTipoPension = "", tempCuenta = "";

            // 🚀 VARIABLE PARA ALMACENAR EL IMPONIBLE TEMPORALMENTE
            decimal tempImponible = 0;
            List<BoletaDetalle> tempDetalles = new List<BoletaDetalle>();

            using (var stream = archivoLis.OpenReadStream())
            using (var reader = new StreamReader(stream))
            {
                string linea;
                while ((linea = await reader.ReadLineAsync()) != null)
                {
                    linea = linea.Trim();
                    if (string.IsNullOrWhiteSpace(linea)) continue;

                    var matchPeriodo = RxPeriodo.Match(linea);
                    if (matchPeriodo.Success && (linea.Contains("CES/") || linea.Contains("ACT/")))
                    {
                        mesGlobal = ConvertirMesANumero(matchPeriodo.Groups[1].Value.Trim());
                        anioGlobal = matchPeriodo.Groups[2].Value.Trim();
                    }

                    var matchApellidos = RxApellidos.Match(linea);
                    if (matchApellidos.Success)
                    {
                        if ((estadoActual == EstadoParser.LeyendoConceptos || tempDetalles.Count > 0) && !string.IsNullOrEmpty(tempDni))
                        {
                            // 🚀 PASAMOS EL tempImponible AL MÉTODO
                            GuardarBoletaTemporal(boletasProcesadas, tempDni, tempApellidos, tempNombres, tempCargo, tempTipoPensionista, tempTipoPension, tempCuenta, mesGlobal, anioGlobal, tempImponible, tempDetalles, usuarioQueSube);
                            tempDetalles = new List<BoletaDetalle>();
                            tempDni = ""; tempCargo = ""; tempTipoPensionista = ""; tempTipoPension = ""; tempCuenta = "";
                            tempImponible = 0; // Resetear
                        }

                        tempApellidos = matchApellidos.Groups[1].Value.Trim();
                        estadoActual = EstadoParser.LeyendoCabecera;
                        continue;
                    }

                    if (RxSeparador.IsMatch(linea) && estadoActual == EstadoParser.LeyendoCabecera)
                    {
                        estadoActual = EstadoParser.LeyendoConceptos;
                        continue;
                    }

                    if (estadoActual == EstadoParser.LeyendoConceptos && (linea.StartsWith("T-REMUN") || linea.StartsWith("LIQUIDO") || linea.StartsWith("T-DSCTO")))
                    {
                        estadoActual = EstadoParser.BuscandoCabecera;
                        continue;
                    }

                    if (estadoActual == EstadoParser.LeyendoCabecera)
                    {
                        var matchNombres = RxNombres.Match(linea);
                        if (matchNombres.Success) tempNombres = matchNombres.Groups[1].Value.Trim();

                        var matchDni = RxDni.Match(linea);
                        if (matchDni.Success) tempDni = matchDni.Groups[1].Value.Trim();

                        var matchCargo = RxCargo.Match(linea);
                        if (matchCargo.Success) tempCargo = matchCargo.Groups[1].Value.Trim();

                        var matchTipoPta = RxTipoPensionista.Match(linea);
                        if (matchTipoPta.Success) tempTipoPensionista = matchTipoPta.Groups[1].Value.Trim();

                        var matchTipoPen = RxTipoPension.Match(linea);
                        if (matchTipoPen.Success) tempTipoPension = matchTipoPen.Groups[1].Value.Trim();

                        var matchCuenta = RxCuenta.Match(linea);
                        if (matchCuenta.Success) tempCuenta = matchCuenta.Groups[1].Value.Trim();

                        // 🚀 EXTRACCIÓN DEL MONTO IMPONIBLE
                        var matchImponible = RxMontoImponible.Match(linea);
                        if (matchImponible.Success)
                        {
                            string valorLimpio = matchImponible.Groups[1].Value.Replace(",", "");
                            tempImponible = decimal.Parse(valorLimpio, CultureInfo.InvariantCulture);
                        }
                    }
                    else if (estadoActual == EstadoParser.LeyendoConceptos)
                    {
                        var matchesConceptos = RxConcepto.Matches(linea);
                        foreach (Match m in matchesConceptos)
                        {
                            string signo = m.Groups[1].Value;
                            string codigo = m.Groups[2].Value.ToUpper();
                            decimal monto = decimal.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);

                            string tipo = signo == "+" ? "I" : "D";
                            tempDetalles.Add(new BoletaDetalle(codigo, codigo, tipo, monto, usuarioQueSube));
                        }
                    }
                }

                if (!string.IsNullOrEmpty(tempDni) && tempDetalles.Count > 0)
                {
                    // 🚀 PASAMOS EL tempImponible A LA ÚLTIMA BOLETA
                    GuardarBoletaTemporal(boletasProcesadas, tempDni, tempApellidos, tempNombres, tempCargo, tempTipoPensionista, tempTipoPension, tempCuenta, mesGlobal, anioGlobal, tempImponible, tempDetalles, usuarioQueSube);
                }
            }

            return boletasProcesadas;
        }

        // 🚀 ACTUALIZAMOS LA FIRMA DEL MÉTODO PARA RECIBIR 'decimal imponible'
        private void GuardarBoletaTemporal(List<BoletaCabecera> lista, string dni, string apellidos, string nombres, string cargo, string tipoPensionista, string tipoPension, string cuenta, string mes, string anio, decimal imponible, List<BoletaDetalle> detalles, string usuario)
        {
            decimal totalIngresos = 0;
            decimal totalDescuentos = 0;

            foreach (var d in detalles)
            {
                if (d.TipoConcepto == "I") totalIngresos += d.Monto;
                if (d.TipoConcepto == "D") totalDescuentos += d.Monto;
            }

            decimal montoLiquido = totalIngresos - totalDescuentos;

            // 🚀 AQUÍ RESOLVEMOS EL ERROR ENVIANDO EL DATO AL CONSTRUCTOR
            var nuevaBoleta = new BoletaCabecera(
                dni: dni,
                apellidos: apellidos,
                nombres: nombres,
                cargo: cargo,
                tipoPensionista: tipoPensionista,
                tipoPension: tipoPension,
                cuentaBancaria: cuenta,
                mes: mes,
                anio: anio,
                montoImponible: imponible, // <-- Este es el parámetro que faltaba
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