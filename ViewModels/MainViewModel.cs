using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mercader.Models;
using Mercader.Domain.Entities;
using Mercader.Services.Interfaces;
using Mercader.Data.Interfaces;
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

        // ===== Resumen del mes actual =====
        [ObservableProperty]
        private string _mesLabel = DateTime.Now.ToString("MMMM yyyy", new CultureInfo("es-ES"));

        [ObservableProperty]
        private string _mesVentas = "$0";

        [ObservableProperty]
        private string _mesGastos = "$0";

        [ObservableProperty]
        private string _mesEncargos = "$0";

        [ObservableProperty]
        private string _mesGanancias = "$0";

        [ObservableProperty]
        private string _mesMargen = "0%";

        // ===== Resumen financiero por período (bindeables) =====
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

        // ===== Valores raw para exportación (sin formateo) =====
        private decimal _rawTotalVentas;
        private decimal _rawTotalGastos;
        private decimal _rawTotalEncargos;
        private decimal _rawGanancias;
        private decimal _rawMargen;

        // ===== Selector de período =====
        public List<string> Periodos { get; } = new() { "Días", "Semanas", "Meses", "Años" };

        [ObservableProperty]
        private string _periodoSeleccionado = "Meses";

        private readonly INavigationService _navigationService;

        // ===== Constructor =====
        public MainViewModel(
            IDataRepository dataRepository,
            IChartService chartService,
            IBalanceCalculatorService balanceService,
            INavigationService navigationService)
        {
            _dataRepository = dataRepository;
            _chartService = chartService;
            _balanceService = balanceService;
            _navigationService = navigationService;
        }

        partial void OnPeriodoSeleccionadoChanged(string value)
        {
            Recalcular();
        }

        // ===== Navegación a modales =====

        [RelayCommand]
        private async Task AgregarEncargoAsync()
        {
            await _navigationService.PushModalAsync<EncModal>();
            await CargarDatosCommand.ExecuteAsync(null);
        }

        [RelayCommand]
        private async Task AgregarVentaAsync()
        {
            await _navigationService.PushModalAsync<VentaModal>();
            await CargarDatosCommand.ExecuteAsync(null);
        }

        [RelayCommand]
        private async Task AgregarGastoAsync()
        {
            await _navigationService.PushModalAsync<GastoModal>();
            await CargarDatosCommand.ExecuteAsync(null);
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

            // Guardar raw values para exportación (evita string→decimal)
            _rawTotalVentas = totalVentas;
            _rawTotalGastos = totalGastos;
            _rawTotalEncargos = totalEncargos;
            _rawGanancias = ganancias;
            _rawMargen = margen;

            // Actualizar UI con formato
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

            // Resumen del mes actual
            CalcularResumenMensual();
        }

        private void CalcularResumenMensual()
        {
            var hoy = DateTime.Now;
            MesLabel = hoy.ToString("MMMM yyyy", new CultureInfo("es-ES"));

            var ventas = Ventas
                .Where(v => v.Fecha.Year == hoy.Year && v.Fecha.Month == hoy.Month)
                .Sum(v => v.Precio * v.Cantidad);
            var gastos = Gastos
                .Where(g => g.Fecha.Year == hoy.Year && g.Fecha.Month == hoy.Month)
                .Sum(g => g.Monto * g.Cantidad);
            var encargos = Encargos
                .Where(e => e.Fecha.Year == hoy.Year && e.Fecha.Month == hoy.Month)
                .Sum(e => e.Precio * e.Cantidad);
            var ganancias = ventas - gastos;
            var margen = ventas > 0 ? (ganancias / ventas) * 100 : 0;

            MesVentas = ventas.ToString("C");
            MesGastos = gastos.ToString("C");
            MesEncargos = encargos.ToString("C");
            MesGanancias = ganancias.ToString("C");
            MesMargen = $"{margen:F1}%";
        }

        // ===== CRUD Commands =====

        [RelayCommand]
        private async Task GuardarVentaAsync(Ventas venta)
        {
            await ExecuteBusyAsync(async () =>
            {
                await _dataRepository.SaveVentasAsync(venta);
                await CargarDatosCommand.ExecuteAsync(null);
            });
        }

        [RelayCommand]
        private async Task EliminarVentaAsync(Ventas venta)
        {
            await ExecuteBusyAsync(async () =>
            {
                await _dataRepository.DeleteVentaAsync(venta);
                Ventas.Remove(venta);
                Recalcular();
            });
        }

        [RelayCommand]
        private async Task GuardarGastoAsync(Gasto gasto)
        {
            await ExecuteBusyAsync(async () =>
            {
                await _dataRepository.SaveGastoAsync(gasto);
                await CargarDatosCommand.ExecuteAsync(null);
            });
        }

        [RelayCommand]
        private async Task EliminarGastoAsync(Gasto gasto)
        {
            await ExecuteBusyAsync(async () =>
            {
                await _dataRepository.DeleteGastoAsync(gasto);
                Gastos.Remove(gasto);
                Recalcular();
            });
        }

        [RelayCommand]
        private async Task GuardarEncargoAsync(Encargo encargo)
        {
            await ExecuteBusyAsync(async () =>
            {
                await _dataRepository.SaveEncargoAsync(encargo);
                await CargarDatosCommand.ExecuteAsync(null);
            });
        }

        [RelayCommand]
        private async Task EliminarEncargoAsync(Encargo encargo)
        {
            await ExecuteBusyAsync(async () =>
            {
                await _dataRepository.DeleteEncargoAsync(encargo);
                Encargos.Remove(encargo);
                Recalcular();
            });
        }

        // ===== Carga de datos =====
        [RelayCommand]
        private async Task CargarDatosAsync()
        {
            await ExecuteBusyAsync(async () =>
            {
                var ventas = await _dataRepository.GetVentasAsync();
                var gastos = await _dataRepository.GetGastosAsync();
                var encargos = await _dataRepository.GetEncargosAsync();

                Ventas = new ObservableCollection<Ventas>(ventas);
                Gastos = new ObservableCollection<Gasto>(gastos);
                Encargos = new ObservableCollection<Encargo>(encargos);

                Recalcular();
            });
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
                TotalVentas = _rawTotalVentas,
                TotalGastos = _rawTotalGastos,
                TotalEncargos = _rawTotalEncargos,
                Ganancias = _rawGanancias,
                Margen = _rawMargen,
                Periodo = PeriodoSeleccionado
            };
        }
    }
}
