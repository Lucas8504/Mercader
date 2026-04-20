using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using System.Globalization;
using OfficeOpenXml.Drawing.Chart;
using Color = System.Drawing.Color;
using Mercader.Models;
using Mercader.Domain.Entities;
using Mercader.ViewModels;

namespace Mercader
{
    public static class ExportExcel
    {
        public static async Task ExportarBalanceAExcelAsync(BalanceExportDto data, string rutaArchivo)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (ExcelPackage package = new ExcelPackage())
            {
                // Crear todas las hojas
                CrearHojaResumen(package, data);
                CrearHojaVentas(package, data.Ventas.ToList());
                CrearHojaGastos(package, data.Gastos.ToList());
                CrearHojaEncargos(package, data.Encargos.ToList());
                CrearHojaGrafico(package, data);

                // Guardar el archivo
                FileInfo fileInfo = new FileInfo(rutaArchivo);
                await package.SaveAsAsync(fileInfo);
            }
        }

        private static void CrearHojaResumen(ExcelPackage package, BalanceExportDto data)
        {
            var worksheet = package.Workbook.Worksheets.Add("📊 Resumen Ejecutivo");

            // Configurar ancho de columnas
            worksheet.Column(1).Width = 25;
            worksheet.Column(2).Width = 20;
            worksheet.Column(3).Width = 20;
            worksheet.Column(4).Width = 20;

            // TÍTULO PRINCIPAL
            worksheet.Cells["A1:D1"].Merge = true;
            worksheet.Cells["A1"].Value = "REPORTE FINANCIERO - BALANCE GENERAL";
            var tituloRange = worksheet.Cells["A1:D1"];
            tituloRange.Style.Font.Bold = true;
            tituloRange.Style.Font.Size = 18;
            tituloRange.Style.Font.Color.SetColor(System.Drawing.Color.White);
            tituloRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
            tituloRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(44, 62, 80)); // #2C3E50
            tituloRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            tituloRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            worksheet.Row(1).Height = 35;

            // Fecha de generación
            worksheet.Cells["A2"].Value = $"Generado el: {DateTime.Now:dd/MM/yyyy HH:mm}";
            worksheet.Cells["A2"].Style.Font.Italic = true;

            // Fix for CS0117: 'Color' no contiene una definición para 'Gray'
            // The issue arises because `Microsoft.Maui.Graphics.Color` does not have a predefined `Gray` property.
            // Replace the problematic line with the following:

            worksheet.Cells["A2"].Style.Font.Color.SetColor(Color.FromArgb(128, 128, 128)); // Gray color using ARGB values
            

            // MÉTRICAS PRINCIPALES
            int filaActual = 4;

            // Encabezado de métricas
            worksheet.Cells[filaActual, 1].Value = "MÉTRICAS CLAVE";
            worksheet.Cells[filaActual, 2].Value = "VALOR";
            worksheet.Cells[filaActual, 3].Value = "PARTICIPACIÓN";
            worksheet.Cells[filaActual, 4].Value = "ESTADO";

            var encabezadoRange = worksheet.Cells[filaActual, 1, filaActual, 4];
            EstilarEncabezado(encabezadoRange, Color.FromArgb(52, 73, 94)); // #34495E

            filaActual++;

            // Calcular métricas
            var ganancias = data.Ganancias;
            var ventas = data.TotalVentas;
            var gastos = data.TotalGastos;
            var encargos = data.TotalEncargos;
            var totalOperaciones =
                data.TotalVentas +
                Math.Abs(data.TotalGastos) +
                Math.Abs(data.TotalEncargos);

            // Datos de métricas
            var datosMetricas = new object[,]
            {
                { "💰 Ganancias Netas", ganancias, "", ganancias >= 0 ? "✅ POSITIVO" : "❌ NEGATIVO" },
                { "📈 Ventas Totales", ventas, $"{(totalOperaciones > 0 ? (ventas/totalOperaciones*100):0):F1}%", "💚 INGRESOS" },
                { "📉 Gastos Totales", gastos, $"{(totalOperaciones > 0 ? (Math.Abs(gastos)/totalOperaciones*100):0):F1}%", "🔴 EGRESOS" },
                { "📋 Encargos Pendientes", encargos, $"{(totalOperaciones > 0 ? (Math.Abs(encargos)/totalOperaciones*100):0):F1}%", "🟡 PENDIENTE" }
            };

