using Mercader.ViewModels;
using System.Collections.ObjectModel;

namespace Mercader.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly DataRepository _repo;

        public ObservableCollection<Gasto> Gastos { get; } = new();
        public ObservableCollection<Encargo> Encargos { get; } = new();
        public ObservableCollection<Ventas> Ventas { get; } = new();

        private decimal _ganancias;
        public decimal Ganancias
        {
            get => _ganancias;
            set => SetProperty(ref _ganancias, value);
        }

        public MainViewModel(DataRepository repo)
        {
            _repo = repo;
            _ = CargarDatosAsync();
        }

        private async Task CargarDatosAsync()
        {
            var gastos = await _repo.GetGastosAsync();
            var encargos = await _repo.GetEncargosAsync();
            var ventas = await _repo.GetVentasAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Gastos.Clear();
                foreach (var g in gastos) Gastos.Add(g);

                Encargos.Clear();
                foreach (var e in encargos) Encargos.Add(e);

                Ventas.Clear();
                foreach (var v in ventas) Ventas.Add(v);

                CalcularGanancias();
            });
        }

        private void CalcularGanancias()
        {
            Ganancias =
                Ventas.Sum(v => v.Precio * v.Cantidad) -
                Gastos.Sum(g => g.Monto * g.Cantidad);
        }
    }
}
