using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mercader.Domain.Entities;
using Mercader.Data.Interfaces;

namespace Mercader.ViewModels
{
    public partial class VentasViewModel : BaseViewModel
    {
        private readonly IDataRepository _repository;
        private List<Ventas> _todasLasVentas = new();

        [ObservableProperty]
        private ObservableCollection<Ventas> _ventas = new();

        [ObservableProperty]
        private string _textoBusqueda = string.Empty;

        [ObservableProperty]
        private string _filtroPeriodo = "TODOS";

        public VentasViewModel(IDataRepository repository)
        {
            _repository = repository;
        }

        partial void OnTextoBusquedaChanged(string value) => AplicarFiltros();
        partial void OnFiltroPeriodoChanged(string value) => AplicarFiltros();

        [RelayCommand]
        public async Task CargarVentasAsync()
        {
            await ExecuteBusyAsync(async () =>
            {
                var lista = await _repository.GetVentasAsync();
                _todasLasVentas = lista;
                AplicarFiltros();
            });
        }

        [RelayCommand]
        public async Task EliminarVentaAsync(Ventas venta)
        {
            if (venta is null) return;

            await ExecuteBusyAsync(async () =>
            {
                await _repository.DeleteVentaAsync(venta);
                _todasLasVentas.Remove(venta);
                AplicarFiltros();
            });
        }

        private void AplicarFiltros()
        {
            var resultado = _todasLasVentas.AsEnumerable();

            if (FiltroPeriodo == "HOY")
                resultado = resultado.Where(v => v.Fecha.Date == DateTime.Today);
            else if (FiltroPeriodo == "ESTE MES")
                resultado = resultado.Where(v =>
                    v.Fecha.Year == DateTime.Today.Year &&
                    v.Fecha.Month == DateTime.Today.Month);

            if (!string.IsNullOrWhiteSpace(TextoBusqueda))
            {
                var busqueda = TextoBusqueda.Trim().ToLowerInvariant();
                resultado = resultado.Where(v =>
                    v.Descripcion != null &&
                    v.Descripcion.ToLowerInvariant().Contains(busqueda));
            }

            Ventas = new ObservableCollection<Ventas>(resultado);
        }
    }
}
