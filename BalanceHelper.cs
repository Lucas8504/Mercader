using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Mercader.Helpers
{
    public static class BalanceHelper
    {
        public static List<(string Periodo, decimal Total)> AgruparVentas(List<Ventas> ventas, string periodo)
        {
            if (ventas == null || !ventas.Any())
                return new List<(string, decimal)>();

            return periodo switch
            {
                "Días" => AgruparPorDia(ventas),
                "Semanas" => AgruparPorSemana(ventas),
                _ => AgruparPorMes(ventas)
            };
        }

        public static List<(string Periodo, decimal Total)> AgruparGastos(List<Gasto> gastos, string periodo)
        {
            if (gastos == null || !gastos.Any())
                return new List<(string, decimal)>();

            return periodo switch
            {
                "Días" => AgruparPorDia(gastos),
                "Semanas" => AgruparPorSemana(gastos),
                _ => AgruparPorMes(gastos)
            };
        }
        /// <summary>
        /// Rellena los períodos faltantes con valores cero.
        /// </summary>
        public static List<(string Periodo, decimal Total)> RellenarPeriodosFaltantes(
            List<(string Periodo, decimal Total)> datos, IEnumerable<string> periodosRequeridos)
        {
            return periodosRequeridos
                .GroupJoin(datos,
                    periodo => periodo,
                    dato => dato.Periodo,
                    (periodo, datosGrupo) => (
                        Periodo: periodo,
                        Total: datosGrupo.Select(d => d.Total).FirstOrDefault()
                    ))
                .Select(x => (x.Periodo, x.Total))
                .ToList();
        }

        /// <summary>
        /// Obtiene los últimos N días en formato yyyy-MM-dd.
        /// </summary>
        public static List<string> ObtenerUltimosDias(int dias)
        {
            return Enumerable.Range(0, dias)
                .Select(i => DateTime.Now.AddDays(-i).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
                .Reverse()
                .ToList();
        }

        /// <summary>
        /// Obtiene las últimas N semanas en formato yyyy-ww.
        /// </summary>
        public static List<string> ObtenerUltimasSemanas(int semanas)
        {
            return Enumerable.Range(0, semanas)
                .Select(i =>
                {
                    var fecha = DateTime.Now.AddDays(-7 * i);
                    return $"{fecha.Year}-{GetWeekNumber(fecha):D2}";
                })
                .Reverse()
                .ToList();
        }

        /// <summary>
        /// Obtiene los últimos N meses en formato yyyy-MM.
        /// </summary>
        public static List<string> ObtenerUltimosMeses(int meses)
        {
            return Enumerable.Range(0, meses)
                .Select(i => DateTime.Now.AddMonths(-i).ToString("yyyy-MM", CultureInfo.InvariantCulture))
                .Reverse()
                .ToList();
        }

        /// <summary>
        /// Agrupa registros financieros por día.
        /// </summary>
        private static List<(string Periodo, decimal Total)> AgruparPorDia<T>(List<T> registros)
            where T : IRegistroFinanciero
        {
            return registros
                .GroupBy(r => r.Fecha.ToString("yyyy-MM-dd"))
                .Select(g => (
                    Periodo: g.Key,
                    Total: g.Sum(r => r.Monto * r.Cantidad)
                ))
                .OrderBy(x => x.Periodo)
                .ToList();
        }

        /// <summary>
        /// Agrupa registros financieros por semana.
        /// </summary>
        private static List<(string Periodo, decimal Total)> AgruparPorSemana<T>(List<T> registros)
            where T : IRegistroFinanciero
        {
            return registros
                .GroupBy(r => GetWeekNumber(r.Fecha))
                .Select(g => (
                    Periodo: $"Semana {g.Key}",
                    Total: g.Sum(r => r.Monto * r.Cantidad)
                ))
                .OrderBy(x => x.Periodo)
                .ToList();
        }

        /// <summary>
        /// Agrupa registros financieros por mes.
        /// </summary>
        private static List<(string Periodo, decimal Total)> AgruparPorMes<T>(List<T> registros)
            where T : IRegistroFinanciero
        {
            return registros
                .GroupBy(r => r.Fecha.ToString("yyyy-MM"))
                .Select(g => (
                    Periodo: g.Key,
                    Total: g.Sum(r => r.Monto * r.Cantidad)
                ))
                .OrderBy(x => x.Periodo)
                .ToList();
        }

        /// <summary>
        /// Obtiene el número de la semana del año para una fecha dada.
        /// </summary>
        private static int GetWeekNumber(DateTime date)
        {
            return CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                date, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
        }
    }

    /// <summary>
    /// Interfaz común para registros financieros (Ventas y Gastos).
    /// </summary>
    public interface IRegistroFinanciero
    {
        DateTime Fecha { get; }
        decimal Monto { get; }
        int Cantidad { get; }
    }
}