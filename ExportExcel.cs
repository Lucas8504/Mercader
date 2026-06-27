using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using NPOI.XDDF.UserModel;
using NPOI.XDDF.UserModel.Chart;
using NPOI.OpenXmlFormats.Dml;
using Mercader.Domain.Entities;
using Mercader.Models;
using SkiaSharp;
using HA = NPOI.SS.UserModel.HorizontalAlignment;
using VA = NPOI.SS.UserModel.VerticalAlignment;

namespace Mercader
{
    public static class ExportExcel
    {
        public static async Task ExportarBalanceAExcelAsync(BalanceExportDto data, string rutaArchivo)
        {
            using (var workbook = new XSSFWorkbook())
            {
                CrearHojaVentas(workbook, data.Ventas.ToList());
                CrearHojaGastos(workbook, data.Gastos.ToList());
                CrearHojaEncargos(workbook, data.Encargos.ToList());

                try { CrearHojaResumen(workbook, data); }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ExportExcel] CRITICAL - Resumen sheet failed: {ex}");
                }

                try { CrearHojaGrafico(workbook, data); }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ExportExcel] CRITICAL - Charts sheet failed: {ex}");
                }

                await Task.Run(() =>
                {
                    using var fs = new FileStream(rutaArchivo, FileMode.Create, FileAccess.Write);
                    workbook.Write(fs);
                });
            }
        }

        // ======================================================================
        // ESTILOS COMPARTIDOS (creados una sola vez para evitar duplicados)
        // ======================================================================

        /// <summary>Fondo celeste claro, texto negro, bold, centrado, bordes.</summary>
        private static ICellStyle CrearEstiloHeaderAzul(XSSFWorkbook wb)
        {
            var s = wb.CreateCellStyle();
            var f = wb.CreateFont();
            f.FontName = "Calibri"; f.FontHeightInPoints = 12; f.IsBold = true;
            s.SetFont(f);
            ((XSSFCellStyle)s).SetFillForegroundColor(new XSSFColor(new SKColor(198, 224, 247)));
            s.FillPattern = FillPattern.SolidForeground;
            s.Alignment = HA.Center;
            s.VerticalAlignment = VA.Center;
            s.BorderTop = BorderStyle.Thin; s.BorderBottom = BorderStyle.Thin;
            s.BorderLeft = BorderStyle.Thin; s.BorderRight = BorderStyle.Thin;
            return s;
        }

        /// <summary>Fondo verde claro, texto oscuro, bold.</summary>
        private static ICellStyle CrearEstiloHeaderVerde(XSSFWorkbook wb)
        {
            var s = wb.CreateCellStyle();
            var f = wb.CreateFont();
            f.FontName = "Calibri"; f.FontHeightInPoints = 12; f.IsBold = true;
            s.SetFont(f);
            ((XSSFCellStyle)s).SetFillForegroundColor(new XSSFColor(new SKColor(198, 239, 206)));
            s.FillPattern = FillPattern.SolidForeground;
            s.Alignment = HA.Center;
            s.VerticalAlignment = VA.Center;
            s.BorderTop = BorderStyle.Thin; s.BorderBottom = BorderStyle.Thin;
            s.BorderLeft = BorderStyle.Thin; s.BorderRight = BorderStyle.Thin;
            return s;
        }

        /// <summary>Fondo gris claro, texto oscuro, bold.</summary>
        private static ICellStyle CrearEstiloHeaderGris(XSSFWorkbook wb)
        {
            var s = wb.CreateCellStyle();
            var f = wb.CreateFont();
            f.FontName = "Calibri"; f.FontHeightInPoints = 11; f.IsBold = true;
            s.SetFont(f);
            ((XSSFCellStyle)s).SetFillForegroundColor(new XSSFColor(new SKColor(233, 236, 239)));
            s.FillPattern = FillPattern.SolidForeground;
            s.BorderTop = BorderStyle.Thin; s.BorderBottom = BorderStyle.Thin;
            s.BorderLeft = BorderStyle.Thin; s.BorderRight = BorderStyle.Thin;
            return s;
        }

        /// <summary>Fondo amarillo claro, bold — para totales.</summary>
        private static ICellStyle CrearEstiloTotal(XSSFWorkbook wb)
        {
            var s = wb.CreateCellStyle();
            var f = wb.CreateFont();
            f.FontName = "Calibri"; f.FontHeightInPoints = 12; f.IsBold = true;
            s.SetFont(f);
            ((XSSFCellStyle)s).SetFillForegroundColor(new XSSFColor(new SKColor(255, 243, 205)));
            s.FillPattern = FillPattern.SolidForeground;
            s.DataFormat = wb.CreateDataFormat().GetFormat("$#,##0");
            s.BorderTop = BorderStyle.Thin; s.BorderBottom = BorderStyle.Thin;
            s.BorderLeft = BorderStyle.Thin; s.BorderRight = BorderStyle.Thin;
            return s;
        }

        /// <summary>Fondo gris muy claro para filas alternadas.</summary>
        private static ICellStyle CrearEstiloAlternado(XSSFWorkbook wb)
        {
            var s = wb.CreateCellStyle();
            ((XSSFCellStyle)s).SetFillForegroundColor(new XSSFColor(new SKColor(248, 249, 250)));
            s.FillPattern = FillPattern.SolidForeground;
            return s;
        }

        /// <summary>Bordes delgados + Calibri 11, para datos.</summary>
        private static ICellStyle CrearEstiloDato(XSSFWorkbook wb)
        {
            var s = wb.CreateCellStyle();
            var f = wb.CreateFont();
            f.FontName = "Calibri"; f.FontHeightInPoints = 11;
            s.SetFont(f);
            s.BorderTop = BorderStyle.Thin; s.BorderBottom = BorderStyle.Thin;
            s.BorderLeft = BorderStyle.Thin; s.BorderRight = BorderStyle.Thin;
            return s;
        }

        /// <summary>Dato con formato moneda $X.XXX.</summary>
        private static ICellStyle CrearEstiloMoneda(XSSFWorkbook wb)
        {
            var s = CrearEstiloDato(wb);
            s.DataFormat = wb.CreateDataFormat().GetFormat("$#,##0");
            return s;
        }

        /// <summary>Fondo verde claro + bold para ganancias positivas.</summary>
        private static ICellStyle CrearEstiloGananciaPositiva(XSSFWorkbook wb)
        {
            var s = CrearEstiloMoneda(wb);
            var f = wb.CreateFont();
            f.FontName = "Calibri"; f.FontHeightInPoints = 11; f.IsBold = true;
            ((XSSFFont)f).SetColor(new XSSFColor(new SKColor(21, 128, 61)));
            s.SetFont(f);
            ((XSSFCellStyle)s).SetFillForegroundColor(new XSSFColor(new SKColor(212, 237, 218)));
            s.FillPattern = FillPattern.SolidForeground;
            return s;
        }

        /// <summary>Fondo rojo claro + bold para ganancias negativas.</summary>
        private static ICellStyle CrearEstiloGananciaNegativa(XSSFWorkbook wb)
        {
            var s = CrearEstiloMoneda(wb);
            var f = wb.CreateFont();
            f.FontName = "Calibri"; f.FontHeightInPoints = 11; f.IsBold = true;
            ((XSSFFont)f).SetColor(new XSSFColor(new SKColor(192, 31, 42)));
            s.SetFont(f);
            ((XSSFCellStyle)s).SetFillForegroundColor(new XSSFColor(new SKColor(248, 215, 218)));
            s.FillPattern = FillPattern.SolidForeground;
            return s;
        }

        // ======================================================================
        // HOJA RESUMEN EJECUTIVO
        // ======================================================================

        private static void CrearHojaResumen(XSSFWorkbook workbook, BalanceExportDto data)
        {
            var ws = workbook.CreateSheet("Resumen Ejecutivo");

            ws.DefaultColumnWidth = 15;
            ws.SetColumnWidth(0, 28 * 256);
            ws.SetColumnWidth(1, 18 * 256);
            ws.SetColumnWidth(2, 18 * 256);
            ws.SetColumnWidth(3, 18 * 256);

            var stHeader = CrearEstiloHeaderAzul(workbook);
            var stDato = CrearEstiloDato(workbook);
            var stMoneda = CrearEstiloMoneda(workbook);
            var stAlternado = CrearEstiloAlternado(workbook);

            // TÍTULO PRINCIPAL
            var titleRow = ws.CreateRow(0);
            titleRow.HeightInPoints = 30;
            var titleCell = titleRow.CreateCell(0);
            titleCell.SetCellValue("REPORTE FINANCIERO - BALANCE GENERAL");
            titleCell.CellStyle = stHeader;
            for (int c = 1; c < 4; c++)
                titleRow.CreateCell(c).CellStyle = stHeader;
            ws.AddMergedRegion(new CellRangeAddress(0, 0, 0, 3));

            // Fecha de generación
            var dateRow = ws.CreateRow(1);
            var dateCell = dateRow.CreateCell(0);
            dateCell.SetCellValue($"Generado el: {DateTime.Now:dd/MM/yyyy HH:mm}");
            var dateStyle = workbook.CreateCellStyle();
            var dateFont = workbook.CreateFont();
            dateFont.FontName = "Calibri"; dateFont.FontHeightInPoints = 10; dateFont.IsItalic = true;
            ((XSSFFont)dateFont).SetColor(new XSSFColor(new SKColor(108, 117, 125)));
            dateStyle.SetFont(dateFont);
            dateCell.CellStyle = dateStyle;

            // MÉTRICAS CLAVE
            int filaActual = 3;

            var headerRow = ws.CreateRow(filaActual);
            string[] metricHeaders = { "METRICA CLAVE", "VALOR", "PARTICIPACION", "ESTADO" };
            for (int c = 0; c < 4; c++)
            {
                var cell = headerRow.CreateCell(c);
                cell.SetCellValue(metricHeaders[c]);
                cell.CellStyle = stHeader;
            }
            filaActual++;

            var ganancias = data.Ganancias;
            var ventas = data.TotalVentas;
            var gastos = data.TotalGastos;
            var encargos = data.TotalEncargos;
            var totalOperaciones = data.TotalVentas + Math.Abs(data.TotalGastos) + Math.Abs(data.TotalEncargos);

            var datosMetricas = new (string Label, decimal Valor, string Participacion, string Estado)[]
            {
                ("Ganancias Netas", ganancias, "", ganancias >= 0 ? "POSITIVO" : "NEGATIVO"),
                ("Ventas Totales", ventas, $"{(totalOperaciones > 0 ? ventas / totalOperaciones * 100 : 0):F1}%", "INGRESOS"),
                ("Gastos Totales", gastos, $"{(totalOperaciones > 0 ? Math.Abs(gastos) / totalOperaciones * 100 : 0):F1}%", "EGRESOS"),
                ("Encargos Pendientes", encargos, $"{(totalOperaciones > 0 ? Math.Abs(encargos) / totalOperaciones * 100 : 0):F1}%", "PENDIENTE")
            };

            var stGananciaPos = CrearEstiloGananciaPositiva(workbook);
            var stGananciaNeg = CrearEstiloGananciaNegativa(workbook);

            for (int i = 0; i < datosMetricas.Length; i++)
            {
                var (label, valor, participacion, estado) = datosMetricas[i];
                var row = ws.CreateRow(filaActual + i);

                row.CreateCell(0).SetCellValue(label);
                var cellValor = row.CreateCell(1);
                cellValor.SetCellValue((double)valor);
                row.CreateCell(2).SetCellValue(participacion);
                row.CreateCell(3).SetCellValue(estado);

                for (int c = 0; c < 4; c++)
                    row.GetCell(c).CellStyle = stDato;

                // Colorear ganancias primera fila
                if (i == 0)
                {
                    for (int c = 0; c < 4; c++)
                        row.GetCell(c).CellStyle = valor >= 0 ? stGananciaPos : stGananciaNeg;
                }

                // Formato moneda para columna valor
                if (i > 0) cellValor.CellStyle = stMoneda;

                // Alternar color de fila
                if (i % 2 == 1)
                {
                    for (int c = 0; c < 4; c++)
                    {
                        var existing = row.GetCell(c).CellStyle;
                        var alt = workbook.CreateCellStyle();
                        alt.CloneStyleFrom(existing);
                        alt.FillPattern = stAlternado.FillPattern;
                        try { ((XSSFCellStyle)alt).SetFillForegroundColor((XSSFColor)((XSSFCellStyle)stAlternado).FillForegroundColorColor); }
                        catch { }
                        row.GetCell(c).CellStyle = alt;
                    }
                }
            }

            // ANÁLISIS POR PERÍODO
            filaActual += 6;
            CrearSeccionAnalisisPeriodo(workbook, ws, data, filaActual);

            // Datos para gráfico PIE (columnas F-G)
            int filaGrafico = filaActual + 2;

            var totalVentasReal = data.Ventas.Sum(v => v.Precio * v.Cantidad);
            var totalGastosReal = data.Gastos.Sum(g => g.Monto * g.Cantidad);
            var totalEncargosReal = data.Encargos.Sum(e => e.Precio * e.Cantidad);

            EscribirCelda(ws, filaGrafico, 5, "Concepto", stHeader);
            EscribirCelda(ws, filaGrafico, 6, "Monto", stHeader);
            EscribirCelda(ws, filaGrafico + 1, 5, "Ventas", stDato);
            EscribirCelda(ws, filaGrafico + 1, 6, (double)totalVentasReal, stMoneda);
            EscribirCelda(ws, filaGrafico + 2, 5, "Gastos", stDato);
            EscribirCelda(ws, filaGrafico + 2, 6, (double)Math.Abs(totalGastosReal), stMoneda);
            EscribirCelda(ws, filaGrafico + 3, 5, "Encargos", stDato);
            EscribirCelda(ws, filaGrafico + 3, 6, (double)Math.Abs(totalEncargosReal), stMoneda);

            // Gráfico PIE nativo (sin ejes — pie chart no los necesita)
            var drawing = (XSSFDrawing)ws.CreateDrawingPatriarch();
            var anchor = drawing.CreateAnchor(0, 0, 0, 0, 4, 3, 18, 18);
            var chart = drawing.CreateChart(anchor);
            chart.GetOrAddLegend().Position = LegendPosition.Bottom;
            var catRange = new CellRangeAddress(filaGrafico + 1, filaGrafico + 3, 5, 5);
            var valRange = new CellRangeAddress(filaGrafico + 1, filaGrafico + 3, 6, 6);
            var catDS = XDDFDataSourcesFactory.FromStringCellRange(ws, catRange);
            var valDS = XDDFDataSourcesFactory.FromNumericCellRange(ws, valRange);
            var pieData = chart.CreateData<string, double>(ChartTypes.PIE, null, null);
            pieData.SetVaryColors(true);
            var pieSeries = pieData.AddSeries(catDS, valDS);
            pieSeries.SetTitle("Distribucion del Balance", null);
            chart.Plot(pieData);
        }

        // ======================================================================
        // SECCIÓN ANÁLISIS POR PERÍODO
        // ======================================================================

        private static void CrearSeccionAnalisisPeriodo(XSSFWorkbook workbook, ISheet ws, BalanceExportDto data, int filaInicio)
        {
            var stSeccion = CrearEstiloHeaderAzul(workbook);
            var stHeader = CrearEstiloHeaderVerde(workbook);
            var stDato = CrearEstiloDato(workbook);
            var stMoneda = CrearEstiloMoneda(workbook);
            var stTotal = CrearEstiloTotal(workbook);
            var stAlternado = CrearEstiloAlternado(workbook);

            // Título de sección
            var titleRow = ws.CreateRow(filaInicio);
            var titleCell = titleRow.CreateCell(0);
            titleCell.SetCellValue("ANALISIS POR PERIODO (ULTIMOS 6 MESES)");
            titleCell.CellStyle = stSeccion;
            for (int c = 1; c < 4; c++)
                titleRow.CreateCell(c).CellStyle = stSeccion;
            ws.AddMergedRegion(new CellRangeAddress(filaInicio, filaInicio, 0, 3));

            filaInicio += 2;

            var datosMensuales = GenerarDatosMensuales(data);

            // Encabezados
            var headerRow = ws.CreateRow(filaInicio);
            string[] headers = { "MES", "VENTAS", "GASTOS", "GANANCIA" };
            for (int c = 0; c < 4; c++)
            {
                var cell = headerRow.CreateCell(c);
                cell.SetCellValue(headers[c]);
                cell.CellStyle = stHeader;
            }
            filaInicio++;

            var stGananciaPos = CrearEstiloGananciaPositiva(workbook);
            var stGananciaNeg = CrearEstiloGananciaNegativa(workbook);

            for (int i = 0; i < datosMensuales.Count; i++)
            {
                var dato = datosMensuales[i];
                var row = ws.CreateRow(filaInicio + i);

                row.CreateCell(0).SetCellValue(dato.Mes);
                row.CreateCell(1).SetCellValue((double)dato.Ventas);
                row.GetCell(1).CellStyle = stMoneda;
                row.CreateCell(2).SetCellValue((double)dato.Gastos);
                row.GetCell(2).CellStyle = stMoneda;
                row.CreateCell(3).SetCellValue((double)dato.Ganancia);
                row.GetCell(3).CellStyle = dato.Ganancia >= 0 ? stGananciaPos : stGananciaNeg;

                for (int c = 0; c < 4; c++)
                {
                    var esBase = row.GetCell(c).CellStyle;
                    var merged = workbook.CreateCellStyle();
                    merged.CloneStyleFrom(esBase);
                    merged.BorderTop = stDato.BorderTop;
                    merged.BorderBottom = stDato.BorderBottom;
                    merged.BorderLeft = stDato.BorderLeft;
                    merged.BorderRight = stDato.BorderRight;
                    row.GetCell(c).CellStyle = merged;
                }

                // Alternar color de fila
                if (i % 2 == 1)
                {
                    for (int c = 0; c < 4; c++)
                    {
                        var existing = row.GetCell(c).CellStyle;
                        var alt = workbook.CreateCellStyle();
                        alt.CloneStyleFrom(existing);
                        alt.FillPattern = stAlternado.FillPattern;
                        try { ((XSSFCellStyle)alt).SetFillForegroundColor((XSSFColor)((XSSFCellStyle)stAlternado).FillForegroundColorColor); }
                        catch { }
                        row.GetCell(c).CellStyle = alt;
                    }
                }
            }

            // Totales
            filaInicio += datosMensuales.Count + 1;
            var totalVentas = datosMensuales.Sum(d => d.Ventas);
            var totalGastos = datosMensuales.Sum(d => d.Gastos);
            var totalGanancias = datosMensuales.Sum(d => d.Ganancia);

            var totalRow = ws.CreateRow(filaInicio);
            EscribirCelda(totalRow, 0, "TOTALES", stTotal);
            EscribirCelda(totalRow, 1, (double)totalVentas, stTotal);
            EscribirCelda(totalRow, 2, (double)totalGastos, stTotal);
            EscribirCelda(totalRow, 3, (double)totalGanancias, stTotal);
        }

        // ======================================================================
        // HOJAS DE DATOS DETALLADAS
        // ======================================================================

        private static void CrearHojaVentas(XSSFWorkbook workbook, List<Ventas> ventas)
        {
            var ws = workbook.CreateSheet("Ventas");
            CrearTablaDetallada(workbook, ws, "REGISTRO DE VENTAS",
                new[] { "FECHA", "DESCRIPCION", "CANTIDAD", "PRECIO UNIT.", "TOTAL" },
                ventas.Count,
                (i) => new object[]
                {
                    ventas[i].Fecha.ToString("dd/MM/yyyy"),
                    ventas[i].Descripcion ?? "N/A",
                    ventas[i].Cantidad,
                    (double)ventas[i].Precio,
                    (double)(ventas[i].Precio * ventas[i].Cantidad)
                },
                CrearEstiloHeaderVerde(workbook));
        }

        private static void CrearHojaGastos(XSSFWorkbook workbook, List<Gasto> gastos)
        {
            var ws = workbook.CreateSheet("Gastos");
            CrearTablaDetallada(workbook, ws, "REGISTRO DE GASTOS",
                new[] { "FECHA", "DESCRIPCION", "CANTIDAD", "MONTO UNIT.", "TOTAL" },
                gastos.Count,
                (i) => new object[]
                {
                    gastos[i].Fecha.ToString("dd/MM/yyyy"),
                    gastos[i].Descripcion ?? "N/A",
                    gastos[i].Cantidad,
                    (double)gastos[i].Monto,
                    (double)(gastos[i].Monto * gastos[i].Cantidad)
                },
                CrearEstiloHeaderGris(workbook));
        }

        private static void CrearHojaEncargos(XSSFWorkbook workbook, List<Encargo> encargos)
        {
            var ws = workbook.CreateSheet("Encargos");
            CrearTablaDetallada(workbook, ws, "REGISTRO DE ENCARGOS",
                new[] { "FECHA", "NOMBRE", "DESCRIPCION", "CANTIDAD", "PRECIO UNIT.", "TOTAL", "FECHA ENTREGA" },
                encargos.Count,
                (i) => new object[]
                {
                    encargos[i].Fecha.ToString("dd/MM/yyyy"),
                    encargos[i].Nombre ?? "N/A",
                    encargos[i].Descripcion ?? "N/A",
                    encargos[i].Cantidad,
                    (double)encargos[i].Precio,
                    (double)(encargos[i].Precio * encargos[i].Cantidad),
                    encargos[i].FechaEntrega.ToString("dd/MM/yyyy")
                },
                CrearEstiloHeaderGris(workbook));
        }

        private static void CrearTablaDetallada(XSSFWorkbook workbook, ISheet ws, string titulo,
            string[] encabezados, int cantidadDatos, Func<int, object[]> obtenerDatos,
            ICellStyle estiloHeaderColor)
        {
            var stHeaderGray = CrearEstiloHeaderGris(workbook);
            var stDato = CrearEstiloDato(workbook);
            var stMoneda = CrearEstiloMoneda(workbook);
            var stAlternado = CrearEstiloAlternado(workbook);

            for (int i = 0; i < encabezados.Length; i++)
                ws.SetColumnWidth(i, 16 * 256);

            // Título (fila 0)
            var titleRow = ws.CreateRow(0);
            titleRow.HeightInPoints = 28;
            var titleCell = titleRow.CreateCell(0);
            titleCell.SetCellValue(titulo);
            titleCell.CellStyle = estiloHeaderColor;
            for (int c = 1; c < encabezados.Length; c++)
                titleRow.CreateCell(c).CellStyle = estiloHeaderColor;
            ws.AddMergedRegion(new CellRangeAddress(0, 0, 0, encabezados.Length - 1));

            // Info de registros (fila 2)
            var infoRow = ws.CreateRow(2);
            var infoCell = infoRow.CreateCell(0);
            infoCell.SetCellValue($"Total de registros: {cantidadDatos}");
            var boldStyle = workbook.CreateCellStyle();
            var boldFont = workbook.CreateFont();
            boldFont.FontName = "Calibri"; boldFont.FontHeightInPoints = 11; boldFont.IsBold = true;
            boldStyle.SetFont(boldFont);
            infoCell.CellStyle = boldStyle;

            // Encabezados de tabla (fila 4)
            var headerRow = ws.CreateRow(4);
            for (int i = 0; i < encabezados.Length; i++)
            {
                var cell = headerRow.CreateCell(i);
                cell.SetCellValue(encabezados[i]);
                cell.CellStyle = stHeaderGray;
            }

            // Datos (desde fila 5)
            for (int i = 0; i < cantidadDatos; i++)
            {
                var row = ws.CreateRow(5 + i);
                var datos = obtenerDatos(i);

                for (int j = 0; j < datos.Length; j++)
                {
                    var cell = row.CreateCell(j);
                    SetCellValue(cell, datos[j]);

                    // Moneda para PRECIO, MONTO, TOTAL
                    bool isCurrency = encabezados[j].Contains("PRECIO") || encabezados[j].Contains("MONTO") || encabezados[j].Contains("TOTAL");
                    cell.CellStyle = isCurrency ? stMoneda : stDato;
                }

                // Alternar color de fila
                if (i % 2 == 1)
                {
                    for (int j = 0; j < encabezados.Length; j++)
                    {
                        var existing = row.GetCell(j).CellStyle;
                        var alt = workbook.CreateCellStyle();
                        alt.CloneStyleFrom(existing);
                        alt.FillPattern = stAlternado.FillPattern;
                        try { ((XSSFCellStyle)alt).SetFillForegroundColor((XSSFColor)((XSSFCellStyle)stAlternado).FillForegroundColorColor); }
                        catch { }
                        row.GetCell(j).CellStyle = alt;
                    }
                }
            }
        }

        // ======================================================================
        // HOJA GRÁFICO FINANCIERO
        // ======================================================================

        private static void CrearHojaGrafico(XSSFWorkbook workbook, BalanceExportDto data)
        {
            var ws = workbook.CreateSheet("Grafico Financiero");

            var stTitle = CrearEstiloHeaderAzul(workbook);
            var stHeader = CrearEstiloHeaderGris(workbook);
            var stDato = CrearEstiloDato(workbook);
            var stMoneda = CrearEstiloMoneda(workbook);
            var stAlternado = CrearEstiloAlternado(workbook);

            ws.SetColumnWidth(0, 15 * 256);
            ws.SetColumnWidth(1, 15 * 256);
            ws.SetColumnWidth(2, 15 * 256);
            ws.SetColumnWidth(3, 15 * 256);

            // Título
            var titleRow = ws.CreateRow(0);
            titleRow.HeightInPoints = 28;
            var titleCell = titleRow.CreateCell(0);
            titleCell.SetCellValue("EVOLUCION FINANCIERA - ULTIMOS 6 MESES");
            titleCell.CellStyle = stTitle;
            for (int c = 1; c < 4; c++)
                titleRow.CreateCell(c).CellStyle = stTitle;
            ws.AddMergedRegion(new CellRangeAddress(0, 0, 0, 3));

            var datosMensuales = GenerarDatosMensuales(data);

            // Encabezados (fila 2)
            var headerRow = ws.CreateRow(2);
            string[] headers = { "MES", "VENTAS", "GASTOS", "GANANCIAS" };
            for (int c = 0; c < 4; c++)
            {
                var cell = headerRow.CreateCell(c);
                cell.SetCellValue(headers[c]);
                cell.CellStyle = stHeader;
            }

            // Datos (desde fila 3)
            for (int i = 0; i < datosMensuales.Count; i++)
            {
                var dato = datosMensuales[i];
                var row = ws.CreateRow(3 + i);

                row.CreateCell(0).SetCellValue(dato.Mes);
                row.CreateCell(1).SetCellValue((double)dato.Ventas);
                row.GetCell(1).CellStyle = stMoneda;
                row.CreateCell(2).SetCellValue((double)Math.Abs(dato.Gastos));
                row.GetCell(2).CellStyle = stMoneda;
                row.CreateCell(3).SetCellValue((double)dato.Ganancia);
                row.GetCell(3).CellStyle = stMoneda;

                for (int c = 0; c < 4; c++)
                    row.GetCell(c).CellStyle = MergeBorder(workbook, row.GetCell(c).CellStyle, stDato);

                // Alternar color de fila
                if (i % 2 == 1)
                {
                    for (int c = 0; c < 4; c++)
                        row.GetCell(c).CellStyle = MergeFill(workbook, row.GetCell(c).CellStyle, stAlternado);
                }
            }

            // Gráfico de líneas nativo
            int dataStartRow = 3;
            int dataEndRow = 3 + datosMensuales.Count - 1;

            var drawing = (XSSFDrawing)ws.CreateDrawingPatriarch();
            var anchor = drawing.CreateAnchor(0, 0, 0, 0, 0, dataEndRow + 2, 10, dataEndRow + 20);
            var chart = drawing.CreateChart(anchor);
            chart.GetOrAddLegend().Position = LegendPosition.Bottom;
            var bottomAxis = chart.CreateCategoryAxis(AxisPosition.Bottom);
            bottomAxis.SetTitle("Mes");
            var leftAxis = chart.CreateValueAxis(AxisPosition.Left);
            leftAxis.SetTitle("Monto ($)");

            var mesRange = new CellRangeAddress(dataStartRow, dataEndRow, 0, 0);
            var ventasRange = new CellRangeAddress(dataStartRow, dataEndRow, 1, 1);
            var gastosRange = new CellRangeAddress(dataStartRow, dataEndRow, 2, 2);
            var gananciasRange = new CellRangeAddress(dataStartRow, dataEndRow, 3, 3);

            var mesDS = XDDFDataSourcesFactory.FromStringCellRange(ws, mesRange);
            var ventasDS = XDDFDataSourcesFactory.FromNumericCellRange(ws, ventasRange);
            var gastosDS = XDDFDataSourcesFactory.FromNumericCellRange(ws, gastosRange);
            var gananciasDS = XDDFDataSourcesFactory.FromNumericCellRange(ws, gananciasRange);

            var lineData = chart.CreateData<string, double>(ChartTypes.LINE, bottomAxis, leftAxis);
            lineData.SetVaryColors(true);
            var ventasSeries = lineData.AddSeries(mesDS, ventasDS);
            ventasSeries.SetTitle("Ventas", null);
            SetLineSeriesColor(ventasSeries, 41, 128, 185); // Azul
            var gastosSeries = lineData.AddSeries(mesDS, gastosDS);
            gastosSeries.SetTitle("Gastos", null);
            SetLineSeriesColor(gastosSeries, 231, 76, 60);  // Rojo
            var gananciasSeries = lineData.AddSeries(mesDS, gananciasDS);
            gananciasSeries.SetTitle("Ganancias", null);
            SetLineSeriesColor(gananciasSeries, 39, 174, 96); // Verde
            chart.Plot(lineData);
        }

        // ======================================================================
        // HELPERS: Escribir celdas
        // ======================================================================

        private static void EscribirCelda(ISheet sheet, int row, int col, string value, ICellStyle? style)
        {
            var r = sheet.GetRow(row) ?? sheet.CreateRow(row);
            var cell = r.CreateCell(col);
            cell.SetCellValue(value);
            if (style != null) cell.CellStyle = style;
        }

        private static void EscribirCelda(ISheet sheet, int row, int col, double value, ICellStyle? style)
        {
            var r = sheet.GetRow(row) ?? sheet.CreateRow(row);
            var cell = r.CreateCell(col);
            cell.SetCellValue(value);
            if (style != null) cell.CellStyle = style;
        }

        private static void EscribirCelda(IRow row, int col, string value, ICellStyle style)
        {
            var cell = row.CreateCell(col);
            cell.SetCellValue(value);
            if (style != null) cell.CellStyle = style;
        }

        private static void EscribirCelda(IRow row, int col, double value, ICellStyle style)
        {
            var cell = row.CreateCell(col);
            cell.SetCellValue(value);
            if (style != null) cell.CellStyle = style;
        }

        private static void SetCellValue(ICell cell, object value)
        {
            if (value == null) cell.SetCellValue("");
            else if (value is string s) cell.SetCellValue(s);
            else if (value is int i) cell.SetCellValue(i);
            else if (value is double dbl) cell.SetCellValue(dbl);
            else if (value is decimal dec) cell.SetCellValue((double)dec);
            else if (value is DateTime dt) cell.SetCellValue(dt.ToString("dd/MM/yyyy"));
            else cell.SetCellValue(value.ToString());
        }

        // ======================================================================
        // HELPERS: Estilos combinados (solo para casos que lo requieren)
        // ======================================================================

        private static ICellStyle MergeFill(XSSFWorkbook workbook, ICellStyle baseStyle, ICellStyle fillStyle)
        {
            if (baseStyle == null) return fillStyle;
            if (fillStyle == null) return baseStyle;

            var result = workbook.CreateCellStyle();
            result.CloneStyleFrom(baseStyle);

            try
            {
                if (fillStyle.FillPattern != FillPattern.NoFill)
                {
                    result.FillPattern = fillStyle.FillPattern;
                    ((XSSFCellStyle)result).SetFillForegroundColor((XSSFColor)((XSSFCellStyle)fillStyle).FillForegroundColorColor);
                }
            }
            catch { }

            return result;
        }

        private static ICellStyle MergeBorder(XSSFWorkbook workbook, ICellStyle baseStyle, ICellStyle borderStyle)
        {
            if (baseStyle == null) return borderStyle;
            if (borderStyle == null) return baseStyle;

            var result = workbook.CreateCellStyle();
            result.CloneStyleFrom(baseStyle);

            result.BorderTop = borderStyle.BorderTop;
            result.BorderBottom = borderStyle.BorderBottom;
            result.BorderLeft = borderStyle.BorderLeft;
            result.BorderRight = borderStyle.BorderRight;

            return result;
        }

        // ======================================================================
        // HELPER: Datos mensuales
        // ======================================================================

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
                    Mes = mes.ToString("MMM yyyy", new System.Globalization.CultureInfo("es-ES")),
                    Ventas = totalVentas,
                    Gastos = totalGastos,
                    Ganancia = ganancia
                };
            }).ToList();
        }

        private static void SetLineSeriesColor(XDDFChartData<string, double>.Series series, byte r, byte g, byte b)
        {
            try
            {
                var spPr = new XDDFShapeProperties();
                var ctSpPr = spPr.GetXmlObject();
                var ctLine = ctSpPr.AddNewLn();
                var ctSolidFill = ctLine.AddNewSolidFill();
                var ctSrgbClr = ctSolidFill.AddNewSrgbClr();
                ctSrgbClr.val = new byte[] { r, g, b };
                series.SetShapeProperties(spPr);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ExportExcel] Color failed: {ex.Message}");
            }
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
