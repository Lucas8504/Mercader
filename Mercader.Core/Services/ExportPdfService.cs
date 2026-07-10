using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;
using Syncfusion.Drawing;
using Mercader.Models;
using Mercader.Services.Interfaces;

namespace Mercader.Services;

public sealed class ExportPdfService : IExportPdfService
{
    // ── Professional color palette ──
    private static readonly PdfColor BluePrimary  = new(41, 128, 185);
    private static readonly PdfColor BlueDark     = new(31, 97, 141);
    private static readonly PdfColor BlueLight    = new(52, 152, 219);
    private static readonly PdfColor SectionBg    = new(245, 247, 250);
    private static readonly PdfColor BorderLight  = new(230, 232, 235);
    private static readonly PdfColor TextDark     = new(44, 62, 80);
    private static readonly PdfColor TextMuted    = new(160, 165, 170);
    private static readonly PdfColor GreenProfit  = new(39, 174, 96);

    private const float Margin       = 50f;
    private const float FooterHeight = 35f;

    public MemoryStream GenerarBalancePdf(
        BalanceExportDto data,
        IReadOnlyList<(byte[] ImageBytes, string Title)>? charts = null,
        byte[]? fontBytes = null)
    {
        using var document = new PdfDocument();
        var pages = new List<PdfPage>();

        // ── Fonts ──
        PdfFont titleFont, subtitleFont, sectionFont, headerFont, cellFont, smallFont;

        if (fontBytes != null)
        {
            titleFont    = new PdfTrueTypeFont(new MemoryStream(fontBytes), 22);
            subtitleFont = new PdfTrueTypeFont(new MemoryStream(fontBytes), 10);
            sectionFont  = new PdfTrueTypeFont(new MemoryStream(fontBytes), 12, PdfFontStyle.Bold);
            headerFont   = new PdfTrueTypeFont(new MemoryStream(fontBytes), 10, PdfFontStyle.Bold);
            cellFont     = new PdfTrueTypeFont(new MemoryStream(fontBytes), 10);
            smallFont    = new PdfTrueTypeFont(new MemoryStream(fontBytes), 8);
        }
        else
        {
            titleFont    = new PdfStandardFont(PdfFontFamily.Helvetica, 22, PdfFontStyle.Bold);
            subtitleFont = new PdfStandardFont(PdfFontFamily.Helvetica, 10, PdfFontStyle.Regular);
            sectionFont  = new PdfStandardFont(PdfFontFamily.Helvetica, 12, PdfFontStyle.Bold);
            headerFont   = new PdfStandardFont(PdfFontFamily.Helvetica, 10, PdfFontStyle.Bold);
            cellFont     = new PdfStandardFont(PdfFontFamily.Helvetica, 10, PdfFontStyle.Regular);
            smallFont    = new PdfStandardFont(PdfFontFamily.Helvetica, 8, PdfFontStyle.Regular);
        }

        // ── First page ──
        var page = document.Pages.Add();
        pages.Add(page);
        var graphics = page.Graphics;
        float pageWidth = page.GetClientSize().Width;
        float usableWidth = pageWidth - Margin * 2;
        float y = 0;

        // ════════════════════════════════════════════
        //  HEADER BAND  (full‑width blue banner)
        // ════════════════════════════════════════════
        graphics.DrawRectangle(new PdfSolidBrush(BluePrimary),
            new RectangleF(0, 0, pageWidth, 72));

        graphics.DrawString("Balance Financiero", titleFont, PdfBrushes.White,
            new PointF(Margin, 16));

        string periodText = string.IsNullOrWhiteSpace(data.Periodo)
            ? $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}"
            : $"Periodo: {data.Periodo}  |  Generado: {DateTime.Now:dd/MM/yyyy HH:mm}";
        graphics.DrawString(periodText, subtitleFont, PdfBrushes.White,
            new PointF(Margin, 46));

        y = 92;

        // ════════════════════════════════════════════
        //  SECTION — RESUMEN FINANCIERO
        // ════════════════════════════════════════════
        y = DrawSectionHeader(graphics, "Resumen Financiero", sectionFont,
            Margin, usableWidth, y);

        // ── Financial table ──
        var grid = new PdfGrid();
        grid.Columns.Add(2);

        //  Header row  — blue background, white text
        var headerRow = grid.Headers.Add(1)[0];
        headerRow.Style.BackgroundBrush = new PdfSolidBrush(BluePrimary);
        headerRow.Style.TextBrush = PdfBrushes.White;
        headerRow.Style.Font = headerFont;
        headerRow.Cells[0].Value = "Métrica";
        headerRow.Cells[1].Value = "Valor";
        headerRow.Cells[0].StringFormat = new PdfStringFormat(
            PdfTextAlignment.Left, PdfVerticalAlignment.Middle);
        headerRow.Cells[1].StringFormat = new PdfStringFormat(
            PdfTextAlignment.Right, PdfVerticalAlignment.Middle);

        //  Data rows
        AddDataRow(grid, "Total Ventas",   $"$ {data.TotalVentas:N2}", cellFont);
        AddDataRow(grid, "Total Gastos",   $"$ {data.TotalGastos:N2}", cellFont);
        AddDataRow(grid, "Total Encargos", $"$ {data.TotalEncargos:N2}", cellFont);

