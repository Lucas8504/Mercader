using Microcharts;

public interface IChartService
{
    Chart CrearGraficoVentas(List<(string Periodo, decimal Total)> datos);
    Chart CrearGraficoEncargos(List<(string Periodo, decimal Total)> datos);
    Chart CrearGraficoGastos(List<(string Periodo, decimal Total)> datos);
    Chart CrearGraficoGanancias(
        List<(string Periodo, decimal Total)> ventas,
        List<(string Periodo, decimal Total)> gastos);
}
