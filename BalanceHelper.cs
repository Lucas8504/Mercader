using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Mercader.Helpers
{
    public static class BalanceHelper
    {
        public static List<(string Mes, decimal Total)> AgruparVentasPorMes(List<Ventas> ventas)
        {
            if (ventas == null || !ventas.Any())
                return new List<(string, decimal)>();

            return ventas
                .GroupBy(v => new DateTime(v.Fecha.Year, v.Fecha.Month, 1))
                .Select(g => (
                    Mes: g.Key.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                    Total: g.Sum(v => v.Precio * v.Cantidad)
                ))
                .OrderBy(x => x.Mes)
                .ToList();
        }

        public static List<(string Mes, decimal Total)> AgruparGastosPorMes(List<Gasto> gastos)
        {
            if (gastos == null || !gastos.Any())
                return new List<(string, decimal)>();

            return gastos
                .GroupBy(g => new DateTime(g.Fecha.Year, g.Fecha.Month, 1))
                .Select(g => (
                    Mes: g.Key.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                    Total: g.Sum(x => x.Monto * x.Cantidad)
                ))
                .OrderBy(x => x.Mes)
                .ToList();
        }

        public static List<(string Mes, decimal Total)> RellenarMesesFaltantes(
            List<(string Mes, decimal Total)> datos, IEnumerable<string> mesesRequeridos)
        {
            return mesesRequeridos
                .GroupJoin(datos,
                    mes => mes,
                    dato => dato.Mes,
                    (mes, datosGrupo) => (
                        Mes: mes,
                        Total: datosGrupo.Select(d => d.Total).FirstOrDefault()
                    ))
                .Select(x => (x.Mes, x.Total))
                .ToList();
        }

        public static List<string> ObtenerUltimos6Meses()
        {
            return Enumerable.Range(0, 6)
                .Select(i => DateTime.Now.AddMonths(-i).ToString("yyyy-MM", CultureInfo.InvariantCulture))
                .Reverse()
                .ToList();
        }
    }
}