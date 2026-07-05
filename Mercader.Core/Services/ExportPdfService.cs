using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;
using Syncfusion.Drawing;
using Mercader.Models;
using Mercader.Services.Interfaces;

namespace Mercader.Services;

public sealed class ExportPdfService : IExportPdfService
{
    public MemoryStream GenerarBalancePdf(BalanceExportDto data)
    {
        using var document = new PdfDocument();
        var page = document.Pages.Add();
        var graphics = page.Graphics;

        float margin = 50;
        float y = margin;

        // — Fonts —
        var titleFont = new PdfStandardFont(PdfFontFamily.Helvetica, 22, PdfFontStyle.Bold);
        var subtitleFont = new PdfStandardFont(PdfFontFamily.Helvetica, 12, PdfFontStyle.Regular);
        var headerFont = new PdfStandardFont(PdfFontFamily.Helvetica, 10, PdfFontStyle.Bold);
        var cellFont = new PdfStandardFont(PdfFontFamily.Helvetica, 10, PdfFontStyle.Regular);

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

        // Header
        var headerRow = grid.Headers.Add(1)[0];
        headerRow.Style.BackgroundBrush = new PdfSolidBrush(new PdfColor(41, 128, 185));
        headerRow.Style.TextBrush = PdfBrushes.White;
        headerRow.Style.Font = headerFont;
        headerRow.Cells[0].Value = "Metrica";
        headerRow.Cells[1].Value = "Valor";
        headerRow.Cells[0].StringFormat = new PdfStringFormat(PdfTextAlignment.Center, PdfVerticalAlignment.Middle);
        headerRow.Cells[1].StringFormat = new PdfStringFormat(PdfTextAlignment.Center, PdfVerticalAlignment.Middle);

        // Data rows
        AddRow(grid, "Total Ventas", $"$ {data.TotalVentas:N2}", cellFont);
        AddRow(grid, "Total Gastos", $"$ {data.TotalGastos:N2}", cellFont);
        AddRow(grid, "Total Encargos", $"$ {data.TotalEncargos:N2}", cellFont);
        AddRow(grid, "Ganancias", $"$ {data.Ganancias:N2}", cellFont);
        AddRow(grid, "Margen", $"{data.Margen:N2} %", cellFont);

        // Column widths
        grid.Columns[0].Width = usableWidth * 0.6f;
        grid.Columns[1].Width = usableWidth * 0.4f;

        // Grid style
        grid.Style.CellPadding = new PdfPaddings(6, 6, 4, 4);
        grid.Style.Font = cellFont;

        // Draw
        var gridResult = grid.Draw(page, new PointF(margin, y));
        y = gridResult.Bounds.Bottom + 20;

        // — Footer —
        graphics.DrawString(
            "Mercader App de Gestion",
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