            // Llenar datos
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    worksheet.Cells[filaActual + i, j + 1].Value = datosMetricas[i, j];
                }

                // Formatear valores monetarios
                worksheet.Cells[filaActual + i, 2].Style.Numberformat.Format = "$#,##0";

                // Aplicar estilos alternados
                var filaRange = worksheet.Cells[filaActual + i, 1, filaActual + i, 4];
                if (i % 2 == 0)
                {
                    filaRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    filaRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(248, 249, 250)); // #F8F9FA
                }
                filaRange.Style.Border.BorderAround(ExcelBorderStyle.Thin);

                // Color especial para ganancias
                if (i == 0)
                {
                    var colorGanancias = ganancias >= 0 ? Color.FromArgb(212, 237, 218) : Color.FromArgb(248, 215, 218);
                    filaRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    filaRange.Style.Fill.BackgroundColor.SetColor(colorGanancias);
                    filaRange.Style.Font.Bold = true;
                }
            }

            filaActual += 6;

            // ANÁLISIS POR PERÍODO
            CrearSeccionAnalisisPeriodo(worksheet, data, filaActual);

            int filaGrafico = filaActual + 2;

            // Datos para gráfico
            worksheet.Cells[filaGrafico, 6].Value = "Concepto";
            worksheet.Cells[filaGrafico, 7].Value = "Monto";

            worksheet.Cells[filaGrafico + 1, 6].Value = "Ventas";
            worksheet.Cells[filaGrafico + 1, 7].Value = data.TotalVentas;

            worksheet.Cells[filaGrafico + 2, 6].Value = "Gastos";
            worksheet.Cells[filaGrafico + 2, 7].Value = Math.Abs(data.TotalGastos);

            worksheet.Cells[filaGrafico + 3, 6].Value = "Encargos";
            worksheet.Cells[filaGrafico + 3, 7].Value = Math.Abs(data.TotalEncargos);

            var chart = worksheet.Drawings.AddChart("DistribucionBalance", eChartType.Doughnut);

            chart.Title.Text = "Distribución del Balance";
            chart.Title.Font.Size = 14;
            chart.Title.Font.Bold = true;

            chart.SetPosition(4, 0, 4, 0);   // fila, offset, columna, offset
            chart.SetSize(420, 320);

            // Serie
            var serie = chart.Series.Add(
                worksheet.Cells[filaGrafico + 1, 7, filaGrafico + 3, 7],
                worksheet.Cells[filaGrafico + 1, 6, filaGrafico + 3, 6]
            );

            serie.Header = "Distribución";


            var doughnut = chart.PlotArea.ChartTypes[0] as ExcelDoughnutChart;

            doughnut!.DataLabel.ShowPercent = true;
            doughnut.DataLabel.ShowCategory = true;
            doughnut.DataLabel.Position = eLabelPosition.BestFit;

            // Colores
            doughnut.Series[0].DataPoints[0].Fill.Color = Color.FromArgb(40, 167, 69);  // Ventas (verde)
            doughnut.Series[0].DataPoints[1].Fill.Color = Color.FromArgb(220, 53, 69);  // Gastos (rojo)
            doughnut.Series[0].DataPoints[2].Fill.Color = Color.FromArgb(255, 193, 7);  // Encargos (amarillo)



        }

        private static void CrearSeccionAnalisisPeriodo(ExcelWorksheet worksheet, BalanceExportDto data, int filaInicio)
        {
            // Título de sección
            worksheet.Cells[filaInicio, 1, filaInicio, 4].Merge = true;
            worksheet.Cells[filaInicio, 1].Value = "📊 ANÁLISIS POR PERÍODO (ÚLTIMOS 6 MESES)";
            var tituloRange = worksheet.Cells[filaInicio, 1, filaInicio, 4];
            EstilarEncabezado(tituloRange, Color.FromArgb(52, 152, 219)); // #3498DB

            filaInicio += 2;

            // Generar datos por mes
            var datosMensuales = GenerarDatosMensuales(data);

            // Encabezados
            worksheet.Cells[filaInicio, 1].Value = "MES";
            worksheet.Cells[filaInicio, 2].Value = "VENTAS";
            worksheet.Cells[filaInicio, 3].Value = "GASTOS";
            worksheet.Cells[filaInicio, 4].Value = "GANANCIA";

            var encabezadosRange = worksheet.Cells[filaInicio, 1, filaInicio, 4];
            EstilarEncabezado(encabezadosRange, Color.FromArgb(22, 160, 133)); // #16A085

            filaInicio++;

            // Datos mensuales
            for (int i = 0; i < datosMensuales.Count; i++)
            {
                var dato = datosMensuales[i];
                var fila = filaInicio + i;

                worksheet.Cells[fila, 1].Value = dato.Mes;
                worksheet.Cells[fila, 2].Value = dato.Ventas;
                worksheet.Cells[fila, 3].Value = dato.Gastos;
                worksheet.Cells[fila, 4].Value = dato.Ganancia;

                // Formato monetario
                worksheet.Cells[fila, 2, fila, 4].Style.Numberformat.Format = "$#,##0";

                // Color alternado
                var filaRange = worksheet.Cells[fila, 1, fila, 4];
                if (i % 2 == 0)
                {
                    filaRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    filaRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(248, 249, 250));
                }

                // Color para ganancia
                if (dato.Ganancia >= 0)
                {
                    worksheet.Cells[fila, 4].Style.Font.Color.SetColor(Color.FromArgb(40, 167, 69)); // Verde
                }
                else
                {
                    worksheet.Cells[fila, 4].Style.Font.Color.SetColor(Color.FromArgb(220, 53, 69)); // Rojo
                }

                filaRange.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            // Agregar totales
            filaInicio += datosMensuales.Count;
            var totalVentas = datosMensuales.Sum(d => d.Ventas);
            var totalGastos = datosMensuales.Sum(d => d.Gastos);
            var totalGanancias = datosMensuales.Sum(d => d.Ganancia);

            worksheet.Cells[filaInicio, 1].Value = "TOTALES";
            worksheet.Cells[filaInicio, 2].Value = totalVentas;
            worksheet.Cells[filaInicio, 3].Value = totalGastos;
            worksheet.Cells[filaInicio, 4].Value = totalGanancias;

            var totalesRange = worksheet.Cells[filaInicio, 1, filaInicio, 4];
            totalesRange.Style.Font.Bold = true;
            totalesRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
            totalesRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 193, 7)); // #FFC107
            totalesRange.Style.Numberformat.Format = "$#,##0";
        }

        private static void CrearHojaVentas(ExcelPackage package, List<Ventas> ventas)
        {
            var worksheet = package.Workbook.Worksheets.Add("💰 Ventas");
            CrearTablaDetallada(worksheet, "REGISTRO DE VENTAS",
                new[] { "FECHA", "DESCRIPCIÓN", "CANTIDAD", "PRECIO UNIT.", "TOTAL" },
                ventas.Count,
                (i) => new object[]
                {
                    ventas[i].Fecha.ToString("dd/MM/yyyy"),
                    ventas[i].Descripcion ?? "N/A",
                    ventas[i].Cantidad,
                    ventas[i].Precio,
                    ventas[i].Precio * ventas[i].Cantidad
                },
                Color.FromArgb(40, 167, 69)); // Verde
        }

        private static void CrearHojaGastos(ExcelPackage package, List<Gasto> gastos)
        {
            var worksheet = package.Workbook.Worksheets.Add("💸 Gastos");
            CrearTablaDetallada(worksheet, "REGISTRO DE GASTOS",
                new[] { "FECHA", "DESCRIPCIÓN", "CANTIDAD", "MONTO UNIT.", "TOTAL" },
                gastos.Count,
                (i) => new object[]
                {
                    gastos[i].Fecha.ToString("dd/MM/yyyy"),
                    gastos[i].Descripcion ?? "N/A",
                    gastos[i].Cantidad,
                    gastos[i].Monto,
                    gastos[i].Monto * gastos[i].Cantidad
                },
                Color.FromArgb(220, 53, 69)); // Rojo
        }

        private static void CrearHojaEncargos(ExcelPackage package, List<Encargo> encargos)
        {
            var worksheet = package.Workbook.Worksheets.Add("📋 Encargos");
            CrearTablaDetallada(worksheet, "REGISTRO DE ENCARGOS",
                new[] { "FECHA", "NOMBRE", "DESCRIPCIÓN", "CANTIDAD", "PRECIO UNIT.", "TOTAL", "FECHA ENTREGA" },
                encargos.Count,
                (i) => new object[]
                {
                    encargos[i].Fecha.ToString("dd/MM/yyyy"),
                    encargos[i].Nombre ?? "N/A",
                    encargos[i].Descripcion ?? "N/A",
                    encargos[i].Cantidad,
                    encargos[i].Precio,
                    encargos[i].Precio * encargos[i].Cantidad,
                    encargos[i].FechaEntrega.ToString("dd/MM/yyyy")
                },
                Color.FromArgb(255, 193, 7)); // Amarillo
        }

        private static void CrearHojaGrafico(ExcelPackage package, BalanceExportDto data)
        {
            var worksheet = package.Workbook.Worksheets.Add("📈 Gráfico Financiero");

            // Título
            worksheet.Cells["A1:D1"].Merge = true;
            worksheet.Cells["A1"].Value = "EVOLUCIÓN FINANCIERA - ÚLTIMOS 6 MESES";
            var tituloRange = worksheet.Cells["A1:D1"];
            EstilarEncabezado(tituloRange, Color.FromArgb(142, 68, 173)); // #8E44AD
            worksheet.Row(1).Height = 30;

            // Generar datos mensuales
            var datosMensuales = GenerarDatosMensuales(data);

            // Encabezados de datos
            worksheet.Cells["A3"].Value = "MES";
            worksheet.Cells["B3"].Value = "VENTAS";
            worksheet.Cells["C3"].Value = "GASTOS";
            worksheet.Cells["D3"].Value = "GANANCIAS";

            var encabezadosRange = worksheet.Cells["A3:D3"];
            EstilarEncabezado(encabezadosRange, Color.FromArgb(44, 62, 80)); // #2C3E50

            // Llenar datos
            for (int i = 0; i < datosMensuales.Count; i++)
            {
                var dato = datosMensuales[i];
                var fila = 4 + i;

                worksheet.Cells[fila, 1].Value = dato.Mes;
                worksheet.Cells[fila, 2].Value = dato.Ventas;
                worksheet.Cells[fila, 3].Value = Math.Abs(dato.Gastos);
                worksheet.Cells[fila, 4].Value = dato.Ganancia;


                // Formato alternado
                var filaRange = worksheet.Cells[fila, 1, fila, 4];
                if (i % 2 == 0)
                {
                    filaRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    filaRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(248, 249, 250));
                }

                // Formato monetario
                worksheet.Cells[fila, 2, fila, 4].Style.Numberformat.Format = "#,##0";
            }

            // Crear gráfico
            var chart = worksheet.Drawings.AddChart("GraficoFinanciero", eChartType.Line);
            chart.Title.Text = "Evolución Financiera Mensual";
            chart.Title.Font.Size = 16;
            chart.Title.Font.Bold = true;

            // Configurar posición del gráfico
            chart.SetPosition(4 + datosMensuales.Count + 2, 0, 0, 0);
            chart.SetSize(800, 400);

            // Agregar series de datos
            var serieVentas = chart.Series.Add(worksheet.Cells[4, 2, 4 + datosMensuales.Count - 1, 2],
                                              worksheet.Cells[4, 1, 4 + datosMensuales.Count - 1, 1]);
            serieVentas.Header = "Ventas";

            var serieGastos = chart.Series.Add(worksheet.Cells[4, 3, 4 + datosMensuales.Count - 1, 3],
                                              worksheet.Cells[4, 1, 4 + datosMensuales.Count - 1, 1]);
            serieGastos.Header = "Gastos";

            var serieGanancias = chart.Series.Add(worksheet.Cells[4, 4, 4 + datosMensuales.Count - 1, 4],
                                                 worksheet.Cells[4, 1, 4 + datosMensuales.Count - 1, 1]);
            serieGanancias.Header = "Ganancias";

            // Configurar ejes
            chart.XAxis.Title.Text = "Período";
            chart.YAxis.Title.Text = "Monto ($)";
            chart.Legend.Position = eLegendPosition.Bottom;

            // Configurar anchos de columna
            worksheet.Column(1).Width = 15;
            worksheet.Column(2).Width = 15;
            worksheet.Column(3).Width = 15;
            worksheet.Column(4).Width = 15;

            // Agregar instrucciones
            var filaInstrucciones = 4 + datosMensuales.Count + 20;
            worksheet.Cells[filaInstrucciones, 1].Value = "💡 ANÁLISIS AUTOMÁTICO:";
            worksheet.Cells[filaInstrucciones, 1].Style.Font.Bold = true;
            worksheet.Cells[filaInstrucciones, 1].Style.Font.Color.SetColor(Color.FromArgb(23, 162, 184));

            worksheet.Cells[filaInstrucciones + 1, 1].Value = "• El gráfico muestra la evolución de tus finanzas en los últimos 6 meses";
            worksheet.Cells[filaInstrucciones + 2, 1].Value = "• Línea verde: Ventas (ingresos)";
            worksheet.Cells[filaInstrucciones + 3, 1].Value = "• Línea roja: Gastos (egresos)";
            worksheet.Cells[filaInstrucciones + 4, 1].Value = "• Línea azul: Ganancias netas (ventas - gastos)";
            worksheet.Cells[filaInstrucciones + 5, 1].Value = "• Puedes hacer clic derecho en el gráfico para personalizarlo";
        }

        private static void CrearTablaDetallada(ExcelWorksheet worksheet, string titulo,
            string[] encabezados, int cantidadDatos, Func<int, object[]> obtenerDatos, Color colorTema)
        {
            // Configurar anchos de columnas
            for (int i = 1; i <= encabezados.Length; i++)
            {
                worksheet.Column(i).Width = 15;
            }

            // Título
            worksheet.Cells[1, 1, 1, encabezados.Length].Merge = true;
            worksheet.Cells[1, 1].Value = titulo;
            var tituloRange = worksheet.Cells[1, 1, 1, encabezados.Length];
            EstilarEncabezado(tituloRange, colorTema);
            worksheet.Row(1).Height = 30;

            // Información adicional
            worksheet.Cells[3, 1].Value = $"Total de registros: {cantidadDatos}";
            worksheet.Cells[3, 1].Style.Font.Bold = true;

            // Encabezados de tabla
            for (int i = 0; i < encabezados.Length; i++)
            {
                worksheet.Cells[5, i + 1].Value = encabezados[i];
            }
            var encabezadosRange = worksheet.Cells[5, 1, 5, encabezados.Length];
            EstilarEncabezado(encabezadosRange, Color.FromArgb(73, 80, 87)); // #495057

            // Datos
            for (int i = 0; i < cantidadDatos; i++)
            {
                var fila = 6 + i;
                var datos = obtenerDatos(i);

                for (int j = 0; j < datos.Length; j++)
                {
                    worksheet.Cells[fila, j + 1].Value = datos[j];
                }

                // Estilo alternado
                var filaRange = worksheet.Cells[fila, 1, fila, encabezados.Length];
                if (i % 2 == 0)
                {
                    filaRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    filaRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(248, 249, 250));
                }
                filaRange.Style.Border.BorderAround(ExcelBorderStyle.Thin);

                // Formato monetario para columnas de precio y total
                for (int j = 0; j < encabezados.Length; j++)
                {
                    if (encabezados[j].Contains("PRECIO") || encabezados[j].Contains("MONTO") || encabezados[j].Contains("TOTAL"))
                    {
                        worksheet.Cells[fila, j + 1].Style.Numberformat.Format = "$#,##0.00";
                    }
                }
            }

            // Marco general
            var tablaCompleta = worksheet.Cells[5, 1, 5 + cantidadDatos, encabezados.Length];
            tablaCompleta.Style.Border.BorderAround(ExcelBorderStyle.Medium);
        }

        private static void EstilarEncabezado(ExcelRange range, Color color)
        {
            range.Style.Font.Bold = true;
            range.Style.Font.Color.SetColor(Color.White);
            range.Style.Font.Size = 12;
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(color);
            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            range.Style.Border.BorderAround(ExcelBorderStyle.Medium);
        }

        private static List<DatoMensual> GenerarDatosMensuales(BalanceExportDto data)
        {
            var hoy = DateTime.Today;
            var ultimosMeses = Enumerable.Range(0, 6)
                .Select(i => hoy.AddMonths(-i))
                .Reverse()
                .ToList();

            return ultimosMeses.Select(mes =>
            {
                var ventasMes = data.Ventas.Where(v => v.Fecha.Year == mes.Year && v.Fecha.Month == mes.Month);
                var gastosMes = data.Gastos.Where(g => g.Fecha.Year == mes.Year && g.Fecha.Month == mes.Month);

                var totalVentas = ventasMes.Sum(v => v.Precio * v.Cantidad);
                var totalGastos = gastosMes.Sum(g => g.Monto * g.Cantidad);
                var ganancia = totalVentas - totalGastos;

                return new DatoMensual
                {
                    Mes = mes.ToString("MMM yyyy", new CultureInfo("es-ES")),
                    Ventas = totalVentas,
                    Gastos = totalGastos,
                    Ganancia = ganancia
                };
            }).ToList();
        }

        private class DatoMensual
        {
            public string Mes { get; set; } = "";
            public decimal Ventas { get; set; }
            public decimal Gastos { get; set; }
            public decimal Ganancia { get; set; }
        }
    }
}