        //  Ganancias — highlighted row (bold + green)
        var ganRow = grid.Rows.Add();
        ganRow.Cells[0].Value = "Ganancias";
        ganRow.Cells[1].Value = $"$ {data.Ganancias:N2}";
        ganRow.Cells[0].Style.Font = headerFont;
        ganRow.Cells[1].Style.Font = headerFont;
        ganRow.Cells[1].Style.TextBrush = new PdfSolidBrush(GreenProfit);
        ganRow.Cells[0].StringFormat = new PdfStringFormat(
            PdfTextAlignment.Left, PdfVerticalAlignment.Middle);
        ganRow.Cells[1].StringFormat = new PdfStringFormat(
            PdfTextAlignment.Right, PdfVerticalAlignment.Middle);

        AddDataRow(grid, "Margen", $"{data.Margen:N2} %", cellFont);

        //  Column widths & padding
        grid.Columns[0].Width = usableWidth * 0.6f;
        grid.Columns[1].Width = usableWidth * 0.4f;
        grid.Style.CellPadding = new PdfPaddings(8, 8, 5, 5);
        grid.Style.Font = cellFont;

        var gridResult = grid.Draw(page, new PointF(Margin, y));
        y = gridResult.Bounds.Bottom + 22;

        // ════════════════════════════════════════════
        //  CHARTS
        // ════════════════════════════════════════════
        if (charts != null)
        {
            foreach (var (imageBytes, chartTitle) in charts)
            {
                if (imageBytes is null || imageBytes.Length == 0)
                    continue;

                try
                {
                    float chartNeededHeight = 36 + 320 + 15;
                    if (y + chartNeededHeight > page.GetClientSize().Height - FooterHeight)
                    {
                        page = document.Pages.Add();
                        pages.Add(page);
                        graphics = page.Graphics;
                        y = Margin;
                    }

                    // Section header for this chart
                    y = DrawSectionHeader(graphics, chartTitle, sectionFont,
                        Margin, usableWidth, y);

                    using var imgStream = new MemoryStream(imageBytes);
                    var chartImg = new PdfBitmap(imgStream);

                    float imgWidth  = chartImg.Width;
                    float imgHeight = chartImg.Height;

                    if (imgWidth > usableWidth)
                    {
                        float ratio = usableWidth / imgWidth;
                        imgWidth  = usableWidth;
                        imgHeight *= ratio;
                    }

                    if (imgHeight > 320f)
                    {
                        float ratio = 320f / imgHeight;
                        imgHeight = 250f;
                        imgWidth  *= ratio;
                    }

                    float imgX = Margin + (usableWidth - imgWidth) / 2;
                    graphics.DrawImage(chartImg,
                        new PointF(imgX, y),
                        new SizeF(imgWidth, imgHeight));

                    y += imgHeight + 15;
                }
                catch
                {
                    // Chart failed to render — skip silently
                }
            }
        }

        // ════════════════════════════════════════════
        //  FOOTER  (on every page — drawn after all
        //  pages are created so «Page X of Y» is
        //  accurate)
        // ════════════════════════════════════════════
        var footerBrush = new PdfSolidBrush(TextMuted);
        var footerPen   = new PdfPen(BorderLight, 0.5f);
        string dateText = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

        for (int i = 0; i < pages.Count; i++)
        {
            var p  = pages[i];
            float pw = p.GetClientSize().Width;
            float ph = p.GetClientSize().Height;
            float fy = ph - FooterHeight;

            // Thin separator line at the top of the footer area
            p.Graphics.DrawLine(footerPen, 0, fy + 5, pw, fy + 5);

            // Page X of Y  (left side)
            p.Graphics.DrawString(
                $"Página {i + 1} de {pages.Count}",
                smallFont, footerBrush,
                new PointF(0, fy + 12));

            // Generation timestamp  (right side)
            p.Graphics.DrawString(
                dateText,
                smallFont, footerBrush,
                new PointF(pw - 90, fy + 12));
        }

        // ── Persist ──
        var stream = new MemoryStream();
        document.Save(stream);
        stream.Position = 0;
        return stream;
    }

    // ══════════════════════════════════════════════════════
    //  Helpers
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// Draws a section header: light‑gray background, left accent bar, title.
    /// Returns the Y position right after the section header.
    /// </summary>
    private static float DrawSectionHeader(
        PdfGraphics graphics, string title, PdfFont font,
        float margin, float usableWidth, float y)
    {
        // Light background
        graphics.DrawRectangle(new PdfSolidBrush(SectionBg),
            new RectangleF(margin, y, usableWidth, 30));

        // Left accent bar  (thin blue rectangle)
        graphics.DrawRectangle(new PdfSolidBrush(BluePrimary),
            new RectangleF(margin, y, 4, 30));

        // Title text
        graphics.DrawString(title, font, new PdfSolidBrush(TextDark),
            new PointF(margin + 14, y + 6));

        return y + 36;   // header height + small gap
    }

    /// <summary>
    /// Adds a two‑column data row to the grid.
    /// </summary>
    private static void AddDataRow(PdfGrid grid, string metrica, string valor, PdfFont font)
    {
        var row = grid.Rows.Add();
        row.Cells[0].Value = metrica;
        row.Cells[1].Value = valor;
        row.Cells[0].Style.Font = font;
        row.Cells[1].Style.Font = font;
        row.Cells[0].StringFormat = new PdfStringFormat(
            PdfTextAlignment.Left, PdfVerticalAlignment.Middle);
        row.Cells[1].StringFormat = new PdfStringFormat(
            PdfTextAlignment.Right, PdfVerticalAlignment.Middle);
    }
}
