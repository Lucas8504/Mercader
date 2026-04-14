using System.Collections.Generic;
using Mercader.Models.Domain;

namespace Mercader.Models
{
    public class BalanceExportDto
    {
        public IReadOnlyList<Ventas> Ventas { get; init; } = [];
        public IReadOnlyList<Gasto> Gastos { get; init; } = [];
        public IReadOnlyList<Encargo> Encargos { get; init; } = [];

        public decimal TotalVentas { get; set; }
        public decimal TotalGastos { get; set; }
        public decimal TotalEncargos { get; set; }
        public decimal Ganancias { get; set; }
        public decimal Margen { get; set; }
        public string Periodo { get; init; } = "";
    }
}
