using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mercader.Domain.Entities;
using Mercader.Data.Interfaces;

namespace Mercader.ViewModels
{
    public partial class EncargosViewModel : BaseViewModel
    {
        private readonly IDataRepository _repository;
        private List<Encargo> _todosLosEncargos = new();

        [ObservableProperty]
        private ObservableCollection<Encargo> _encargos = new();

        [ObservableProperty]
        private string _textoBusqueda = string.Empty;

        [ObservableProperty]
        private string _filtroEstado = "TODOS";

        public EncargosViewModel(IDataRepository repository)
        {
            _repository = repository;
        }

        partial void OnTextoBusquedaChanged(string value) => AplicarFiltros();
        partial void OnFiltroEstadoChanged(string value) => AplicarFiltros();

        [RelayCommand]
        public async Task CargarEncargosAsync()
        {
            await ExecuteBusyAsync(async () =>
            {
                var lista = await _repository.GetEncargosAsync();
                _todosLosEncargos = lista;
                AplicarFiltros();
            });
        }

        [RelayCommand]
        public async Task EliminarEncargoAsync(Encargo encargo)
        {
            if (encargo is null) return;

            await ExecuteBusyAsync(async () =>
            {
                await _repository.DeleteEncargoAsync(encargo);
                _todosLosEncargos.Remove(encargo);
                AplicarFiltros();
            });
        }

        [RelayCommand]
        public async Task ConvertirEnVentaAsync(Encargo encargo)
        {
            if (encargo is null) return;

            await ExecuteBusyAsync(async () =>
            {
                var venta = new Ventas
                {
                    Descripcion = $"Venta de: {encargo.Nombre} - {encargo.Descripcion}",
                    Precio = encargo.Precio,
                    Cantidad = encargo.Cantidad,
                    Fecha = DateTime.Now
                };

                await _repository.SaveVentasAsync(venta);
                await _repository.DeleteEncargoAsync(encargo);
                _todosLosEncargos.Remove(encargo);
                AplicarFiltros();
            });
        }

        [RelayCommand]
        public async Task MarcarEntregadoAsync(Encargo encargo)
        {
            if (encargo is null) return;

            await ExecuteBusyAsync(async () =>
            {
                encargo.Estado = "ENTREGADO";
                await _repository.SaveEncargoAsync(encargo);
                AplicarFiltros();
            });
        }

        private void AplicarFiltros()
        {
            var resultado = _todosLosEncargos.AsEnumerable();

            if (FiltroEstado == "PENDIENTES")
                resultado = resultado.Where(e => e.Estado == "PENDIENTE");
            else if (FiltroEstado == "ENTREGADOS")
                resultado = resultado.Where(e => e.Estado == "ENTREGADO");

            if (!string.IsNullOrWhiteSpace(TextoBusqueda))
            {
                var busqueda = TextoBusqueda.Trim().ToLowerInvariant();
                resultado = resultado.Where(e =>
                    (e.Nombre != null && e.Nombre.ToLowerInvariant().Contains(busqueda)) ||
                    (e.Descripcion != null && e.Descripcion.ToLowerInvariant().Contains(busqueda)));
            }

            Encargos = new ObservableCollection<Encargo>(resultado);
        }
    }
}
