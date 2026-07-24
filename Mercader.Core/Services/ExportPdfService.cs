using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;
using Syncfusion.Drawing;
using Mercader.Domain.Entities;
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

        // Track every page (including auto-pagination from tables) for the footer
        document.Pages.PageAdded += (_, args) => pages.Add(args.Page);

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
        //  DETAIL SECTIONS — Ventas, Gastos y
        //  Encargos del mes actual y anterior
        // ════════════════════════════════════════════
        var currentMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var prevMonthStart = currentMonthStart.AddMonths(-1);

        string[] detHeaders = ["Fecha", "Descripción", "Cant.", "Total"];
        float[] detWidths  = [0.12f, 0.44f, 0.13f, 0.31f];

        // — Ventas —
        var vtasCurrent = data.Ventas
            .Where(v => !v.IsDeleted && v.Fecha >= currentMonthStart && v.Fecha < currentMonthStart.AddMonths(1))
            .ToList();
        var vtasPrev = data.Ventas
            .Where(v => !v.IsDeleted && v.Fecha >= prevMonthStart && v.Fecha < currentMonthStart)
            .ToList();

        // Section header always visible
        if (y + 36 > page.GetClientSize().Height - FooterHeight)
        {
            page = document.Pages.Add();
            graphics = page.Graphics;
            y = Margin;
        }
        y = DrawSectionHeader(graphics, "Detalle de Ventas", sectionFont,
            Margin, usableWidth, y);

        if (vtasCurrent.Count > 0)
        {
            var (vtasRows, vtasTotal) = ExpandVentasConArticulos(vtasCurrent, data.ArticulosVenta);
            y = RenderMonthTable(ref page, ref graphics, y,
                Capitalize(currentMonthStart.ToString("MMMM yyyy")),
                vtasRows, vtasTotal,
                detHeaders, detWidths, cellFont, headerFont,
                Margin, usableWidth, document, FooterHeight);
        }
        else
            y = DrawNoActivityMessage(graphics, y, cellFont, Margin, usableWidth);

        if (vtasPrev.Count > 0)
        {
            if (y + 20 > page.GetClientSize().Height - FooterHeight)
            {
                page = document.Pages.Add();
                graphics = page.Graphics;
                y = Margin;
            }
            var (vtasPrevRows, vtasPrevTotal) = ExpandVentasConArticulos(vtasPrev, data.ArticulosVenta);
            y = RenderMonthTable(ref page, ref graphics, y,
                Capitalize(prevMonthStart.ToString("MMMM yyyy")),
                vtasPrevRows, vtasPrevTotal,
                detHeaders, detWidths, cellFont, headerFont,
                Margin, usableWidth, document, FooterHeight);
        }

        // — Gastos —
        var gtosCurrent = data.Gastos
            .Where(g => !g.IsDeleted && g.Fecha >= currentMonthStart && g.Fecha < currentMonthStart.AddMonths(1))
            .ToList();
        var gtosPrev = data.Gastos
            .Where(g => !g.IsDeleted && g.Fecha >= prevMonthStart && g.Fecha < currentMonthStart)
            .ToList();

        // Section header always visible
        if (y + 36 > page.GetClientSize().Height - FooterHeight)
        {
            page = document.Pages.Add();
            graphics = page.Graphics;
            y = Margin;
        }
        y = DrawSectionHeader(graphics, "Detalle de Gastos", sectionFont,
            Margin, usableWidth, y);

        if (gtosCurrent.Count > 0)
        {
            var (gtosRows, gtosTotal) = ExpandGastosConArticulos(gtosCurrent, data.ArticulosGasto);
            y = RenderMonthTable(ref page, ref graphics, y,
                Capitalize(currentMonthStart.ToString("MMMM yyyy")),
                gtosRows, gtosTotal,
                detHeaders, detWidths, cellFont, headerFont,
                Margin, usableWidth, document, FooterHeight);
        }
        else
            y = DrawNoActivityMessage(graphics, y, cellFont, Margin, usableWidth);

        if (gtosPrev.Count > 0)
        {
            if (y + 20 > page.GetClientSize().Height - FooterHeight)
            {
                page = document.Pages.Add();
                graphics = page.Graphics;
                y = Margin;
            }
            var (gtosPrevRows, gtosPrevTotal) = ExpandGastosConArticulos(gtosPrev, data.ArticulosGasto);
            y = RenderMonthTable(ref page, ref graphics, y,
                Capitalize(prevMonthStart.ToString("MMMM yyyy")),
                gtosPrevRows, gtosPrevTotal,
                detHeaders, detWidths, cellFont, headerFont,
                Margin, usableWidth, document, FooterHeight);
        }

        // — Encargos —
        string[] encHeaders = ["Entrega", "Cliente", "Descripción", "Total"];
        float[] encWidths   = [0.12f, 0.30f, 0.27f, 0.31f];

        var encCurrent = data.Encargos
            .Where(e => !e.IsDeleted && e.Fecha >= currentMonthStart && e.Fecha < currentMonthStart.AddMonths(1))
            .ToList();
        var encPrev = data.Encargos
            .Where(e => !e.IsDeleted && e.Fecha >= prevMonthStart && e.Fecha < currentMonthStart)
            .ToList();

        // Section header always visible
        if (y + 36 > page.GetClientSize().Height - FooterHeight)
        {
            page = document.Pages.Add();
            graphics = page.Graphics;
            y = Margin;
        }
        y = DrawSectionHeader(graphics, "Detalle de Encargos", sectionFont,
            Margin, usableWidth, y);

        if (encCurrent.Count > 0)
        {
            var (encRows, encTotal) = ExpandEncargosConArticulos(encCurrent, data.ArticulosEncargo);
            y = RenderMonthTable(ref page, ref graphics, y,
                Capitalize(currentMonthStart.ToString("MMMM yyyy")),
                encRows, encTotal,
                encHeaders, encWidths, cellFont, headerFont,
                Margin, usableWidth, document, FooterHeight);
        }
        else
            y = DrawNoActivityMessage(graphics, y, cellFont, Margin, usableWidth);

        if (encPrev.Count > 0)
        {
            if (y + 20 > page.GetClientSize().Height - FooterHeight)
            {
                page = document.Pages.Add();
                graphics = page.Graphics;
                y = Margin;
            }
            var (encPrevRows, encPrevTotal) = ExpandEncargosConArticulos(encPrev, data.ArticulosEncargo);
            y = RenderMonthTable(ref page, ref graphics, y,
                Capitalize(prevMonthStart.ToString("MMMM yyyy")),
                encPrevRows, encPrevTotal,
                encHeaders, encWidths, cellFont, headerFont,
                Margin, usableWidth, document, FooterHeight);
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

    // ══════════════════════════════════════════════════════
    //  Detail table helpers
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// Draws a "no activity" placeholder message for an empty month.
    /// </summary>
    private static float DrawNoActivityMessage(
        PdfGraphics graphics, float y, PdfFont font,
        float margin, float usableWidth)
    {
        graphics.DrawString("No se registró actividad en este mes a la fecha.",
            font, new PdfSolidBrush(TextMuted),
            new RectangleF(margin, y, usableWidth, 20),
            new PdfStringFormat(PdfTextAlignment.Left, PdfVerticalAlignment.Middle));
        return y + 22;
    }

    /// <summary>
    /// Renders a month sub-header, a detail table, and a total row.
    /// Returns the Y position after the table.
    /// </summary>
    private static float RenderMonthTable(
        ref PdfPage page, ref PdfGraphics graphics, float currentY,
        string monthLabel,
        string[][] rows, decimal total,
        string[] headers, float[] widths,
        PdfFont cellFont, PdfFont headerFont,
        float margin, float usableWidth,
        PdfDocument document, float footerHeight)
    {
        float y = currentY;

        // Estimate height: sub-header(22) + header(22) + rows(18×N) + total(22) + gap(8)
        float estimated = 22 + 22 + rows.Length * 18 + 22 + 8;
        if (y + estimated > page.GetClientSize().Height - footerHeight)
        {
            page = document.Pages.Add();
            graphics = page.Graphics;
            y = margin;
        }

        // Month sub-header
        float shY = y;
        graphics.DrawRectangle(new PdfSolidBrush(new PdfColor(240, 243, 248)),
            new RectangleF(margin, shY, usableWidth, 22));
        graphics.DrawString(monthLabel, headerFont, new PdfSolidBrush(TextDark),
            new PointF(margin + 8, shY + 3));
        y = shY + 26;

        // Build & draw the grid
        var grid = BuildDetailGrid(headers, widths, rows, total, headerFont, cellFont, usableWidth);
        var result = grid.Draw(page, new PointF(margin, y));
        return result.Bounds.Bottom + 8;
    }

    /// <summary>
    /// Creates a PdfGrid with headers, data rows, and a bold total row.
    /// </summary>
    private static PdfGrid BuildDetailGrid(
        string[] headers, float[] widths,
        string[][] rows, decimal total,
        PdfFont headerFont, PdfFont cellFont,
        float usableWidth)
    {
        var grid = new PdfGrid();
        int colCount = headers.Length;
        grid.Columns.Add(colCount);

        // ── Header row ──
        var h = grid.Headers.Add(1)[0];
        h.Style.BackgroundBrush = new PdfSolidBrush(BluePrimary);
        h.Style.TextBrush = PdfBrushes.White;
        h.Style.Font = headerFont;

        for (int i = 0; i < colCount; i++)
        {
            h.Cells[i].Value = headers[i];
            bool isLast = i == colCount - 1;
            h.Cells[i].StringFormat = new PdfStringFormat(
                isLast ? PdfTextAlignment.Right : PdfTextAlignment.Left,
                PdfVerticalAlignment.Middle);
        }

        // ── Data rows ──
        for (int r = 0; r < rows.Length; r++)
        {
            var row = grid.Rows.Add();
            for (int c = 0; c < colCount; c++)
            {
                row.Cells[c].Value = rows[r][c];
                row.Cells[c].Style.Font = cellFont;
                bool isLast = c == colCount - 1;
                row.Cells[c].StringFormat = new PdfStringFormat(
                    isLast ? PdfTextAlignment.Right : PdfTextAlignment.Left,
                    PdfVerticalAlignment.Middle);
            }
        }

        // ── Total row ──
        var t = grid.Rows.Add();
        for (int c = 0; c < colCount; c++)
        {
            t.Cells[c].Style.Font = headerFont;
            t.Cells[c].StringFormat = new PdfStringFormat(
                c == 0 ? PdfTextAlignment.Left :
                c == colCount - 1 ? PdfTextAlignment.Right :
                PdfTextAlignment.Center,
                PdfVerticalAlignment.Middle);
        }
        t.Cells[0].Value = "TOTAL";
        t.Cells[colCount - 1].Value = $"$ {total:N2}";

        // ── Column widths ──
        float totalWidthRatio = widths.Sum();
        for (int i = 0; i < colCount; i++)
            grid.Columns[i].Width = usableWidth * widths[i] / totalWidthRatio;

        grid.Style.CellPadding = new PdfPaddings(5, 5, 3, 3);
        grid.Style.Font = cellFont;

        return grid;
    }

    /// <summary>
    /// Capitalizes the first letter of a string.
    /// </summary>
    private static string Capitalize(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return char.ToUpper(value[0]) + value[1..];
    }

    // ══════════════════════════════════════════════════════
    //  Article-expansion helpers (multi-artículo)
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// Expands Ventas entities into article-level rows.
    /// When articles exist, each article becomes a row with the parent's date.
    /// Falls back to the entity row for legacy data without articles.
    /// </summary>
    private static (string[][] Rows, decimal Total) ExpandVentasConArticulos(
        List<Ventas> entities, IReadOnlyDictionary<int, List<ArticuloVenta>> articles)
    {
        var rows = new List<string[]>();
        decimal total = 0;
        foreach (var v in entities)
        {
            if (articles.TryGetValue(v.Id, out var arts) && arts.Count > 0)
            {
                foreach (var a in arts.OrderBy(a => a.Orden))
                {
                    rows.Add(new[] { v.Fecha.ToString("dd/MM"), a.Descripcion ?? "", a.Cantidad.ToString("N0"), $"$ {a.Total:N2}" });
                    total += a.Total;
                }
            }
            else
            {
                rows.Add(new[] { v.Fecha.ToString("dd/MM"), v.Descripcion ?? "", v.Cantidad.ToString("N0"), $"$ {v.Total:N2}" });
                total += v.Total;
            }
        }
        return (rows.ToArray(), total);
    }

    /// <summary>
    /// Expands Gasto entities into article-level rows.
    /// </summary>
    private static (string[][] Rows, decimal Total) ExpandGastosConArticulos(
        List<Gasto> entities, IReadOnlyDictionary<int, List<ArticuloGasto>> articles)
    {
        var rows = new List<string[]>();
        decimal total = 0;
        foreach (var g in entities)
        {
            if (articles.TryGetValue(g.Id, out var arts) && arts.Count > 0)
            {
                foreach (var a in arts.OrderBy(a => a.Orden))
                {
                    rows.Add(new[] { g.Fecha.ToString("dd/MM"), a.Descripcion ?? "", a.Cantidad.ToString("N0"), $"$ {a.Total:N2}" });
                    total += a.Total;
                }
            }
            else
            {
                rows.Add(new[] { g.Fecha.ToString("dd/MM"), g.Descripcion ?? "", g.Cantidad.ToString("N0"), $"$ {g.Total:N2}" });
                total += g.Total;
            }
        }
        return (rows.ToArray(), total);
    }

    /// <summary>
    /// Expands Encargo entities into article-level rows (4 columns: Entrega, Cliente, Descripción, Total).
    /// </summary>
    private static (string[][] Rows, decimal Total) ExpandEncargosConArticulos(
        List<Encargo> entities, IReadOnlyDictionary<int, List<ArticuloEncargo>> articles)
    {
        var rows = new List<string[]>();
        decimal total = 0;
        foreach (var e in entities)
        {
            if (articles.TryGetValue(e.Id, out var arts) && arts.Count > 0)
            {
                foreach (var a in arts.OrderBy(a => a.Orden))
                {
                    rows.Add(new[] { e.FechaEntrega.ToString("dd/MM"), e.Nombre ?? "", a.Descripcion ?? "", $"$ {a.Total:N2}" });
                    total += a.Total;
                }
            }
            else
            {
                rows.Add(new[] { e.FechaEntrega.ToString("dd/MM"), e.Nombre ?? "", e.Descripcion ?? "", $"$ {e.Total:N2}" });
                total += e.Total;
            }
        }
        return (rows.ToArray(), total);
    }
}
