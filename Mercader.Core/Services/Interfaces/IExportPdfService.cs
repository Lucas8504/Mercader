using Mercader.Models;

namespace Mercader.Services.Interfaces;

public interface IExportPdfService
{
    /// <summary>
    /// Generates a PDF balance report and returns it as a MemoryStream.
    /// </summary>
    /// <param name="data">Balance data to include in the report.</param>
    /// <param name="charts">Optional list of (PNG bytes, title) charts to embed.</param>
    /// <param name="fontBytes">Optional TrueType font bytes for Unicode text rendering.</param>
    MemoryStream GenerarBalancePdf(
        BalanceExportDto data,
        IReadOnlyList<(byte[] ImageBytes, string Title)>? charts = null,
        byte[]? fontBytes = null);
}
