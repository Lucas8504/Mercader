using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mercader
{
    public static class BalanceHelper
    {
        // Agrupa ventas por mes
        public static List<(string Mes, decimal Total)> AgruparPorMes(List<Ventas> ventas)
        {
            return ventas.GroupBy(v => new { v.Fecha.Year, v.Fecha.Month })
                        .Select(g => (
                            Mes: $"{g.Key.Year}-{g.Key.Month:D2}",
                            Total: g.Sum(v => v.Precio * v.Cantidad)
                        ))
                        .OrderBy(x => x.Mes)
                        .ToList();
        }

        // Agrupa gastos por mes
        public static List<(string Mes, decimal Total)> AgruparPorMes(List<Gasto> gastos)
        {
            return gastos.GroupBy(g => new { g.Fecha.Year, g.Fecha.Month })
                        .Select(g => (
                            Mes: $"{g.Key.Year}-{g.Key.Month:D2}",
                            Total: g.Sum(gt => gt.Monto * gt.Cantidad)
                        ))
                        .OrderBy(x => x.Mes)
                        .ToList();
        }
    }
}
