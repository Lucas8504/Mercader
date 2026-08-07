using PdfSharp.Pdf;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using Mercader.Domain.Entities;
using Mercader.Models;
using Mercader.Services.Interfaces;

namespace Mercader.Services;

/// <summary>
/// Font resolver that provides embedded font bytes to PDFsharp.
/// Required because PDFsharp on Android/iOS cannot access system fonts.
/// </summary>
internal sealed class PdfSharpFontResolver : IFontResolver
{
    private readonly byte[] _fontBytes;
    private readonly string _familyName;

    public PdfSharpFontResolver(byte[] fontBytes, string familyName = "OpenSans")
    {
        _fontBytes = fontBytes;
        _familyName = familyName;
    }

    public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        return new FontResolverInfo(_familyName, isBold, isItalic);
    }

    public byte[]? GetFont(string faceName)
    {
        return _fontBytes;
    }
}

public sealed class ExportPdfService : IExportPdfService
{
    private static readonly XColor BluePrimary  = XColor.FromArgb(255, 41, 128, 185);
    private static readonly XColor BlueDark     = XColor.FromArgb(255, 31, 97, 141);
    private static readonly XColor BlueLight    = XColor.FromArgb(255, 52, 152, 219);
    private static readonly XColor SectionBg    = XColor.FromArgb(255, 245, 247, 250);
    private static readonly XColor BorderLight  = XColor.FromArgb(255, 230, 232, 235);
    private static readonly XColor TextDark     = XColor.FromArgb(255, 44, 62, 80);
    private static readonly XColor TextMuted    = XColor.FromArgb(255, 160, 165, 170);
    private static readonly XColor GreenProfit  = XColor.FromArgb(255, 39, 174, 96);
    private static readonly XColor MonthBg      = XColor.FromArgb(255, 240, 243, 248);

    private const float Margin       = 50f;
    private const float FooterHeight = 35f;

