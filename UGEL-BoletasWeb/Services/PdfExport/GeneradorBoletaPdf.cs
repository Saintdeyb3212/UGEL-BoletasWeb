using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;
using System.Linq;
using UGEL_BoletasWeb.Models.Entities;

namespace UGEL_BoletasWeb.Services.PdfExport
{
    public interface IGeneradorBoletaPdf
    {
        byte[] GenerarBoleta(BoletaCabecera boleta);
    }

    public class GeneradorBoletaPdf : IGeneradorBoletaPdf
    {
        public GeneradorBoletaPdf()
        {
            // Activación obligatoria de la licencia comunitaria de QuestPDF
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] GenerarBoleta(BoletaCabecera boleta)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.0f, Unit.Centimetre); // Reducido a 1.0cm para maximizar el área útil vertical
                    page.PageColor(Colors.White);

                    // Soporte nativo completo para tildes, caracteres especiales y letra Ñ usando Arial
                    page.DefaultTextStyle(x => x.FontSize(8.5f).FontFamily(Fonts.Arial).FontColor("#1e293b"));

                    page.Header().Element(x => ComposeHeader(x, boleta));
                    page.Content().Element(x => ComposeContent(x, boleta));
                    page.Footer().Element(ComposeFooter);
                });
            });

            using var ms = new MemoryStream();
            document.GeneratePdf(ms);
            return ms.ToArray();
        }

        private void ComposeHeader(IContainer container, BoletaCabecera boleta)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("MINISTERIO DE EDUCACIÓN").FontSize(10.5f).Bold().FontColor("#0a4b7d");
                    column.Item().Text("DRE JUNÍN - UGEL CONCEPCIÓN").FontSize(9.0f).SemiBold().FontColor("#10609e");
                    column.Item().Text("SISTEMA DE DIGITALIZACIÓN Y CONSULTA DE PLANILLAS").FontSize(7.0f).FontColor("#64748b");
                });

                row.ConstantItem(180).Column(column =>
                {
                    column.Item().Border(1).BorderColor("#0a4b7d").Background("#f8fafc").Padding(4).AlignCenter().Column(c =>
                    {
                        c.Item().Text("BOLETA DIGITAL DE PAGO").FontSize(9.5f).Bold().FontColor("#0a4b7d");
                        c.Item().Text($"PERIODO: {boleta.Mes} - {boleta.Anio}").FontSize(9.0f).Bold().FontColor("#24b3f1");
                    });
                });
            });
        }

        private void ComposeContent(IContainer container, BoletaCabecera boleta)
        {
            container.PaddingVertical(0.4f, Unit.Centimetre).Column(column =>
            {
                // ======================================================================
                // 1. CUADRANTE DE IDENTIFICACIÓN OPTIMIZADO (2 Columnas de Datos Lado a Lado)
                // ======================================================================
                column.Item().Border(1).BorderColor("#cbd5e1").Table(table =>
                {
                    // Definimos la estructura de 4 columnas: 
                    // [Etiqueta Izq (135pt) | Separador (10pt) | Valor Izq] | [Etiqueta Der (135pt) | Separador (10pt) | Valor Der]
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(135);
                        columns.ConstantColumn(10);
                        columns.RelativeColumn();
                        columns.ConstantColumn(135);
                        columns.ConstantColumn(10);
                        columns.RelativeColumn();
                    });

                    // Métodos auxiliares locales para inyección limpia de celdas (DRY)
                    void AgregarCeldaEtiqueta(string texto)
                    {
                        table.Cell().Background("#f1f5f9").PaddingHorizontal(6).PaddingVertical(3)
                             .Text(texto).Bold().FontSize(7.5f).FontColor("#475569");
                    }

                    void AgregarCeldaSeparador()
                    {
                        table.Cell().Background("#f1f5f9").PaddingHorizontal(2).PaddingVertical(3)
                             .Text(":").Bold().FontColor("#475569");
                    }

                    void AgregarCeldaValor(string valor, string colorHex = "#0f172a", bool esBold = false)
                    {
                        // 🚀 FIX: Variable corregida (celdaTexto en vez de "text block" con espacio)
                        var celdaTexto = table.Cell().Background(Colors.White).PaddingHorizontal(6).PaddingVertical(3)
                                              .Text(valor ?? "-").FontSize(8).FontColor(colorHex);

                        if (esBold) celdaTexto.Bold();
                    }

                    void AgregarFilaCompleta(string label1, string val1, string label2, string val2, bool val1Primary = false)
                    {
                        AgregarCeldaEtiqueta(label1); AgregarCeldaSeparador(); AgregarCeldaValor(val1, val1Primary ? "#0a4b7d" : "#0f172a", val1Primary);
                        AgregarCeldaEtiqueta(label2); AgregarCeldaSeparador(); AgregarCeldaValor(val2);
                    }

                    // Distribución organizada en pares lógicos horizontales para comprimir espacio vertical
                    AgregarFilaCompleta("Apellidos", boleta.Apellidos, "Tipo de Pensionista", boleta.TipoPensionista);
                    AgregarFilaCompleta("Nombres", boleta.Nombres, "Tipo de Pension", boleta.TipoPension);
                    AgregarFilaCompleta("Fecha de Nacimiento", boleta.FechaNacimiento, "Niv.Mag./G.Ocup./Horas/HrsAdd", boleta.NivelMagisterial);
                    AgregarFilaCompleta("Documento de Identidad", $"(Lib.Electoral o D.N.) {boleta.DNI}", "ESSALUD", boleta.CodigoEsSalud);
                    AgregarFilaCompleta("Cargo", boleta.Cargo, "Cta. TeleAhorro o Nro. Cheque", boleta.CuentaBancaria, true);

                    // Para las fechas de registro que ocupan una fila intermedia
                    AgregarCeldaEtiqueta("Fecha de Registro"); AgregarCeldaSeparador(); AgregarCeldaValor(boleta.FechasRegistro);
                    table.Cell().RowSpan(1).ColumnSpan(3).Background(Colors.White); // Relleno vacío a la derecha para equilibrar la fila

                    // Las leyendas largas ocupan todo el ancho horizontal (combinando columnas de valor)
                    void AgregarFilaLeyendaLarga(string etiqueta, string valor)
                    {
                        AgregarCeldaEtiqueta(etiqueta);
                        AgregarCeldaSeparador();
                        table.Cell().RowSpan(1).ColumnSpan(4).Background(Colors.White).PaddingHorizontal(6).PaddingVertical(3)
                             .Text(valor ?? "-").FontSize(8).FontColor("#0f172a");
                    }

                    AgregarFilaLeyendaLarga("Leyenda Permanente", boleta.LeyendaPermanente);
                    AgregarFilaLeyendaLarga("Leyenda Mensual", boleta.LeyendaMensual);
                });

                column.Item().PaddingTop(8).PaddingBottom(3).Text("DESGLOSE DE CONCEPTOS LIQUIDADOS").Bold().FontSize(8.5f).FontColor("#0a4b7d");

                // ======================================================================
                // 2. DOS COLUMNAS ESTRICTAS PARA DINERO (Ingresos vs Descuentos)
                // ======================================================================
                column.Item().Row(row =>
                {
                    // Bloque de Ingresos (Verde Esmeralda)
                    row.RelativeItem().Border(1).BorderColor("#e2e8f0").Column(col =>
                    {
                        col.Item().Background("#f0fdf4").Padding(3).AlignCenter()
                           .Text("CONCEPTOS REMUNERATIVOS (INGRESOS)").Bold().FontColor("#065f46").FontSize(7.5f);

                        col.Item().Padding(3).Table(t =>
                        {
                            t.ColumnsDefinition(c => { c.RelativeColumn(); c.ConstantColumn(65); });
                            foreach (var item in boleta.Detalles.Where(d => d.TipoConcepto == "I"))
                            {
                                t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(2).Text(item.CodigoConcepto).FontSize(7.5f);
                                t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(2).AlignRight().Text(item.Monto.ToString("N2")).FontSize(7.5f).Bold().FontColor("#10b981");
                            }
                        });
                    });

                    row.ConstantItem(10); // Espaciador central fijo optimizado

                    // Bloque de Descuentos
                    row.RelativeItem().Border(1).BorderColor("#e2e8f0").Column(col =>
                    {
                        col.Item().Background("#fef2f2").Padding(3).AlignCenter()
                           .Text("DESCUENTOS Y APORTACIONES").Bold().FontColor("#b91c1c").FontSize(7.5f);

                        col.Item().Padding(3).Table(t =>
                        {
                            t.ColumnsDefinition(c => { c.RelativeColumn(); c.ConstantColumn(65); });
                            foreach (var item in boleta.Detalles.Where(d => d.TipoConcepto == "D"))
                            {
                                t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(2).Text(item.CodigoConcepto).FontSize(7.5f);
                                t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(2).AlignRight().Text(item.Monto.ToString("N2")).FontSize(7.5f).Bold().FontColor("#ef4444");
                            }
                        });
                    });
                });

                // ======================================================================
                // 3. PIE DE TOTALES CONTABLE COMPACTO
                // ======================================================================
                column.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); });

                    table.Cell().Border(1).BorderColor("#cbd5e1").Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text("M. IMPONIBLE").Bold().FontSize(7.5f);
                    table.Cell().Border(1).BorderColor("#cbd5e1").Background("#f0fdf4").Padding(4).AlignCenter().Text("TOTAL INGRESOS").Bold().FontSize(7.5f).FontColor("#047857");
                    table.Cell().Border(1).BorderColor("#cbd5e1").Background("#fef2f2").Padding(4).AlignCenter().Text("TOTAL DESCUENTOS").Bold().FontSize(7.5f).FontColor("#b91c1c");
                    table.Cell().Border(1).BorderColor("#0a4b7d").Background("#0a4b7d").Padding(4).AlignCenter().Text("LÍQUIDO A PAGAR").Bold().FontSize(7.5f).FontColor(Colors.White);

                    table.Cell().Border(1).BorderColor("#cbd5e1").Padding(4).AlignCenter().Text($"S/ {boleta.MontoImponible:N2}").Bold().FontSize(8.5f);
                    table.Cell().Border(1).BorderColor("#cbd5e1").Padding(4).AlignCenter().Text($"S/ {boleta.TotalIngresos:N2}").FontColor("#065f46").FontSize(8.5f).Bold();
                    table.Cell().Border(1).BorderColor("#cbd5e1").Padding(4).AlignCenter().Text($"S/ {boleta.TotalDescuentos:N2}").FontColor("#991b1b").FontSize(8.5f).Bold();

                    table.Cell().Border(1).BorderColor("#0a4b7d").Background("#f1f5f9").Padding(4).AlignCenter()
                         .Text($"S/ {boleta.MontoLiquido:N2}").Bold().FontSize(10).FontColor("#0a4b7d");
                });
            });
        }

        private void ComposeFooter(IContainer container)
        {
            container.AlignCenter().Text(x =>
            {
                x.Span("Documento Oficial Emitido por el Sistema de Consulta de Boletas de la UGEL Concepción - Página ").FontSize(7.0f).FontColor(Colors.Grey.Darken1);
                x.CurrentPageNumber().FontSize(7.0f).FontColor(Colors.Grey.Darken1);
                x.Span(" de ").FontSize(7.0f).FontColor(Colors.Grey.Darken1);
                x.TotalPages().FontSize(7.0f).FontColor(Colors.Grey.Darken1);
            });
        }
    }
}