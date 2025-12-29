using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Mercader
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<string> Periodos { get; } =
        new() { "Días", "Semanas", "Meses" };

        public Action<string>? OnPeriodoChanged;


        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private string _totalVentas = "$0";
        public string TotalVentas
        {
            get => _totalVentas;
            set
            {
                _totalVentas = value;
                OnPropertyChanged();
            }
        }

        private string _totalGastos = "$0";
        public string TotalGastos
        {
            get => _totalGastos;
            set
            {
                _totalGastos = value;
                OnPropertyChanged();
            }
        }

        private string _totalEncargos = "$0";
        public string TotalEncargos
        {
            get => _totalEncargos;
            set
            {
                _totalEncargos = value;
                OnPropertyChanged();
            }
        }

        private string _ganancias = "$0";
        public string Ganancias
        {
            get => _ganancias;
            set
            {
                _ganancias = value;
                OnPropertyChanged();
            }
        }

        private string? _margen;
        public string? Margen
        {
            get => _margen;
            set { _margen = value; OnPropertyChanged(); }
        }

        private string _periodoSeleccionado = "Meses";
        public string PeriodoSeleccionado
        {
            get => _periodoSeleccionado;
            set
            {
                if (_periodoSeleccionado == value)
                    return;

                _periodoSeleccionado = value;
                OnPropertyChanged();

                OnPeriodoChanged?.Invoke(value);
            }
        }

    }
}
