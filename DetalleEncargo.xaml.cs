using Mercader.Domain.Entities;
using Mercader.Data.Interfaces;

namespace Mercader;

public partial class DetalleEncargo : ContentPage
{
    private readonly IDataRepository _repository;
    private Encargo _encargo;
    private List<ArticuloEncargo> _articulos = new();

    public DetalleEncargo(Encargo encargo, IDataRepository repository)
    {
        InitializeComponent();
        _encargo = encargo;
        _repository = repository;
        BindingContext = _encargo;
        _ = CargarArticulosAsync();
    }

    private async Task CargarArticulosAsync()
    {
        try
        {
            _articulos = await _repository.GetArticulosEncargoAsync(_encargo.Id);
            BindableLayout.SetItemsSource(ArticulosStack, _articulos);
            CalcularTotal();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DetalleEncargo] Error cargando artículos: {ex.Message}");
        }
    }

    private void CalcularTotal()
    {
        if (_encargo != null)
        {
            decimal total = _articulos.Count > 0
                ? _articulos.Sum(a => a.Total)
                : _encargo.Precio * _encargo.Cantidad;
            LabelTotal.Text = $"${total:F2}";
        }
    }

    private async void OnConcretarVentaClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert(
            "Confirmación",
            $"¿Realmente deseas concretar la venta de {_encargo.Descripcion} para \"{_encargo.Nombre}\" pedido el día: {_encargo.Fecha:dd/MM/yyyy}?",
            "Sí",
            "No");

        if (!confirm) return;

        try
        {
            // Recargar artículos frescos de la BD
            _articulos = await _repository.GetArticulosEncargoAsync(_encargo.Id);

            var venta = new Ventas
            {
                Descripcion = $"Venta de: {_encargo.Nombre} - {_encargo.Descripcion}",
                Precio = _articulos.Sum(a => a.Total),
                Cantidad = 1,
                Fecha = DateTime.Now
            };

            await _repository.SaveVentasAsync(venta);

            // Transferir artículos del encargo a la venta
            foreach (var ae in _articulos)
            {
                var av = new ArticuloVenta
                {
                    VentaId = venta.Id,
                    Descripcion = ae.Descripcion,
                    PrecioUnitario = ae.PrecioUnitario,
                    Cantidad = ae.Cantidad,
                    Orden = ae.Orden
                };
                await _repository.SaveArticuloVentaAsync(av);
            }

            // Soft-delete artículos del encargo y el encargo
            foreach (var ae in _articulos)
                await _repository.DeleteArticuloEncargoAsync(ae);

            await _repository.DeleteEncargoAsync(_encargo);

            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void OnEditarClicked(object sender, EventArgs e)
    {
        try
        {
            await Navigation.PushAsync(new EditarEncargoPage(_encargo, _repository));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al abrir la página de edición: {ex.Message}", "OK");
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = CargarArticulosAsync();
    }

    private async void OnEliminarClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert(
            "Confirmación",
            $"¿Realmente deseas eliminar el encargo de \"{_encargo.Nombre}\" hecho el día: {_encargo.Fecha:dd/MM/yyyy}?",
            "Sí",
            "No");

        if (confirm)
        {
            try
            {
                // Soft-delete artículos del encargo
                var articulos = await _repository.GetArticulosEncargoAsync(_encargo.Id);
                foreach (var ae in articulos)
                    await _repository.DeleteArticuloEncargoAsync(ae);

                await _repository.DeleteEncargoAsync(_encargo);
                await DisplayAlert("Éxito", "Encargo eliminado correctamente", "OK");
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al eliminar el encargo: {ex.Message}", "OK");
            }
        }
    }

    private async void OnVolverClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}