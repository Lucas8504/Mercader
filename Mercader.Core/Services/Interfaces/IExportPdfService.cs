using Mercader.Models;

namespace Mercader.Services.Interfaces;

public interface IExportPdfService
{
    /// <summary>
    /// Generates a PDF balance report and returns it as a MemoryStream.
    /// </summary>
    MemoryStream GenerarBalancePdf(BalanceExportDto data);
}
