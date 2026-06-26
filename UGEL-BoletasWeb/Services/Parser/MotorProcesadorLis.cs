using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using UGEL_BoletasWeb.Models.Entities;
using System.Text;

namespace UGEL_BoletasWeb.Services.Parser
{
    public interface IMotorProcesadorLis
    {
        Task<List<BoletaCabecera>> ProcesarArchivoAsync(IFormFile archivoLis, string usuarioQueSube);
    }

    public enum EstadoParser { BuscandoCabecera, LeyendoCabecera, LeyendoConceptos }

    public class MotorProcesadorLis : IMotorProcesadorLis
    {
        private static readonly Regex RxPeriodo = new Regex(@"([A-Z]+)\s*-\s*(\d{4})", RegexOptions.Compiled);
        private static readonly Regex RxApellidos = new Regex(@"Apellidos\s*:\s*(.+)", RegexOptions.Compiled);
        private static readonly Regex RxNombres = new Regex(@"Nombres\s*:\s*(.+)", RegexOptions.Compiled);
        private static readonly Regex RxFechaNac = new Regex(@"Fecha de Nacimiento\s*:\s*(.+)", RegexOptions.Compiled);

        // 🛡️ BLINDAJE 1: Ampliado de 7 a 15 dígitos (Soporta DNI, Libretas Electorales, RUC y Carnet de Extranjería)
        private static readonly Regex RxDni = new Regex(@"Identidad.*?(?<!\d)(\d{7,15})(?!\d)", RegexOptions.Compiled);
        private static readonly Regex RxCargo = new Regex(@"Cargo\s*:\s*(.+)", RegexOptions.Compiled);
        private static readonly Regex RxTipoPensionista = new Regex(@"Tipo de Pensionista\s*:\s*(.+)", RegexOptions.Compiled);
        private static readonly Regex RxTipoPension = new Regex(@"Tipo de Pension\s*:\s*(.+)", RegexOptions.Compiled);
        private static readonly Regex RxNivelMag = new Regex(@"Niv\.Mag\..*?:\s*(.+)", RegexOptions.Compiled);
        private static readonly Regex RxTiempoServ = new Regex(@"Tiempo de Servicio.*?:\s*([0-9-]+)", RegexOptions.Compiled);
        private static readonly Regex RxEsSalud = new Regex(@"ESSALUD\s*:\s*(.+)", RegexOptions.Compiled);
        private static readonly Regex RxFechaReg = new Regex(@"Fecha de Registro\s*:\s*(.+)", RegexOptions.Compiled);
        private static readonly Regex RxCuenta = new Regex(@"Cta\. TeleAhorro.*:\s*(.+)", RegexOptions.Compiled);
        private static readonly Regex RxLeyendaPerm = new Regex(@"Leyenda Permanente\s*:\s*(.*)", RegexOptions.Compiled);
        private static readonly Regex RxLeyendaMens = new Regex(@"Leyenda Mensual\s*:\s*(.*)", RegexOptions.Compiled);

        private static readonly Regex RxSeparador = new Regex(@"={10,}", RegexOptions.Compiled);