    public MemoryStream GenerarBalancePdf(
        BalanceExportDto data,
        IReadOnlyList<(byte[] ImageBytes, string Title)>? charts = null,
        byte[]? fontBytes = null)
    {
        using var document = new PdfDocument();

        // ── Font resolver ──
        string fontFamily = "OpenSans";
        if (fontBytes != null)
        {
            GlobalFontSettings.FontResolver = new PdfSharpFontResolver(fontBytes, fontFamily);
        }
        else
        {
            fontFamily = "Arial";
        }

        XFont titleFont    = new(fontFamily, 22, XFontStyleEx.Bold);
        XFont subtitleFont = new(fontFamily, 10, XFontStyleEx.Regular);
        XFont sectionFont  = new(fontFamily, 12, XFontStyleEx.Bold);
        XFont headerFont   = new(fontFamily, 10, XFontStyleEx.Bold);
        XFont cellFont     = new(fontFamily, 10, XFontStyleEx.Regular);
        XFont smallFont    = new(fontFamily, 8,  XFontStyleEx.Regular);

        string dateText = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

        // ── Page 1 ──
        int pageNum = 1;
        var page = document.AddPage();
        var gfx = XGraphics.FromPdfPage(page);
        float pageWidth  = (float)page.Width.Point;
        float usableWidth = pageWidth - Margin * 2;
        float y = 0;

        // ── HEADER BAND ──
        gfx.DrawRectangle(new XSolidBrush(BluePrimary), 0, 0, pageWidth, 72);
        gfx.DrawString("Balance Financiero", titleFont, XBrushes.White,
            new XPoint(Margin, 16));

        string periodText = string.IsNullOrWhiteSpace(data.Periodo)
            ? $"Generado: {dateText}"
            : $"Periodo: {data.Periodo}  |  Generado: {dateText}";
        gfx.DrawString(periodText, subtitleFont, PdfBrushesWhite(),
            new XPoint(Margin, 46));

        y = 92;

        // ── RESUMEN FINANCIERO ──
        y = DrawSectionHeader(gfx, "Resumen Financiero", sectionFont,
            Margin, usableWidth, y);

        y = DrawFinancialSummaryTable(gfx, data, headerFont, cellFont,
            Margin, usableWidth, y);

        // ── CHARTS ──
        if (charts != null)
        {
            foreach (var (imageBytes, chartTitle) in charts)
            {
                if (imageBytes is null || imageBytes.Length == 0)
                    continue;

                try
                {
                    float chartNeededHeight = 36 + 320 + 15;
                    if (y + chartNeededHeight > (float)page.Height.Point - FooterHeight)
                    {
                        DrawFooter(gfx, page, pageNum, smallFont, dateText);
                        pageNum++;
                        page = document.AddPage();
                        gfx.Dispose();
                        gfx = XGraphics.FromPdfPage(page);
                        pageWidth = (float)page.Width.Point;
                        usableWidth = pageWidth - Margin * 2;
                        y = Margin;
                    }

                    y = DrawSectionHeader(gfx, chartTitle, sectionFont,
                        Margin, usableWidth, y);

                    using var imgStream = new MemoryStream(imageBytes);
                    var chartImg = XImage.FromStream(imgStream);

                    float imgWidth  = (float)chartImg.PixelWidth * 72f / 96f;
                    float imgHeight = (float)chartImg.PixelHeight * 72f / 96f;

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
                    gfx.DrawImage(chartImg, imgX, y, imgWidth, imgHeight);

                    y += imgHeight + 15;
                }
                catch
                {
                    // Chart failed to render — skip silently
                }
            }
        }

        // ── DETAIL SECTIONS ──
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

        if (y + 36 > (float)page.Height.Point - FooterHeight)
        {
            DrawFooter(gfx, page, pageNum, smallFont, dateText);
            pageNum++;
            gfx.Dispose();
            page = document.AddPage();
            gfx = XGraphics.FromPdfPage(page);
            pageWidth = (float)page.Width.Point;
            usableWidth = pageWidth - Margin * 2;
            y = Margin;
        }
        y = DrawSectionHeader(gfx, "Detalle de Ventas", sectionFont,
            Margin, usableWidth, y);

        if (vtasCurrent.Count > 0)
        {
            var (vtasRows, vtasTotal) = ExpandVentasConArticulos(vtasCurrent, data.ArticulosVenta);
            y = RenderMonthTable(ref page, ref pageNum, ref gfx, ref pageWidth, ref usableWidth, y,
                Capitalize(currentMonthStart.ToString("MMMM yyyy")),
                vtasRows, vtasTotal,
                detHeaders, detWidths, cellFont, headerFont,
                Margin, document, FooterHeight, smallFont, dateText);
        }
        else
            y = DrawNoActivityMessage(gfx, y, cellFont, Margin, usableWidth);

        if (vtasPrev.Count > 0)
        {
            if (y + 20 > (float)page.Height.Point - FooterHeight)
            {
                DrawFooter(gfx, page, pageNum, smallFont, dateText);
                pageNum++;
                gfx.Dispose();
                page = document.AddPage();
                gfx = XGraphics.FromPdfPage(page);
                pageWidth = (float)page.Width.Point;
                usableWidth = pageWidth - Margin * 2;
                y = Margin;
            }
            var (vtasPrevRows, vtasPrevTotal) = ExpandVentasConArticulos(vtasPrev, data.ArticulosVenta);
            y = RenderMonthTable(ref page, ref pageNum, ref gfx, ref pageWidth, ref usableWidth, y,
                Capitalize(prevMonthStart.ToString("MMMM yyyy")),
                vtasPrevRows, vtasPrevTotal,
                detHeaders, detWidths, cellFont, headerFont,
                Margin, document, FooterHeight, smallFont, dateText);
        }

        // — Gastos —
        var gtosCurrent = data.Gastos
            .Where(g => !g.IsDeleted && g.Fecha >= currentMonthStart && g.Fecha < currentMonthStart.AddMonths(1))
            .ToList();
        var gtosPrev = data.Gastos
            .Where(g => !g.IsDeleted && g.Fecha >= prevMonthStart && g.Fecha < currentMonthStart)
            .ToList();

        if (y + 36 > (float)page.Height.Point - FooterHeight)
        {
            DrawFooter(gfx, page, pageNum, smallFont, dateText);
            pageNum++;
            gfx.Dispose();
            page = document.AddPage();
            gfx = XGraphics.FromPdfPage(page);
            pageWidth = (float)page.Width.Point;
            usableWidth = pageWidth - Margin * 2;
            y = Margin;
        }
        y = DrawSectionHeader(gfx, "Detalle de Gastos", sectionFont,
            Margin, usableWidth, y);

        if (gtosCurrent.Count > 0)
        {
            var (gtosRows, gtosTotal) = ExpandGastosConArticulos(gtosCurrent, data.ArticulosGasto);
            y = RenderMonthTable(ref page, ref pageNum, ref gfx, ref pageWidth, ref usableWidth, y,
                Capitalize(currentMonthStart.ToString("MMMM yyyy")),
                gtosRows, gtosTotal,
                detHeaders, detWidths, cellFont, headerFont,
                Margin, document, FooterHeight, smallFont, dateText);
        }
        else
            y = DrawNoActivityMessage(gfx, y, cellFont, Margin, usableWidth);

        if (gtosPrev.Count > 0)
        {
            if (y + 20 > (float)page.Height.Point - FooterHeight)
            {
                DrawFooter(gfx, page, pageNum, smallFont, dateText);
                pageNum++;
                gfx.Dispose();
                page = document.AddPage();
                gfx = XGraphics.FromPdfPage(page);
                pageWidth = (float)page.Width.Point;
                usableWidth = pageWidth - Margin * 2;
                y = Margin;
            }
            var (gtosPrevRows, gtosPrevTotal) = ExpandGastosConArticulos(gtosPrev, data.ArticulosGasto);
            y = RenderMonthTable(ref page, ref pageNum, ref gfx, ref pageWidth, ref usableWidth, y,
                Capitalize(prevMonthStart.ToString("MMMM yyyy")),
                gtosPrevRows, gtosPrevTotal,
                detHeaders, detWidths, cellFont, headerFont,
                Margin, document, FooterHeight, smallFont, dateText);
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

        if (y + 36 > (float)page.Height.Point - FooterHeight)
        {
            DrawFooter(gfx, page, pageNum, smallFont, dateText);
            pageNum++;
            gfx.Dispose();
            page = document.AddPage();
            gfx = XGraphics.FromPdfPage(page);
            pageWidth = (float)page.Width.Point;
            usableWidth = pageWidth - Margin * 2;
            y = Margin;
        }
        y = DrawSectionHeader(gfx, "Detalle de Encargos", sectionFont,
            Margin, usableWidth, y);

        if (encCurrent.Count > 0)
        {
            var (encRows, encTotal) = ExpandEncargosConArticulos(encCurrent, data.ArticulosEncargo);
            y = RenderMonthTable(ref page, ref pageNum, ref gfx, ref pageWidth, ref usableWidth, y,
                Capitalize(currentMonthStart.ToString("MMMM yyyy")),
                encRows, encTotal,
                encHeaders, encWidths, cellFont, headerFont,
                Margin, document, FooterHeight, smallFont, dateText);
        }
        else
            y = DrawNoActivityMessage(gfx, y, cellFont, Margin, usableWidth);

        if (encPrev.Count > 0)
        {
            if (y + 20 > (float)page.Height.Point - FooterHeight)
            {
                DrawFooter(gfx, page, pageNum, smallFont, dateText);
                pageNum++;
                gfx.Dispose();
                page = document.AddPage();
                gfx = XGraphics.FromPdfPage(page);
                pageWidth = (float)page.Width.Point;
                usableWidth = pageWidth - Margin * 2;
                y = Margin;
            }
            var (encPrevRows, encPrevTotal) = ExpandEncargosConArticulos(encPrev, data.ArticulosEncargo);
            y = RenderMonthTable(ref page, ref pageNum, ref gfx, ref pageWidth, ref usableWidth, y,
                Capitalize(prevMonthStart.ToString("MMMM yyyy")),
                encPrevRows, encPrevTotal,
                encHeaders, encWidths, cellFont, headerFont,
                Margin, document, FooterHeight, smallFont, dateText);
        }

        // ── FOOTER on last page ──
        DrawFooter(gfx, page, pageNum, smallFont, dateText);
        gfx.Dispose();

        var stream = new MemoryStream();
        document.Save(stream);
        stream.Position = 0;
        return stream;
    }

    // ══════════════════════════════════════════════════════
    //  Footer — drawn BEFORE leaving each page
    // ══════════════════════════════════════════════════════

    private static void DrawFooter(XGraphics gfx, PdfPage page, int pageNum, XFont font, string dateText)
    {
        float pw = (float)page.Width.Point;
        float ph = (float)page.Height.Point;
        float fy = ph - FooterHeight;

        var pen   = new XPen(BorderLight, 0.5);
        var brush = new XSolidBrush(TextMuted);

        gfx.DrawLine(pen, 0, fy + 5, pw, fy + 5);

        gfx.DrawString(
            $"Página {pageNum}",
            font, brush,
            new XPoint(0, fy + 12));

        gfx.DrawString(
            dateText,
            font, brush,
            new XPoint(pw - 90, fy + 12));
    }

    private static XBrush PdfBrushesWhite() => XBrushes.White;

    // ══════════════════════════════════════════════════════
    //  Helpers
    // ══════════════════════════════════════════════════════

    private static float DrawSectionHeader(
        XGraphics gfx, string title, XFont font,
        float margin, float usableWidth, float y)
    {
        gfx.DrawRectangle(new XSolidBrush(SectionBg), margin, y, usableWidth, 30);
        gfx.DrawRectangle(new XSolidBrush(BluePrimary), margin, y, 4, 30);
        gfx.DrawString(title, font, new XSolidBrush(TextDark),
            new XPoint(margin + 14, y + 6));
        return y + 36;
    }

    private static float DrawFinancialSummaryTable(
        XGraphics gfx, BalanceExportDto data,
        XFont headerFont, XFont cellFont,
        float margin, float usableWidth, float y)
    {
        float col0Width = usableWidth * 0.6f;
        float col1Width = usableWidth * 0.4f;
        float rowHeight = 24;
        float padX = 8;
        float padY = 5;

        string[][] rows =
        [
            ["Total Ventas",   $"$ {data.TotalVentas:N2}"],
            ["Total Gastos",   $"$ {data.TotalGastos:N2}"],
            ["Total Encargos", $"$ {data.TotalEncargos:N2}"],
        ];

        DrawTableRow(gfx, ["Métrica", "Valor"], margin, y,
            [col0Width, col1Width], headerFont,
            true, BluePrimary, XBrushes.White,
            padX, padY, [XStringAlignment.Near, XStringAlignment.Far]);
        y += rowHeight;

        foreach (var row in rows)
        {
            DrawTableRow(gfx, row, margin, y,
                [col0Width, col1Width], cellFont,
                false, XColors.White, null,
                padX, padY, [XStringAlignment.Near, XStringAlignment.Far]);
            y += rowHeight;
        }

        DrawTableRow(gfx, ["Ganancias", $"$ {data.Ganancias:N2}"], margin, y,
            [col0Width, col1Width], headerFont,
            false, XColors.White, null,
            padX, padY,
            [XStringAlignment.Near, XStringAlignment.Far],
            textBrushOverride: [null, new XSolidBrush(GreenProfit)]);
        y += rowHeight;

        DrawTableRow(gfx, ["Margen", $"{data.Margen:N2} %"], margin, y,
            [col0Width, col1Width], cellFont,
            false, XColors.White, null,
            padX, padY, [XStringAlignment.Near, XStringAlignment.Far]);
        y += rowHeight + 14;

        return y;
    }

    private static void DrawTableRow(
        XGraphics gfx, string[] cells, float x, float y,
        float[] widths, XFont font,
        bool isHeader, XColor bgColor, XBrush? textBrush,
        float padX, float padY,
        XStringAlignment[] alignments,
        XBrush?[]? textBrushOverride = null)
    {
        float cellX = x;
        for (int i = 0; i < cells.Length; i++)
        {
            float w = widths[i];

            if (isHeader)
                gfx.DrawRectangle(new XSolidBrush(bgColor), cellX, y, w, 24);
            else
                gfx.DrawRectangle(XBrushes.White, cellX, y, w, 24);

            XBrush brush = textBrushOverride?[i] ?? textBrush ?? new XSolidBrush(TextDark);
            var fmt = new XStringFormat();
            fmt.Alignment = alignments[i];
            fmt.LineAlignment = XLineAlignment.Center;
            gfx.DrawString(cells[i], font, brush,
                new XRect(cellX + padX, y + padY, w - padX * 2, 24 - padY * 2), fmt);

            if (isHeader)
                gfx.DrawLine(new XPen(XColors.White, 0.5), cellX + w, y, cellX + w, y + 24);

            cellX += w;
        }

        if (isHeader)
            gfx.DrawLine(new XPen(BorderLight, 0.5), x, y + 24, x + widths.Sum(), y + 24);
    }

    private static float DrawNoActivityMessage(
        XGraphics gfx, float y, XFont font,
        float margin, float usableWidth)
    {
        var fmt = new XStringFormat();
        fmt.Alignment = XStringAlignment.Near;
        fmt.LineAlignment = XLineAlignment.Center;
        gfx.DrawString("No se registró actividad en este mes a la fecha.",
            font, new XSolidBrush(TextMuted),
            new XRect(margin, y, usableWidth, 20), fmt);
        return y + 22;
    }

    private static float RenderMonthTable(
        ref PdfPage page, ref int pageNum, ref XGraphics gfx,
        ref float pageWidth, ref float usableWidth, float currentY,
        string monthLabel,
        string[][] rows, decimal total,
        string[] headers, float[] widths,
        XFont cellFont, XFont headerFont,
        float margin,
        PdfDocument document, float footerHeight,
        XFont smallFont, string dateText)
    {
        float y = currentY;
        float rowHeight = 18;
        float estimated = 22 + 22 + rows.Length * rowHeight + 22 + 8;

        if (y + estimated > (float)page.Height.Point - footerHeight)
        {
            DrawFooter(gfx, page, pageNum, smallFont, dateText);
            pageNum++;
            gfx.Dispose();
            page = document.AddPage();
            gfx = XGraphics.FromPdfPage(page);
            pageWidth = (float)page.Width.Point;
            usableWidth = pageWidth - margin * 2;
            y = margin;
        }

        // Month sub-header
        gfx.DrawRectangle(new XSolidBrush(MonthBg), margin, y, usableWidth, 22);
        gfx.DrawString(monthLabel, headerFont, new XSolidBrush(TextDark),
            new XPoint(margin + 8, y + 3));
        y += 26;

        // Draw detail grid
        y = DrawDetailGrid(gfx, headers, widths, rows, total,
            headerFont, cellFont, usableWidth, margin, y);

        return y + 8;
    }

    private static float DrawDetailGrid(
        XGraphics gfx, string[] headers, float[] widths,
        string[][] rows, decimal total,
        XFont headerFont, XFont cellFont,
        float usableWidth, float margin, float y)
    {
        int colCount = headers.Length;
        float totalWidthRatio = (float)widths.Sum();
        float[] colWidths = new float[colCount];
        for (int i = 0; i < colCount; i++)
            colWidths[i] = usableWidth * widths[i] / totalWidthRatio;

        float rowHeight = 18;
        float padX = 5;
        float padY = 3;

        XStringAlignment[] headerAlignments = new XStringAlignment[colCount];
        for (int i = 0; i < colCount; i++)
            headerAlignments[i] = i == colCount - 1 ? XStringAlignment.Far : XStringAlignment.Near;

        DrawTableRow(gfx, headers, margin, y, colWidths, headerFont,
            true, BluePrimary, XBrushes.White, padX, padY, headerAlignments);
        y += rowHeight;

        XStringAlignment[] cellAlignments = new XStringAlignment[colCount];
        for (int i = 0; i < colCount; i++)
            cellAlignments[i] = i == colCount - 1 ? XStringAlignment.Far : XStringAlignment.Near;

        for (int r = 0; r < rows.Length; r++)
        {
            DrawTableRow(gfx, rows[r], margin, y, colWidths, cellFont,
                false, XColors.White, null, padX, padY, cellAlignments);
            y += rowHeight;
        }

        string[] totalRow = new string[colCount];
        totalRow[0] = "TOTAL";
        for (int i = 1; i < colCount - 1; i++)
            totalRow[i] = "";
        totalRow[colCount - 1] = $"$ {total:N2}";

        XStringAlignment[] totalAlignments = new XStringAlignment[colCount];
        totalAlignments[0] = XStringAlignment.Near;
        for (int i = 1; i < colCount - 1; i++)
            totalAlignments[i] = XStringAlignment.Center;
        totalAlignments[colCount - 1] = XStringAlignment.Far;

        DrawTableRow(gfx, totalRow, margin, y, colWidths, headerFont,
            false, XColors.White, null, padX, padY, totalAlignments);
        y += rowHeight;

        return y;
    }

    private static string Capitalize(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return char.ToUpper(value[0]) + value[1..];
    }

    // ══════════════════════════════════════════════════════
    //  Article-expansion helpers (multi-artículo)
    // ══════════════════════════════════════════════════════

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
