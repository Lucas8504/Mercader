using Mercader.Models.Domain;
using Mercader.Services.Interfaces;

namespace Mercader.Services
{
    /// <summary>
    /// Implementación de IBalanceCalculatorService para cálculos de balance.
    /// </summary>
    public class BalanceCalculatorService : IBalanceCalculatorService
    {
        public decimal CalcularTotalVentas(IEnumerable<Ventas> ventas, string periodo)
        {
            var filtradas = FiltrarPorPeriodo(ventas, periodo);
            return filtradas.Sum(v => v.Precio * v.Cantidad);
        }

        public decimal CalcularTotalGastos(IEnumerable<Gasto> gastos, string periodo)
        {
            var filtradas = FiltrarPorPeriodo(gastos, periodo);
            return filtradas.Sum(g => g.Monto * g.Cantidad);
        }

        public decimal CalcularTotalEncargos(IEnumerable<Encargo> encargos, string periodo)
        {
            var filtradas = FiltrarPorPeriodo(encargos, periodo);
            return filtradas.Sum(e => e.Precio * e.Cantidad);
        }

        public decimal CalcularGanancias(decimal totalVentas, decimal totalGastos)
        {
            return totalVentas - totalGastos;
        }

        public decimal CalcularMargen(decimal totalVentas, decimal ganancias)
        {
            if (totalVentas == 0)
                return 0;

            return (ganancias / totalVentas) * 100;
        }

        public IEnumerable<T> FiltrarPorPeriodo<T>(IEnumerable<T> lista, string periodo) where T : IFecha
        {
            var hoy = DateTime.Now;

            return periodo switch
            {
                "Días" => lista.Where(x => x.Fecha >= hoy.AddDays(-366)),
                "Semanas" => lista.Where(x => x.Fecha >= hoy.AddDays(-910)),
                "Meses" => lista.Where(x => x.Fecha >= hoy.AddMonths(-12)),
                "Años" => lista.Where(x => x.Fecha >= hoy.AddYears(-10)),
                _ => lista.Where(x => x.Fecha >= hoy.AddMonths(-12))
            };
        }

        public List<(string Periodo, decimal Total)> AgruparVentasPorPeriodo(IEnumerable<Ventas> ventas, string periodo)
        {
            return AgruparPorPeriodo(ventas, periodo, v => v.Fecha, v => v.Precio * v.Cantidad);
        }

        public List<(string Periodo, decimal Total)> AgruparGastosPorPeriodo(IEnumerable<Gasto> gastos, string periodo)
        {
            return AgruparPorPeriodo(gastos, periodo, g => g.Fecha, g => g.Monto * g.Cantidad);
        }

        public List<(string Periodo, decimal Total)> AgruparEncargosPorPeriodo(IEnumerable<Encargo> encargos, string periodo)
        {
            return AgruparPorPeriodo(encargos, periodo, e => e.Fecha, e => e.Precio * e.Cantidad);
        }

        private List<(string Periodo, decimal Total)> AgruparPorPeriodo<T>(
            IEnumerable<T> lista,
            string periodo,
            Func<T, DateTime> getFecha,
            Func<T, decimal> getMonto)
            where T : IFecha
        {
            var filtradas = FiltrarPorPeriodo(lista, periodo).ToList();

            return periodo switch
            {
                "Días" => filtradas
                    .GroupBy(x => getFecha(x).ToString("yyyy-MM-dd"))
                    .Select(g => (g.Key, g.Sum(getMonto)))
                    .OrderBy(x => x.Key)
                    .ToList(),

                "Semanas" => filtradas
                    .GroupBy(x => GetWeekNumber(getFecha(x)))
                    .Select(g => (g.Key, g.Sum(getMonto)))
                    .OrderBy(x => x.Item1)
                    .ToList(),

                "Meses" => filtradas
                    .GroupBy(x => getFecha(x).ToString("yyyy-MM"))
                    .Select(g => (g.Key, g.Sum(getMonto)))
                    .OrderBy(x => x.Key)
                    .ToList(),

                "Años" => filtradas
                    .GroupBy(x => getFecha(x).ToString("yyyy"))
                    .Select(g => (g.Key, g.Sum(getMonto)))
                    .OrderBy(x => x.Key)
                    .ToList(),

                _ => filtradas
                    .GroupBy(x => getFecha(x).ToString("yyyy-MM"))
                    .Select(g => (g.Key, g.Sum(getMonto)))
                    .OrderBy(x => x.Key)
                    .ToList()
            };
        }

        private string GetWeekNumber(DateTime date)
        {
            var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
            int week = cal.GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday);
            return $"{date.Year}-W{week:D2}";
        }
    }
}