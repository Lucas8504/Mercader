using Mercader.Domain.Entities;

namespace Mercader.Data.Interfaces
{
    /// <summary>
    /// Servicio para cálculos de balance (totales, márgenes, filtros por período).
    /// </summary>
    public interface IBalanceCalculatorService
    {
        /// <summary>
        /// Calcula el total de ventas basado en el período seleccionado.
        /// </summary>
        decimal CalcularTotalVentas(IEnumerable<Ventas> ventas, string periodo);

        /// <summary>
        /// Calcula el total de gastos basado en el período seleccionado.
        /// </summary>
        decimal CalcularTotalGastos(IEnumerable<Gasto> gastos, string periodo);

        /// <summary>
        /// Calcula el total de encargos basado en el período seleccionado.
        /// </summary>
        decimal CalcularTotalEncargos(IEnumerable<Encargo> encargos, string periodo);

        /// <summary>
        /// Calcula las ganancias (ventas - gastos).
        /// </summary>
        decimal CalcularGanancias(decimal totalVentas, decimal totalGastos);

        /// <summary>
        /// Calcula el margen de ganancia porcentual.
        /// </summary>
        decimal CalcularMargen(decimal totalVentas, decimal ganancias);

        /// <summary>
        /// Filtra una lista por período.
        /// </summary>
        IEnumerable<T> FiltrarPorPeriodo<T>(IEnumerable<T> lista, string periodo) where T : IFecha;

        /// <summary>
        /// Agrupa ventas por período y retorna lista de (período, total).
        /// </summary>
        List<(string Periodo, decimal Total)> AgruparVentasPorPeriodo(IEnumerable<Ventas> ventas, string periodo);

        /// <summary>
        /// Agrupa gastos por período y retorna lista de (período, total).
        /// </summary>
        List<(string Periodo, decimal Total)> AgruparGastosPorPeriodo(IEnumerable<Gasto> gastos, string periodo);

        /// <summary>
        /// Agrupa encargos por período y retorna lista de (período, total).
        /// </summary>
        List<(string Periodo, decimal Total)> AgruparEncargosPorPeriodo(IEnumerable<Encargo> encargos, string periodo);
    }
}