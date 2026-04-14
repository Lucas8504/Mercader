using Mercader.Models;
using Mercader.Models.Domain;
using Mercader.Services.Interfaces;
using Microcharts;
using System.Collections.ObjectModel;

namespace Mercader.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        // ===== Datos =====
        public ObservableCollection<Ventas> Ventas { get; } = new();
        public ObservableCollection<Gasto> Gastos { get; } = new();
        public ObservableCollection<Encargo> Encargos { get; } = new();

        public IEnumerable<Ventas> VentasPeriodo => FiltrarPorPeriodo(Ventas);
        public IEnumerable<Gasto> GastosPeriodo => FiltrarPorPeriodo(Gastos);
        public IEnumerable<Encargo> EncargosPeriodo => FiltrarPorPeriodo(Encargos);

        public List<(string Periodo, decimal Total)> VentasPorPeriodo { get; set; } = new();
        public List<(string Periodo, decimal Total)> GastosPorPeriodo { get; set; } = new();
        public List<(string Periodo, decimal Total)> EncargosPorPeriodo { get; set; } = new();

        // ===== Services =====
        private readonly IDataRepository _dataRepository;
        private readonly IChartService _chartService;

        public Chart? EncargosChart { get; set; }
        public Chart? VentasChart { get; set; }
        public Chart? GastosChart { get; set; }
        public Chart? GananciasChart { get; set; }


        // ===== Valores internos =====
        private decimal _totalVentasValue;
        private decimal _totalGastosValue;
        private decimal _totalEncargosValue;
        private decimal _gananciasValue;
        private decimal _margenValue;

        // ===== Propiedades para UI (Binding) =====
        private string _totalVentas = "$0";
        public string TotalVentas
        {
            get => _totalVentas;
            set => SetProperty(ref _totalVentas, value);
        }

        private string _totalGastos = "$0";
        public string TotalGastos
        {
            get => _totalGastos;
            set => SetProperty(ref _totalGastos, value);
        }

        private string _totalEncargos = "$0";
        public string TotalEncargos
        {
            get => _totalEncargos;
            set => SetProperty(ref _totalEncargos, value);
        }

        private string _ganancias = "$0";
        public string Ganancias
        {
            get => _ganancias;
            set => SetProperty(ref _ganancias, value);
        }

        private string _margen = "0%";
        public string Margen
        {
            get => _margen;
            set => SetProperty(ref _margen, value);
        }

        // ===== Picker =====
        public List<string> Periodos { get; } = new()
        {
            "Días",
            "Semanas",
            "Meses",
            "Años"
        };

        private string _periodoSeleccionado = "Meses";
        public string PeriodoSeleccionado
        {
            get => _periodoSeleccionado;
            set
            {
                if (_periodoSeleccionado == value) return;
                _periodoSeleccionado = value;
                OnPropertyChanged();

                Recalcular();
                OnPeriodoChanged?.Invoke();
            }
        }

        // ===== Comunicación =====
        public Action? OnPeriodoChanged;

        public MainViewModel(IDataRepository dataRepository, IChartService chartService)
        {
            _dataRepository = dataRepository;
            _chartService = chartService;

            Ventas.CollectionChanged += (_, __) => Recalcular();
            Gastos.CollectionChanged += (_, __) => Recalcular();
            Encargos.CollectionChanged += (_, __) => Recalcular();
        }

        /// <summary>
        /// Actualiza SOLO el gráfico de ventas (migración MVVM paso a paso)
        /// </summary>
        public async Task ActualizarGraficoVentasAsync()
        {
            await Task.Run(() =>
            {
                VentasChart = _chartService.CrearGraficoVentas(VentasPorPeriodo);
            });

            MainThread.BeginInvokeOnMainThread(() =>
            {
                OnPropertyChanged(nameof(VentasChart));
            });
        }

        /// <summary>
        /// Actualiza SOLO el gráfico de gastos (migración MVVM paso a paso)
        /// </summary>
        public async Task ActualizarGraficoGastosAsync()
        {
            await Task.Run(() =>
            {
                GastosChart = _chartService.CrearGraficoGastos(GastosPorPeriodo);
            });

            MainThread.BeginInvokeOnMainThread(() =>
            {
                OnPropertyChanged(nameof(GastosChart));
            });
        }

        /// <summary>
        /// Actualiza SOLO el gráfico de encargos (migración MVVM paso a paso)
        /// </summary>
        public async Task ActualizarGraficoEncargosAsync()
        {
            await Task.Run(() =>
            {
                EncargosChart = _chartService.CrearGraficoEncargos(EncargosPorPeriodo);
            });

            MainThread.BeginInvokeOnMainThread(() =>
            {
                OnPropertyChanged(nameof(EncargosChart));
            });
        }

        /// <summary>
        /// Actualiza SOLO el gráfico de ganancias (migración MVVM paso a paso)
        /// </summary>
        public async Task ActualizarGraficoGananciasAsync()
        {
            await Task.Run(() =>
            {
                GananciasChart = _chartService.CrearGraficoGanancias(VentasPorPeriodo, GastosPorPeriodo);
            });

            MainThread.BeginInvokeOnMainThread(() =>
            {
                OnPropertyChanged(nameof(GananciasChart));
            });
        }

        /// <summary>
        /// Actualiza todos los gráficos (para cuando termine la migración completa)
        /// </summary>
        public async Task ActualizarGraficosAsync()
        {
            await Task.Run(() =>
            {
                EncargosChart = _chartService.CrearGraficoEncargos(EncargosPorPeriodo);
                VentasChart = _chartService.CrearGraficoVentas(VentasPorPeriodo);
                GastosChart = _chartService.CrearGraficoGastos(GastosPorPeriodo);
                GananciasChart = _chartService.CrearGraficoGanancias(VentasPorPeriodo, GastosPorPeriodo);
            });

            MainThread.BeginInvokeOnMainThread(() =>
            {
                OnPropertyChanged(nameof(EncargosChart));
                OnPropertyChanged(nameof(VentasChart));
                OnPropertyChanged(nameof(GastosChart));
                OnPropertyChanged(nameof(GananciasChart));
            });
        }


        public async Task CalcularPorPeriodoAsync()
        {
            // cálculos
            await Task.Run(() => Recalcular());

        }

       


        // ===== Lógica central =====
        public void Recalcular()
        {
            _totalVentasValue = VentasPeriodo.Sum(v => v.Precio * v.Cantidad);
            _totalGastosValue = GastosPeriodo.Sum(g => g.Monto * g.Cantidad);
            _totalEncargosValue = EncargosPeriodo.Sum(e => e.Precio * e.Cantidad);

            _gananciasValue = _totalVentasValue - _totalGastosValue;
            _margenValue = _totalVentasValue == 0
                ? 0
                : (_gananciasValue / _totalVentasValue) * 100;

            // UI
            TotalVentas = _totalVentasValue.ToString("C");
            TotalGastos = _totalGastosValue.ToString("C");
            TotalEncargos = _totalEncargosValue.ToString("C");
            Ganancias = _gananciasValue.ToString("C");
            Margen = $"{_margenValue:F1}%";
        }

        // ===== Filtro por período =====
        private IEnumerable<T> FiltrarPorPeriodo<T>(IEnumerable<T> lista) where T : IFecha
        {
            var hoy = DateTime.Now;

            return PeriodoSeleccionado switch
            {
                "Días" => lista.Where(x => x.Fecha >= hoy.AddDays(-366)),
                "Semanas" => lista.Where(x => x.Fecha >= hoy.AddDays(-910)), // ~130 semanas
                "Meses" => lista.Where(x => x.Fecha >= hoy.AddMonths(-12)),
                "Años" => lista.Where(x => x.Fecha >= hoy.AddYears(-10)),
                _ => lista.Where(x => x.Fecha >= hoy.AddMonths(-12))
            };
        }

        // ===== Export =====
        public BalanceExportDto CrearExportDto()
        {
            return new BalanceExportDto
            {
                Ventas = VentasPeriodo.ToList(),
                Gastos = GastosPeriodo.ToList(),
                Encargos = EncargosPeriodo.ToList(),

                TotalVentas = _totalVentasValue,
                TotalGastos = _totalGastosValue,
                TotalEncargos = _totalEncargosValue,
                Ganancias = _gananciasValue,
                Margen = _margenValue,

                Periodo = PeriodoSeleccionado
            };
        }
    }
}
