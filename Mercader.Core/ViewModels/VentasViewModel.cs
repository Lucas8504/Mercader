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

        // ===== MULTI-ARTÍCULO =====

        public ObservableCollection<ArticuloVenta> Articulos { get; } = new();

        [ObservableProperty]
        private decimal _total;

        [RelayCommand]
        private void AgregarArticulo()
        {
            var nuevo = new ArticuloVenta { Orden = Articulos.Count + 1 };
            Articulos.Add(nuevo);
            RecalcularTotal();
        }

        [RelayCommand]
        private void EliminarArticulo(ArticuloVenta? articulo)
        {
            if (articulo is null) return;
            Articulos.Remove(articulo);
            Reordenar();
            RecalcularTotal();
        }

        [RelayCommand]
        private void SubirArticulo(ArticuloVenta? articulo)
        {
            if (articulo is null) return;
            var idx = Articulos.IndexOf(articulo);
            if (idx <= 0) return;
            Articulos.Move(idx, idx - 1);
            Reordenar();
        }

        [RelayCommand]
        private void BajarArticulo(ArticuloVenta? articulo)
        {
            if (articulo is null) return;
            var idx = Articulos.IndexOf(articulo);
            if (idx < 0 || idx >= Articulos.Count - 1) return;
            Articulos.Move(idx, idx + 1);
            Reordenar();
        }

        private void Reordenar()
        {
            for (int i = 0; i < Articulos.Count; i++)
                Articulos[i].Orden = i + 1;
        }

        public void RecalcularTotal()
        {
            Total = Articulos.Sum(a => a.Total);
        }

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
