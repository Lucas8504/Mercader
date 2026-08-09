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

        // ===== MULTI-ARTÍCULO =====

        public ObservableCollection<ArticuloEncargo> Articulos { get; } = new();

        [ObservableProperty]
        private decimal _total;

        [RelayCommand]
        private void AgregarArticulo()
        {
            var nuevo = new ArticuloEncargo { Orden = Articulos.Count + 1 };
            Articulos.Add(nuevo);
            RecalcularTotal();
        }

        [RelayCommand]
        private void EliminarArticulo(ArticuloEncargo? articulo)
        {
            if (articulo is null) return;
            Articulos.Remove(articulo);
            Reordenar();
            RecalcularTotal();
        }

        [RelayCommand]
        private void SubirArticulo(ArticuloEncargo? articulo)
        {
            if (articulo is null) return;
            var idx = Articulos.IndexOf(articulo);
            if (idx <= 0) return;
            Articulos.Move(idx, idx - 1);
            Reordenar();
        }

        [RelayCommand]
        private void BajarArticulo(ArticuloEncargo? articulo)
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
                System.Diagnostics.Debug.WriteLine($"[ENCARGOS] Cargados {lista.Count} encargos de la DB");

                // Cargar artículos para cada encargo (multi-artículo)
                var articuloRepo = _repository.GetArticleRepository<ArticuloEncargo>();
                foreach (var encargo in lista)
                {
                    try
                    {
                        encargo.Articulos = await articuloRepo.GetByParentIdAsync(encargo.Id);
                        System.Diagnostics.Debug.WriteLine($"[ENCARGOS] EncargoId={encargo.Id} '{encargo.Nombre}' → {encargo.Articulos.Count} artículos, TieneArticulos={encargo.TieneArticulos}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ENCARGOS] ERROR cargando artículos para EncargoId={encargo.Id}: {ex.Message}");
                        encargo.Articulos = new List<Domain.Entities.ArticuloEncargo>();
                    }
                }

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
                // Obtener artículos del encargo
                var articuloEncargoRepo = _repository.GetArticleRepository<ArticuloEncargo>();
                var articulosEncargo = await articuloEncargoRepo.GetByParentIdAsync(encargo.Id);

                var venta = new Ventas
                {
                    Descripcion = $"Venta de: {encargo.Nombre} - {encargo.Descripcion}",
                    Precio = articulosEncargo.Sum(a => a.Total),
                    Cantidad = 1,
                    Fecha = DateTime.Now
                };

                await _repository.SaveVentasAsync(venta);

                // Transferir artículos del encargo a la venta
                var articuloVentaRepo = _repository.GetArticleRepository<ArticuloVenta>();
                foreach (var ae in articulosEncargo)
                {
                    var av = new ArticuloVenta
                    {
                        VentaId = venta.Id,
                        Descripcion = ae.Descripcion,
                        PrecioUnitario = ae.PrecioUnitario,
                        Cantidad = ae.Cantidad,
                        Orden = ae.Orden
                    };
                    await articuloVentaRepo.SaveAsync(av);
                }

                // Soft-delete artículos del encargo y el encargo
                foreach (var ae in articulosEncargo)
                    await articuloEncargoRepo.DeleteAsync(ae);

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
                // Obtener artículos del encargo
                var articuloEncargoRepo = _repository.GetArticleRepository<ArticuloEncargo>();
                var articulosEncargo = await articuloEncargoRepo.GetByParentIdAsync(encargo.Id);

                // Crear venta a partir del encargo
                var descripcionVenta = string.IsNullOrWhiteSpace(encargo.Descripcion)
                    ? encargo.Nombre
                    : $"{encargo.Descripcion} ({encargo.Nombre})";

                var venta = new Ventas
                {
                    Descripcion = descripcionVenta,
                    Precio = articulosEncargo.Sum(a => a.Total),
                    Cantidad = 1,
                    Fecha = DateTime.Now
                };

                await _repository.SaveVentasAsync(venta);

                // Transferir artículos del encargo a la venta
                var articuloVentaRepo = _repository.GetArticleRepository<ArticuloVenta>();
                foreach (var ae in articulosEncargo)
                {
                    var av = new ArticuloVenta
                    {
                        VentaId = venta.Id,
                        Descripcion = ae.Descripcion,
                        PrecioUnitario = ae.PrecioUnitario,
                        Cantidad = ae.Cantidad,
                        Orden = ae.Orden
                    };
                    await articuloVentaRepo.SaveAsync(av);
                }

                // Marcar encargo como entregado
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
