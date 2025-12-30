using Mercader;
using Mercader.ViewModels;
using System.Collections.ObjectModel;

public class MainViewModel : BaseViewModel
{
    // ===== Datos crudos =====
    public List<Ventas> Ventas { get; set; } = new();
    public List<Gasto> Gastos { get; set; } = new();
    public List<Encargo> Encargos { get; set; } = new();

    public interface IFecha
    {
        DateTime Fecha { get; }
    }

    // ===== Picker =====
    public List<string> Periodos { get; } = new()
    {
        "Días",
        "Semanas",
        "Meses"
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

    // ===== Totales =====
    private string? _totalVentas;
    public string? TotalVentas
    {
        get => _totalVentas;
        set { _totalVentas = value; OnPropertyChanged(); }
    }

    private string? _totalGastos;
    public string? TotalGastos
    {
        get => _totalGastos;
        set { _totalGastos = value; OnPropertyChanged(); }
    }

    private string? _totalEncargos;
    public string? TotalEncargos
    {
        get => _totalEncargos;
        set { _totalEncargos = value; OnPropertyChanged(); }
    }

    private string? _ganancias;
    public string? Ganancias
    {
        get => _ganancias;
        set { _ganancias = value; OnPropertyChanged(); }
    }

    private string? _margen;
    public string? Margen
    {
        get => _margen;
        set { _margen = value; OnPropertyChanged(); }
    }

    // ===== Comunicación con la vista =====
    public Action? OnPeriodoChanged;

    // ===== Lógica central =====
    public void Recalcular()
    {
        var ventasPeriodo = FiltrarPorPeriodo(Ventas).Sum(v => v.Precio * v.Cantidad);
        var gastosPeriodo = FiltrarPorPeriodo(Gastos).Sum(g => g.Monto * g.Cantidad);
        var encargosPeriodo = FiltrarPorPeriodo(Encargos).Sum(e => e.Precio * e.Cantidad);

        var ganancias = ventasPeriodo - gastosPeriodo;
        var margen = ventasPeriodo == 0 ? 0 : (ganancias / ventasPeriodo) * 100;

        TotalVentas = ventasPeriodo.ToString("C");
        TotalGastos = gastosPeriodo.ToString("C");
        TotalEncargos = encargosPeriodo.ToString("C");
        Ganancias = ganancias.ToString("C");
        Margen = $"{margen:F1}%";
    }

    private IEnumerable<T> FiltrarPorPeriodo<T>(IEnumerable<T> lista) where T : IFecha
    {
        var hoy = DateTime.Now;

        return PeriodoSeleccionado switch
        {
            "Días" => lista.Where(x => x.Fecha.Date == hoy.Date),
            "Semanas" => lista.Where(x => x.Fecha >= hoy.AddDays(-7)),
            _ => lista.Where(x => x.Fecha >= hoy.AddMonths(-1))
        };
    }
}


