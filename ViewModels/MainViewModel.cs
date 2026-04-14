using Mercader.Models;
using Mercader.Models.Domain;
using Mercader.Services.Interfaces;
using Microcharts;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;

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

        // ===== Services =====
        private readonly IDataRepository _dataRepository;
        private readonly IChartService _chartService;

        // ===== Gráficos =====
        public Chart? EncargosChart { get; private set; }
        public Chart? VentasChart { get; private set; }
        public Chart? GastosChart { get; private set; }
        public Chart? GananciasChart { get; private set; }

        // ===== Valores internos =====
        private decimal _totalVentasValue;
        private decimal _totalGastosValue;
        private decimal _totalEncargosValue;
        private decimal _gananciasValue;
        private decimal _margenValue;

        // ===== Propiedades para UI =====
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

        // ===== Loading state =====
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
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
                if (SetProperty(ref _periodoSeleccionado, value))
                {
                    RefreshCharts();
                }
            }
        }

        // ===== Commands =====
        public ICommand LoadDataCommand { get; }
        public ICommand RefreshCommand { get; }

        // ===== Constructor =====
        public MainViewModel(IDataRepository dataRepository, IChartService chartService)
        {
            _dataRepository = dataRepository;
            _chartService = chartService;

            // Subscribe to collection changes
            Ventas.CollectionChanged += (_, __) => Recalcular();
            Gastos.CollectionChanged += (_, __) => Recalcular();
            Encargos.CollectionChanged += (_, __) => Recalcular();

            // Initialize commands
            LoadDataCommand = new Command(async () => await LoadDataAsync());
            RefreshCommand = new Command(RefreshCharts);
        }

        // ===== Métodos de Carga =====
        public async Task LoadDataAsync()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;

                var encargos = await _dataRepository.GetEncargosAsync();
                var gastos = await _dataRepository.GetGastosAsync();
                var ventas = await _dataRepository.GetVentasAsync();

                // Update collections
                Encargos.Clear();
                foreach (var e in encargos) Encargos.Add(e);

                Gastos.Clear();
                foreach (var g in gastos) Gastos.Add(g);

                Ventas.Clear();
                foreach (var v in ventas) Ventas.Add(v);

                Recalcular();
                RefreshCharts();
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ===== Refresh Charts =====
        public void RefreshCharts()
        {
            // Agrupar datos por período
            VentasPorPeriodo = AgruparPorPeriodo(VentasPeriodo, p => p.Fecha);
            GastosPorPeriodo = AgruparPorPeriodo(GastosPeriodo, p => p.Fecha);
            EncargosPorPeriodo = AgruparPorPeriodo(EncargosPeriodo, p => p.Fecha);

            // Generar gráficos
            EncargosChart = _chartService.CrearGraficoEncargos(EncargosPorPeriodo);
            VentasChart = _chartService.CrearGraficoVentas(VentasPorPeriodo);
            GastosChart = _chartService.CrearGraficoGastos(GastosPorPeriodo);
            GananciasChart = _chartService.CrearGraficoGanancias(VentasPorPeriodo, GastosPorPeriodo);

            // Notify UI
            OnPropertyChanged(nameof(EncargosChart));
            OnPropertyChanged(nameof(VentasChart));
            OnPropertyChanged(nameof(GastosChart));
            OnPropertyChanged(nameof(GananciasChart));
        }

        // ===== Datos agrupados por período =====
        public List<(string Periodo, decimal Total)> VentasPorPeriodo { get; private set; } = new();
        public List<(string Periodo, decimal Total)> GastosPorPeriodo { get; private set; } = new();
        public List<(string Periodo, decimal Total)> EncargosPorPeriodo { get; private set; } = new();

        // ===== Lógica de Agrupación Genérica =====
        private List<(string Periodo, decimal Total)> AgruparPorPeriodo<T>(
            IEnumerable<T> datos, 
            Func<T, DateTime> getFecha) where T : IFecha
        {
            var hoy = DateTime.Today;

            return PeriodoSeleccionado switch
            {
                "Días" => AgruparPorDias(datos, getFecha, hoy),
                "Semanas" => AgruparPorSemanas(datos, getFecha, hoy),
                "Meses" => AgruparPorMeses(datos, getFecha, hoy),
                "Años" => AgruparPorAnios(datos, getFecha, hoy),
                _ => AgruparPorMeses(datos, getFecha, hoy)
            };
        }

        private List<(string Periodo, decimal Total)> AgruparPorDias<T>(
            IEnumerable<T> datos, 
            Func<T, DateTime> getFecha,
            DateTime hoy) where T : IFecha
        {
            var ultimosDias = Enumerable.Range(0, 133)
                .Select(i => hoy.AddDays(-i))
                .Reverse()
                .ToList();

            return ultimosDias.Select(fecha =>
            {
                var total = datos.Where(x => getFecha(x).Date == fecha.Date)
                    .Sum(x => GetTotalValue(x));
                return (fecha.ToString("dd/MM"), total);
            }).ToList();
        }

        private List<(string Periodo, decimal Total)> AgruparPorSemanas<T>(
            IEnumerable<T> datos, 
            Func<T, DateTime> getFecha,
            DateTime hoy) where T : IFecha
        {
            var ultimasSemanas = Enumerable.Range(0, 130)
                .Select(i =>
                {
                    var inicioSemana = hoy.AddDays(-7 * i).AddDays(-(int)hoy.AddDays(-7 * i).DayOfWeek);
                    return new { Inicio = inicioSemana, Fin = inicioSemana.AddDays(6) };
                })
                .Reverse()
                .ToList();

            return ultimasSemanas.Select(semana =>
            {
                var total = datos.Where(x => getFecha(x).Date >= semana.Inicio && getFecha(x).Date <= semana.Fin)
                    .Sum(x => GetTotalValue(x));
                return ($"{semana.Inicio:dd/MM}", total);
            }).ToList();
        }

        private List<(string Periodo, decimal Total)> AgruparPorMeses<T>(
            IEnumerable<T> datos, 
            Func<T, DateTime> getFecha,
            DateTime hoy) where T : IFecha
        {
            var ultimosMeses = Enumerable.Range(0, 130)
                .Select(i => hoy.AddMonths(-i))
                .Reverse()
                .ToList();

            return ultimosMeses.Select(mes =>
            {
                var total = datos.Where(x => getFecha(x).Year == mes.Year && getFecha(x).Month == mes.Month)
                    .Sum(x => GetTotalValue(x));
                return (mes.ToString("MMM", new CultureInfo("es-ES")), total);
            }).ToList();
        }

        private List<(string Periodo, decimal Total)> AgruparPorAnios<T>(
            IEnumerable<T> datos, 
            Func<T, DateTime> getFecha,
            DateTime hoy) where T : IFecha
        {
            var ultimosAnios = Enumerable.Range(0, 10)
                .Select(i => hoy.AddYears(-i))
                .Reverse()
                .ToList();

            return ultimosAnios.Select(anio =>
            {
                var total = datos.Where(x => getFecha(x).Year == anio.Year)
                    .Sum(x => GetTotalValue(x));
                return (anio.Year.ToString(), total);
            }).ToList();
        }

        private decimal GetTotalValue<T>(T item) where T : IFecha
        {
            return item switch
            {
                Ventas v => v.Precio * v.Cantidad,
                Gasto g => g.Monto * g.Cantidad,
                Encargo e => e.Precio * e.Cantidad,
                _ => 0
            };
        }

        // ===== Recalcular Totales =====
        private void Recalcular()
        {
            _totalVentasValue = VentasPeriodo.Sum(v => v.Precio * v.Cantidad);
            _totalGastosValue = GastosPeriodo.Sum(g => g.Monto * g.Cantidad);
            _totalEncargosValue = EncargosPeriodo.Sum(e => e.Precio * e.Cantidad);

            _gananciasValue = _totalVentasValue - _totalGastosValue;
            _margenValue = _totalVentasValue == 0
                ? 0
                : (_gananciasValue / _totalVentasValue) * 100;

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
                "Semanas" => lista.Where(x => x.Fecha >= hoy.AddDays(-910)),
                "Meses" => lista.Where(x => x.Fecha >= hoy.AddMonths(-12)),
                "Años" => lista.Where(x => x.Fecha >= hoy.AddYears(-10)),
                _ => lista.Where(x => x.Fecha >= hoy.AddMonths(-12))
            };
        }

        // ===== CRUD Operations =====

        public async Task AddEncargoAsync(Encargo encargo)
        {
            await _dataRepository.SaveEncargoAsync(encargo);
            Encargos.Add(encargo);
        }

        public async Task AddVentaAsync(Ventas venta)
        {
            await _dataRepository.SaveVentasAsync(venta);
            Ventas.Add(venta);
        }

        public async Task AddGastoAsync(Gasto gasto)
        {
            await _dataRepository.SaveGastoAsync(gasto);
            Gastos.Add(gasto);
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

    // ===== Simple Command implementation =====
    public class Command : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public Command(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
        public void Execute(object? parameter) => _execute();

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    // ===== Async Command implementation =====
    public class AsyncCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool>? _canExecute;
        private bool _isExecuting;

        public AsyncCommand(Func<Task> execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => !_isExecuting && (_canExecute?.Invoke() ?? true);

        public async void Execute(object? parameter)
        {
            if (_isExecuting) return;

            _isExecuting = true;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);

            try
            {
                await _execute();
            }
            finally
            {
                _isExecuting = false;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
