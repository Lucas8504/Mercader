using Microcharts;

namespace Mercader.Services.Interfaces
{
    /// <summary>
    /// Servicio para crear gráficos financieros.
    /// </summary>
    public interface IChartService
    {
        /// <summary>
        /// Crea un gráfico de ventas.
        /// </summary>
        Chart CrearGraficoVentas(List<(string Periodo, decimal Total)> datos);

        /// <summary>
        /// Crea un gráfico de encargos.
        /// </summary>
        Chart CrearGraficoEncargos(List<(string Periodo, decimal Total)> datos);

        /// <summary>
        /// Crea un gráfico de gastos.
        /// </summary>
        Chart CrearGraficoGastos(List<(string Periodo, decimal Total)> datos);

        /// <summary>
        /// Crea un gráfico de ganancias (ventas - gastos).
        /// </summary>
        Chart CrearGraficoGanancias(
            List<(string Periodo, decimal Total)> ventas,
            List<(string Periodo, decimal Total)> gastos);
    }
}
