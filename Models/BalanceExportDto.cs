using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mercader.Models
{
    public class BalanceExportDto
    {
        public IReadOnlyList<Ventas> Ventas { get; init; } = [];
        public IReadOnlyList<Gasto> Gastos { get; init; } = [];
        public IReadOnlyList<Encargo> Encargos { get; init; } = [];

        public string TotalVentas { get; init; } = "";
        public string TotalGastos { get; init; } = "";
        public string TotalEncargos { get; init; } = "";
        public string Ganancias { get; init; } = "";
        public string Margen { get; init; } = "";
        public string Periodo { get; init; } = "";

    }
}
