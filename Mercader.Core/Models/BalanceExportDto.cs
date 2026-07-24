using System.Collections.Generic;
using Mercader.Domain.Entities;

namespace Mercader.Models
{
    public class BalanceExportDto
    {
        public IReadOnlyList<Ventas> Ventas { get; init; } = [];
        public IReadOnlyList<Gasto> Gastos { get; init; } = [];
        public IReadOnlyList<Encargo> Encargos { get; init; } = [];

        // ===== MULTI-ARTÍCULO =====
        public IReadOnlyDictionary<int, List<ArticuloVenta>> ArticulosVenta { get; init; }
            = new Dictionary<int, List<ArticuloVenta>>();
        public IReadOnlyDictionary<int, List<ArticuloGasto>> ArticulosGasto { get; init; }
            = new Dictionary<int, List<ArticuloGasto>>();
        public IReadOnlyDictionary<int, List<ArticuloEncargo>> ArticulosEncargo { get; init; }
            = new Dictionary<int, List<ArticuloEncargo>>();

        public decimal TotalVentas { get; set; }
        public decimal TotalGastos { get; set; }
        public decimal TotalEncargos { get; set; }
        public decimal Ganancias { get; set; }
        public decimal Margen { get; set; }
        public string Periodo { get; init; } = "";
    }
}
