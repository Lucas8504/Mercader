using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mercader.Domain.Entities;
using Mercader.Data.Interfaces;

namespace Mercader.ViewModels
{
    public partial class GastosViewModel : BaseViewModel
    {
        private readonly IDataRepository _repository;
        private List<Gasto> _todosLosGastos = new();

        [ObservableProperty]
        private ObservableCollection<Gasto> _gastos = new();

        [ObservableProperty]
        private string _textoBusqueda = string.Empty;

        [ObservableProperty]
        private string _filtroPeriodo = "TODOS";

        public GastosViewModel(IDataRepository repository)
        {
            _repository = repository;
        }

        partial void OnTextoBusquedaChanged(string value) => AplicarFiltros();
        partial void OnFiltroPeriodoChanged(string value) => AplicarFiltros();

        [RelayCommand]
        public async Task CargarGastosAsync()
        {
            await ExecuteBusyAsync(async () =>
            {
                var lista = await _repository.GetGastosAsync();
                _todosLosGastos = lista;
                AplicarFiltros();
            });
        }

        [RelayCommand]
        public async Task EliminarGastoAsync(Gasto gasto)
        {
            if (gasto is null) return;

            await ExecuteBusyAsync(async () =>
            {
                await _repository.DeleteGastoAsync(gasto);
                _todosLosGastos.Remove(gasto);
                AplicarFiltros();
            });
        }

        private void AplicarFiltros()
        {
            var resultado = _todosLosGastos.AsEnumerable();

            if (FiltroPeriodo == "HOY")
                resultado = resultado.Where(g => g.Fecha.Date == DateTime.Today);
            else if (FiltroPeriodo == "ESTE MES")
                resultado = resultado.Where(g =>
                    g.Fecha.Year == DateTime.Today.Year &&
                    g.Fecha.Month == DateTime.Today.Month);

            if (!string.IsNullOrWhiteSpace(TextoBusqueda))
            {
                var busqueda = TextoBusqueda.Trim().ToLowerInvariant();
                resultado = resultado.Where(g =>
                    g.Descripcion != null &&
                    g.Descripcion.ToLowerInvariant().Contains(busqueda));
            }

            Gastos = new ObservableCollection<Gasto>(resultado);
        }
    }
}
