using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;
using Syncfusion.Drawing;
using Mercader.Models;
using Mercader.Services.Interfaces;

namespace Mercader.Services;

public sealed class ExportPdfService : IExportPdfService
{
    public MemoryStream GenerarBalancePdf(
        BalanceExportDto data,
        IReadOnlyList<(byte[] ImageBytes, string Title)>? charts = null,
        byte[]? fontBytes = null)
    {
        using var document = new PdfDocument();
        var page = document.Pages.Add();
        var graphics = page.Graphics;

        float margin = 50;
        float y = margin;

        // — Fonts —
        PdfFont titleFont, subtitleFont, headerFont, cellFont;

        if (fontBytes != null)
        {
            titleFont = new PdfTrueTypeFont(new MemoryStream(fontBytes), 22);
            subtitleFont = new PdfTrueTypeFont(new MemoryStream(fontBytes), 11);
            headerFont = new PdfTrueTypeFont(new MemoryStream(fontBytes), 10, PdfFontStyle.Bold);
            cellFont = new PdfTrueTypeFont(new MemoryStream(fontBytes), 10);
        }
        else
        {
            titleFont = new PdfStandardFont(PdfFontFamily.Helvetica, 22, PdfFontStyle.Bold);
            subtitleFont = new PdfStandardFont(PdfFontFamily.Helvetica, 11, PdfFontStyle.Regular);
            headerFont = new PdfStandardFont(PdfFontFamily.Helvetica, 10, PdfFontStyle.Bold);
            cellFont = new PdfStandardFont(PdfFontFamily.Helvetica, 10, PdfFontStyle.Regular);
        }

        float pageWidth = page.GetClientSize().Width;
        float usableWidth = pageWidth - margin * 2;

        // — Title —
        graphics.DrawString("Balance Financiero", titleFont, PdfBrushes.Black,
            new PointF(margin, y));
        y += 30;

        // — Period & date —
        string periodText = string.IsNullOrWhiteSpace(data.Periodo)
            ? $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}"
            : $"Periodo: {data.Periodo}  |  Generado: {DateTime.Now:dd/MM/yyyy HH:mm}";
        graphics.DrawString(periodText, subtitleFont, PdfBrushes.Gray,
            new PointF(margin, y));
        y += 25;

        // — Separator line —
        graphics.DrawLine(new PdfPen(PdfBrushes.DarkGray, 0.5f),
            margin, y, margin + usableWidth, y);
        y += 15;

        // — Table —
        var grid = new PdfGrid();
        grid.Columns.Add(2);

        var headerRow = grid.Headers.Add(1)[0];
        headerRow.Style.BackgroundBrush = new PdfSolidBrush(new PdfColor(41, 128, 185));
        headerRow.Style.TextBrush = PdfBrushes.White;
        headerRow.Style.Font = headerFont;
        headerRow.Cells[0].Value = "Métrica";
        headerRow.Cells[1].Value = "Valor";
        headerRow.Cells[0].StringFormat = new PdfStringFormat(PdfTextAlignment.Center, PdfVerticalAlignment.Middle);
        headerRow.Cells[1].StringFormat = new PdfStringFormat(PdfTextAlignment.Center, PdfVerticalAlignment.Middle);

        AddRow(grid, "Total Ventas", $"$ {data.TotalVentas:N2}", cellFont);
        AddRow(grid, "Total Gastos", $"$ {data.TotalGastos:N2}", cellFont);
        AddRow(grid, "Total Encargos", $"$ {data.TotalEncargos:N2}", cellFont);
        AddRow(grid, "Ganancias", $"$ {data.Ganancias:N2}", cellFont);
        AddRow(grid, "Margen", $"{data.Margen:N2} %", cellFont);

        grid.Columns[0].Width = usableWidth * 0.6f;
        grid.Columns[1].Width = usableWidth * 0.4f;

        grid.Style.CellPadding = new PdfPaddings(6, 6, 4, 4);
        grid.Style.Font = cellFont;

        var gridResult = grid.Draw(page, new PointF(margin, y));
        y = gridResult.Bounds.Bottom + 25;

        // — Charts (if provided) —
        if (charts != null)
        {
            foreach (var (imageBytes, chartTitle) in charts)
            {
                if (imageBytes == null || imageBytes.Length == 0)
                    continue;

                try
                {
                    // Check if we need a new page
                    float chartNeededHeight = 18 + 250 + 15; // title + maxImgHeight + spacing
                    if (y + chartNeededHeight > page.GetClientSize().Height - 40)
                    {
                        page = document.Pages.Add();
                        graphics = page.Graphics;
                        y = margin;
                    }

                    // Chart title
                    graphics.DrawString(chartTitle, subtitleFont, PdfBrushes.DarkSlateGray,
                        new PointF(margin, y));
                    y += 18;

                    using var imgStream = new MemoryStream(imageBytes);
                    var chartImg = new PdfBitmap(imgStream);

                    float imgMaxWidth = usableWidth;
                    float imgMaxHeight = 250f;
                    float imgWidth = chartImg.Width;
                    float imgHeight = chartImg.Height;

                    if (imgWidth > imgMaxWidth)
                    {
                        float ratio = imgMaxWidth / imgWidth;
                        imgWidth = imgMaxWidth;
                        imgHeight *= ratio;
                    }
                    if (imgHeight > imgMaxHeight)
                    {
                        float ratio = imgMaxHeight / imgHeight;
                        imgHeight = imgMaxHeight;
                        imgWidth *= ratio;
                    }

                    float imgX = margin + (usableWidth - imgWidth) / 2;
                    graphics.DrawImage(chartImg, new PointF(imgX, y), new SizeF(imgWidth, imgHeight));
                    y += imgHeight + 15;
                }
                catch
                {
                    // Chart image failed to render — skip it silently
                }
            }
        }

        // — Footer —
        graphics.DrawString(
            "Mercader App de Gestión",
            new PdfStandardFont(PdfFontFamily.Helvetica, 8, PdfFontStyle.Italic),
            PdfBrushes.Gray,
            new PointF(margin, page.GetClientSize().Height - 30));

        var stream = new MemoryStream();
        document.Save(stream);
        stream.Position = 0;
        return stream;
    }

    private static void AddRow(PdfGrid grid, string metrica, string valor, PdfFont font)
    {
        var row = grid.Rows.Add();
        row.Cells[0].Value = metrica;
        row.Cells[1].Value = valor;
        row.Cells[0].Style.Font = font;
        row.Cells[1].Style.Font = font;
        row.Cells[0].StringFormat = new PdfStringFormat(PdfTextAlignment.Left, PdfVerticalAlignment.Middle);
        row.Cells[1].StringFormat = new PdfStringFormat(PdfTextAlignment.Right, PdfVerticalAlignment.Middle);
    }
}
