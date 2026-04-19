using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mercader.Models;
using Mercader.Models.Domain;
using Mercader.Services.Interfaces;
using Microcharts;
using System.Collections.ObjectModel;

namespace Mercader.ViewModels
{
    public partial class MainViewModel : BaseViewModel
    {
        // ===== Colecciones de datos =====
        [ObservableProperty]
        private ObservableCollection<Ventas> _ventas = new();

        [ObservableProperty]
        private ObservableCollection<Gasto> _gastos = new();

        [ObservableProperty]
        private ObservableCollection<Encargo> _encargos = new();

        // ===== Servicios =====
        private readonly IDataRepository _dataRepository;
        private readonly IChartService _chartService;
        private readonly IBalanceCalculatorService _balanceService;

        // ===== Gráficos (bindeables) =====
        [ObservableProperty]
        private Chart? _encargosChart;

        [ObservableProperty]
        private Chart? _ventasChart;

        [ObservableProperty]
        private Chart? _gastosChart;

        [ObservableProperty]
        private Chart? _gananciasChart;

        // ===== Resumen financiero (bindeables) =====
        [ObservableProperty]
        private string _totalVentas = "$0";

        [ObservableProperty]
        private string _totalGastos = "$0";

        [ObservableProperty]
        private string _totalEncargos = "$0";

        [ObservableProperty]
        private string _ganancias = "$0";

        [ObservableProperty]
        private string _margen = "0%";

        // ===== Selector de período =====
        public List<string> Periodos { get; } = new() { "Días", "Semanas", "Meses", "Años" };

        [ObservableProperty]
        private string _periodoSeleccionado = "Meses";

        [ObservableProperty]
        private bool _isBusy;

        // ===== Constructor =====
        public MainViewModel(
            IDataRepository dataRepository,
            IChartService chartService,
            IBalanceCalculatorService balanceService)
        {
            _dataRepository = dataRepository;
            _chartService = chartService;
            _balanceService = balanceService;

            // Recalcular cuando cambian las colecciones (usando evento del toolkit)
            Ventas.CollectionChanged += (_, _) => Recalcular();
            Gastos.CollectionChanged += (_, _) => Recalcular();
            Encargos.CollectionChanged += (_, _) => Recalcular();
        }

        partial void OnPeriodoSeleccionadoChanged(string value)
        {
            Recalcular();
        }

        // ===== Commands =====

        [RelayCommand]
        private void Recalcular()
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

        // ===== CRUD Commands =====

        [RelayCommand]
        private async Task GuardarVentaAsync(Ventas venta)
        {
            IsBusy = true;
            try
            {
                await _dataRepository.SaveVentasAsync(venta);
                await CargarDatosCommand.ExecuteAsync(null);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task EliminarVentaAsync(Ventas venta)
        {
            IsBusy = true;
            try
            {
                await _dataRepository.DeleteVentaAsync(venta);
                Ventas.Remove(venta);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task GuardarGastoAsync(Gasto gasto)
        {
            IsBusy = true;
            try
            {
                await _dataRepository.SaveGastoAsync(gasto);
                await CargarDatosCommand.ExecuteAsync(null);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task EliminarGastoAsync(Gasto gasto)
        {
            IsBusy = true;
            try
            {
                await _dataRepository.DeleteGastoAsync(gasto);
                Gastos.Remove(gasto);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task GuardarEncargoAsync(Encargo encargo)
        {
            IsBusy = true;
            try
            {
                await _dataRepository.SaveEncargoAsync(encargo);
                await CargarDatosCommand.ExecuteAsync(null);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task EliminarEncargoAsync(Encargo encargo)
        {
            IsBusy = true;
            try
            {
                await _dataRepository.DeleteEncargoAsync(encargo);
                Encargos.Remove(encargo);
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ===== Carga de datos =====
        [RelayCommand]
        private async Task CargarDatosAsync()
        {
            IsBusy = true;
            try
            {
                var ventas = await _dataRepository.GetVentasAsync();
                var gastos = await _dataRepository.GetGastosAsync();
                var encargos = await _dataRepository.GetEncargosAsync();

                Ventas = new ObservableCollection<Ventas>(ventas);
                Gastos = new ObservableCollection<Gasto>(gastos);
                Encargos = new ObservableCollection<Encargo>(encargos);

                Recalcular();
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ===== Export =====
        // No puede ser [RelayCommand] porque devuelve valor (no void/Task)
        // El método público queda accesible para el code-behind
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
