using Mercader.Domain.Entities;
using Mercader.Data.Interfaces;

namespace Mercader;

public partial class DetalleVenta : ContentPage
{
    private readonly IDataRepository _repository;
    private Ventas _venta;
    private List<ArticuloVenta> _articulos = new();

    public DetalleVenta(Ventas venta, IDataRepository repository)
    {
        InitializeComponent();
        _venta = venta;
        BindingContext = venta;
        _repository = repository;
        _ = CargarArticulosAsync();
    }

    private async Task CargarArticulosAsync()
    {
        try
        {
            _articulos = await _repository.GetArticulosVentaAsync(_venta.Id);
            BindableLayout.SetItemsSource(ArticulosStack, _articulos);
            CalcularYMostrarTotal();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DetalleVenta] Error cargando artículos: {ex.Message}");
        }
    }

    private void CalcularYMostrarTotal()
    {
        if (_venta != null)
        {
            decimal total = _articulos.Count > 0
                ? _articulos.Sum(a => a.Total)
                : _venta.Precio * _venta.Cantidad;
            LabelTotal.Text = $"${total:F2}";
        }
    }

    private async void OnEditarClicked(object sender, EventArgs e)
    {
        try
        {
            await Navigation.PushAsync(new EditarVentaPage(_venta, _repository));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al abrir la pagina de edicion: {ex.Message}", "OK");
        }
    }

    private async void OnEliminarClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Confirmacion",
            $"Esta seguro de eliminar la venta \"{_venta.Descripcion}\" del dia {_venta.Fecha:dd/MM/yyyy}?",
            "Si", "No");

        if (confirm)
        {
            try
            {
                await _repository.DeleteVentaAsync(_venta);
                await DisplayAlert("Exito", "Venta eliminada correctamente", "OK");
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al eliminar la venta: {ex.Message}", "OK");
            }
        }
    }

    private async void OnVolverClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_venta != null)
        {
            _ = CargarArticulosAsync();
        }
    }
}