        // 🛡️ BLINDAJE 2: (?:\.\d{1,2})? hace que los céntimos sean OPCIONALES. Si dice 1,500 o 1,500.5, lo lee perfecto.
        private static readonly Regex RxMontoImponible = new Regex(@"M\.?Imponible\s*:?\s*([\d,]+(?:\.\d{1,2})?)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // 🛡️ BLINDAJE 3: [^\s]+ significa "Cualquier texto que no sea un espacio". Esto captura TODO tipo de código sin importar si trae slash, asteriscos o paréntesis.
        private static readonly Regex RxConcepto = new Regex(@"(?<=^|\s)([+-])([^\s]+)\s+([\d,]+(?:\.\d{1,2})?)", RegexOptions.Compiled);

        public async Task<List<BoletaCabecera>> ProcesarArchivoAsync(IFormFile archivoLis, string usuarioQueSube)
        {
            var boletasProcesadas = new List<BoletaCabecera>();
            EstadoParser estadoActual = EstadoParser.BuscandoCabecera;
            string mesGlobal = "01", anioGlobal = DateTime.Now.Year.ToString();

            string tApe = "", tNom = "", tFecNac = "", tDni = "", tCargo = "", tTipoPta = "", tTipoPen = "";
            string tNivMag = "", tTiemServ = "", tEsSalud = "", tFecReg = "", tCta = "", tLeyPer = "", tLeyMens = "";
            decimal tImp = 0;
            List<BoletaDetalle> tDet = new List<BoletaDetalle>();

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var encodingLatino = Encoding.GetEncoding("Windows-1252");

            using (var stream = archivoLis.OpenReadStream())
            using (var reader = new StreamReader(stream, encodingLatino))
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
                        if ((estadoActual == EstadoParser.LeyendoConceptos || tDet.Count > 0) && !string.IsNullOrEmpty(tDni))
                        {
                            GuardarBoletaTemporal(boletasProcesadas, tDni, tApe, tNom, tFecNac, tCargo, tTipoPta, tTipoPen, tNivMag, tTiemServ, tEsSalud, tFecReg, tCta, tLeyPer, tLeyMens, mesGlobal, anioGlobal, tImp, tDet, usuarioQueSube);
                            tDet = new List<BoletaDetalle>();
                            tApe = ""; tNom = ""; tFecNac = ""; tDni = ""; tCargo = ""; tTipoPta = ""; tTipoPen = "";
                            tNivMag = ""; tTiemServ = ""; tEsSalud = ""; tFecReg = ""; tCta = ""; tLeyPer = ""; tLeyMens = ""; tImp = 0;
                        }

                        tApe = matchApellidos.Groups[1].Value.Trim();
                        estadoActual = EstadoParser.LeyendoCabecera;
                        continue;
                    }

                    var matchImponible = RxMontoImponible.Match(linea);
                    if (matchImponible.Success)
                    {
                        tImp = decimal.Parse(matchImponible.Groups[1].Value.Replace(",", ""), CultureInfo.InvariantCulture);
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
                        var mNom = RxNombres.Match(linea); if (mNom.Success) tNom = mNom.Groups[1].Value.Trim();
                        var mFecNac = RxFechaNac.Match(linea); if (mFecNac.Success) tFecNac = mFecNac.Groups[1].Value.Trim();
                        var mDni = RxDni.Match(linea); if (mDni.Success) tDni = mDni.Groups[1].Value.Trim();
                        var mCargo = RxCargo.Match(linea); if (mCargo.Success) tCargo = mCargo.Groups[1].Value.Trim();
                        var mTipoPta = RxTipoPensionista.Match(linea); if (mTipoPta.Success) tTipoPta = mTipoPta.Groups[1].Value.Trim();
                        var mTipoPen = RxTipoPension.Match(linea); if (mTipoPen.Success) tTipoPen = mTipoPen.Groups[1].Value.Trim();
                        var mNivMag = RxNivelMag.Match(linea); if (mNivMag.Success) tNivMag = mNivMag.Groups[1].Value.Trim();
                        var mTs = RxTiempoServ.Match(linea); if (mTs.Success) tTiemServ = mTs.Groups[1].Value.Trim();
                        var mEs = RxEsSalud.Match(linea); if (mEs.Success) tEsSalud = mEs.Groups[1].Value.Trim();
                        var mFecReg = RxFechaReg.Match(linea); if (mFecReg.Success) tFecReg = mFecReg.Groups[1].Value.Trim();
                        var mCta = RxCuenta.Match(linea); if (mCta.Success) tCta = mCta.Groups[1].Value.Trim();
                        var mLeyP = RxLeyendaPerm.Match(linea); if (mLeyP.Success) tLeyPer = mLeyP.Groups[1].Value.Trim();
                        var mLeyM = RxLeyendaMens.Match(linea); if (mLeyM.Success) tLeyMens = mLeyM.Groups[1].Value.Trim();
                    }
                    else if (estadoActual == EstadoParser.LeyendoConceptos)
                    {
                        var matchesConceptos = RxConcepto.Matches(linea);
                        foreach (Match m in matchesConceptos)
                        {
                            string tipo = m.Groups[1].Value == "+" ? "I" : "D";
                            decimal montoReal = decimal.Parse(m.Groups[3].Value.Replace(",", ""), CultureInfo.InvariantCulture);
                            tDet.Add(new BoletaDetalle(m.Groups[2].Value.ToUpper(), m.Groups[2].Value.ToUpper(), tipo, montoReal, usuarioQueSube));
                        }
                    }
                }

                // Guardar la última boleta si el archivo termina
                if (!string.IsNullOrEmpty(tDni) && tDet.Count > 0)
                {
                    GuardarBoletaTemporal(boletasProcesadas, tDni, tApe, tNom, tFecNac, tCargo, tTipoPta, tTipoPen, tNivMag, tTiemServ, tEsSalud, tFecReg, tCta, tLeyPer, tLeyMens, mesGlobal, anioGlobal, tImp, tDet, usuarioQueSube);
                }
            }

            return boletasProcesadas;
        }

        private void GuardarBoletaTemporal(List<BoletaCabecera> lista, string dni, string ape, string nom, string fecNac, string cargo, string tipoPta, string tipoPen, string nivMag, string tServ, string eSalud, string fReg, string cta, string leyPer, string leyMen, string mes, string anio, decimal imp, List<BoletaDetalle> det, string usu)
        {
            decimal tIng = 0, tDes = 0;
            foreach (var d in det) { if (d.TipoConcepto == "I") tIng += d.Monto; else tDes += d.Monto; }

            var nBoleta = new BoletaCabecera(dni, ape, nom, fecNac, cargo, tipoPta, tipoPen, nivMag, tServ, eSalud, fReg, cta, leyPer, leyMen, mes, anio, imp, tIng, tDes, tIng - tDes, usu);
            foreach (var d in det) nBoleta.AgregarDetalle(d);
            lista.Add(nBoleta);
        }

        // 🛡️ BLINDAJE 4: Tolerancia a fallas ortográficas y abreviaturas (SEPT/SET, ENE/ENERO)
        private string ConvertirMesANumero(string m)
        {
            var mes = m.ToUpper().Trim();
            if (mes.StartsWith("ENE")) return "01";
            if (mes.StartsWith("FEB")) return "02";
            if (mes.StartsWith("MAR")) return "03";
            if (mes.StartsWith("ABR")) return "04";
            if (mes.StartsWith("MAY")) return "05";
            if (mes.StartsWith("JUN")) return "06";
            if (mes.StartsWith("JUL")) return "07";
            if (mes.StartsWith("AGO")) return "08";
            if (mes.StartsWith("SET") || mes.StartsWith("SEP")) return "09"; // Cubre SETIEMBRE y SEPTIEMBRE
            if (mes.StartsWith("OCT")) return "10";
            if (mes.StartsWith("NOV")) return "11";
            if (mes.StartsWith("DIC")) return "12";
            return "00";
        }
    }
}