using System.Globalization;
using Mercader.Models;
using Mercader.Models.Domain;
using Mercader.Services.Interfaces;
using Microcharts;
using System.Collections.ObjectModel;

namespace Mercader.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        // ===== Colecciones de datos =====
        public ObservableCollection<Ventas> Ventas { get; } = new();
        public ObservableCollection<Gasto> Gastos { get; } = new();
        public ObservableCollection<Encargo> Encargos { get; } = new();

        // ===== Servicios =====
        private readonly IDataRepository _dataRepository;
        private readonly IChartService _chartService;
        private readonly IBalanceCalculatorService _balanceService;

        // ===== Gráficos (bindeables) =====
        private Chart? _encargosChart;
        public Chart? EncargosChart
        {
            get => _encargosChart;
            private set => SetProperty(ref _encargosChart, value);
        }

        private Chart? _ventasChart;
        public Chart? VentasChart
        {
            get => _ventasChart;
            private set => SetProperty(ref _ventasChart, value);
        }

        private Chart? _gastosChart;
        public Chart? GastosChart
        {
            get => _gastosChart;
            private set => SetProperty(ref _gastosChart, value);
        }

        private Chart? _gananciasChart;
        public Chart? GananciasChart
        {
            get => _gananciasChart;
            private set => SetProperty(ref _gananciasChart, value);
        }

        // ===== Resumen financiero (bindeables) =====
        private string _totalVentas = "$0";
        public string TotalVentas
        {
            get => _totalVentas;
            private set => SetProperty(ref _totalVentas, value);
        }

        private string _totalGastos = "$0";
        public string TotalGastos
        {
            get => _totalGastos;
            private set => SetProperty(ref _totalGastos, value);
        }

        private string _totalEncargos = "$0";
        public string TotalEncargos
        {
            get => _totalEncargos;
            private set => SetProperty(ref _totalEncargos, value);
        }

        private string _ganancias = "$0";
        public string Ganancias
        {
            get => _ganancias;
            private set => SetProperty(ref _ganancias, value);
        }

        private string _margen = "0%";
        public string Margen
        {
            get => _margen;
            private set => SetProperty(ref _margen, value);
        }

        // ===== Selector de período =====
        public List<string> Periodos { get; } = new() { "Días", "Semanas", "Meses", "Años" };

        private string _periodoSeleccionado = "Meses";
        public string PeriodoSeleccionado
        {
            get => _periodoSeleccionado;
            set
            {
                if (SetProperty(ref _periodoSeleccionado, value))
                {
                    Recalcular();
                }
            }
        }

        // ===== Constructor =====
        public MainViewModel(
            IDataRepository dataRepository,
            IChartService chartService,
            IBalanceCalculatorService balanceService)
        {
            _dataRepository = dataRepository;
            _chartService = chartService;
            _balanceService = balanceService;

            // Recalcular cuando cambian las colecciones
            Ventas.CollectionChanged += (_, _) => Recalcular();
            Gastos.CollectionChanged += (_, _) => Recalcular();
            Encargos.CollectionChanged += (_, _) => Recalcular();
        }

        /// <summary>
        /// Recalcula todos los totales, datos por período y gráficos.
        /// </summary>
        public void Recalcular()
        {
            // Calcular totales usando BalanceCalculatorService
            var totalVentas = _balanceService.CalcularTotalVentas(Ventas, PeriodoSeleccionado);
            var totalGastos = _balanceService.CalcularTotalGastos(Gastos, PeriodoSeleccionado);
            var totalEncargos = _balanceService.CalcularTotalEncargos(Encargos, PeriodoSeleccionado);
            var ganancias = _balanceService.CalcularGanancias(totalVentas, totalGastos);
            var margen = _balanceService.CalcularMargen(totalVentas, ganancias);

            // Agrupar datos por período
            var ventasPorPeriodo = _balanceService.AgruparVentasPorPeriodo(Ventas, PeriodoSeleccionado);
            var gastosPorPeriodo = _balanceService.AgruparGastosPorPeriodo(Gastos, PeriodoSeleccionado);
            var encargosPorPeriodo = _balanceService.AgruparEncargosPorPeriodo(Encargos, PeriodoSeleccionado);

            // Actualizar UI
            TotalVentas = totalVentas.ToString("C");
            TotalGastos = totalGastos.ToString("C");
            TotalEncargos = totalEncargos.ToString("C");
            Ganancias = ganancias.ToString("C");
            Margen = $"{margen:F1}%";

            // Generar gráficos
            EncargosChart = _chartService.CrearGraficoEncargos(encargosPorPeriodo);
            VentasChart = _chartService.CrearGraficoVentas(ventasPorPeriodo);
            GastosChart = _chartService.CrearGraficoGastos(gastosPorPeriodo);
            GananciasChart = _chartService.CrearGraficoGanancias(ventasPorPeriodo, gastosPorPeriodo);
        }

        /// <summary>
        /// Actualiza los gráficos (método público para compatibilidad).
        /// </summary>
        public Task ActualizarGraficosAsync() => Task.CompletedTask;

        /// <summary>
        /// Crea el DTO para exportar a Excel.
        /// </summary>
        public BalanceExportDto CrearExportDto()
        {
            return new BalanceExportDto
            {
                Ventas = Ventas.ToList(),
                Gastos = Gastos.ToList(),
                Encargos = Encargos.ToList(),
                TotalVentas = decimal.Parse(TotalVentas, NumberStyles.Currency),
                TotalGastos = decimal.Parse(TotalGastos, NumberStyles.Currency),
                TotalEncargos = decimal.Parse(TotalEncargos, NumberStyles.Currency),
                Ganancias = decimal.Parse(Ganancias, NumberStyles.Currency),
                Margen = decimal.Parse(Margen.TrimEnd('%')),
                Periodo = PeriodoSeleccionado
            };
        }
    }
}
