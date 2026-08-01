using System.Globalization;
using Mercader.Domain.Entities;
using Mercader.Services.Interfaces;

namespace Mercader.Services.Calculators
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
            var filtradas = FiltrarPorPeriodo(encargos, periodo)
                .Where(e => e.Estado != "ENTREGADO");
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
            var encargosPendientes = encargos.Where(e => e.Estado != "ENTREGADO");
            return AgruparPorPeriodo(encargosPendientes, periodo, e => e.Fecha, e => e.Precio * e.Cantidad);
        }

        private List<(string Periodo, decimal Total)> AgruparPorPeriodo<T>(
            IEnumerable<T> lista,
            string periodo,
            Func<T, DateTime> getFecha,
            Func<T, decimal> getMonto)
            where T : IFecha
        {
            var datos = FiltrarPorPeriodo(lista, periodo).ToList();

            return periodo switch
            {
                "Días" => GenerarDias(datos, getFecha, getMonto),
                "Semanas" => GenerarSemanas(datos, getFecha, getMonto),
                "Meses" => GenerarMeses(datos, getFecha, getMonto),
                "Años" => GenerarAnios(datos, getFecha, getMonto),
                _ => GenerarMeses(datos, getFecha, getMonto)
            };
        }

        /// <summary>
        /// Genera todos los días del rango, incluso los que no tienen datos.
        /// </summary>
        private List<(string Periodo, decimal Total)> GenerarDias<T>(
            List<T> datos,
            Func<T, DateTime> getFecha,
            Func<T, decimal> getMonto) where T : IFecha
        {
            var hoy = DateTime.Today;
            var dias = Enumerable.Range(0, 133)
                .Select(i => hoy.AddDays(-i))
                .Reverse()
                .ToList();

            return dias.Select(fecha =>
            {
                var total = datos
                    .Where(x => getFecha(x).Date == fecha.Date)
                    .Sum(getMonto);
                return (fecha.ToString("dd/MM"), total);
            }).ToList();
        }

        /// <summary>
        /// Genera todas las semanas del rango, incluso las que no tienen datos.
        /// </summary>
        private List<(string Periodo, decimal Total)> GenerarSemanas<T>(
            List<T> datos,
            Func<T, DateTime> getFecha,
            Func<T, decimal> getMonto) where T : IFecha
        {
            var hoy = DateTime.Today;
            var semanas = Enumerable.Range(0, 130)
                .Select(i =>
                {
                    var fecha = hoy.AddDays(-7 * i);
                    var inicioSemana = fecha.AddDays(-(int)fecha.DayOfWeek);
                    return inicioSemana;
                })
                .Reverse()
                .Distinct()
                .ToList();

            return semanas.Select(inicioSemana =>
            {
                var finSemana = inicioSemana.AddDays(6);
                var total = datos
                    .Where(x =>
                    {
                        var fecha = getFecha(x).Date;
                        return fecha >= inicioSemana && fecha <= finSemana;
                    })
                    .Sum(getMonto);
                return (inicioSemana.ToString("dd/MM"), total);
            }).ToList();
        }

        /// <summary>
        /// Genera todos los meses del rango, incluso los que no tienen datos.
        /// </summary>
        private List<(string Periodo, decimal Total)> GenerarMeses<T>(
            List<T> datos,
            Func<T, DateTime> getFecha,
            Func<T, decimal> getMonto) where T : IFecha
        {
            var hoy = DateTime.Today;
            var meses = Enumerable.Range(0, 130)
                .Select(i => hoy.AddMonths(-i))
                .Reverse()
                .ToList();

            return meses.Select(mes =>
            {
                var total = datos
                    .Where(x => getFecha(x).Year == mes.Year && getFecha(x).Month == mes.Month)
                    .Sum(getMonto);
                return (mes.ToString("MMM", new CultureInfo("es-ES")), total);
            }).ToList();
        }

        /// <summary>
        /// Genera todos los años del rango, incluso los que no tienen datos.
        /// </summary>
        private List<(string Periodo, decimal Total)> GenerarAnios<T>(
            List<T> datos,
            Func<T, DateTime> getFecha,
            Func<T, decimal> getMonto) where T : IFecha
        {
            var hoy = DateTime.Today;
            var anios = Enumerable.Range(0, 10)
                .Select(i => hoy.AddYears(-i))
                .Reverse()
                .ToList();

            return anios.Select(anio =>
            {
                var total = datos
                    .Where(x => getFecha(x).Year == anio.Year)
                    .Sum(getMonto);
                return (anio.Year.ToString(), total);
            }).ToList();
        }
    }
}
