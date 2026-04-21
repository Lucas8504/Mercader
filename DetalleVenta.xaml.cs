using Mercader.Domain.Entities;
using Mercader.Data.Interfaces;

namespace Mercader;

public partial class DetalleVenta : ContentPage
{
    private readonly IDataRepository _repository;
    private Ventas _venta;

    public DetalleVenta(Ventas venta, IDataRepository repository)
    {
        InitializeComponent();
        _venta = venta;
        BindingContext = venta;
        CalcularYMostrarTotal();
        _repository = repository;
    }

    private void CalcularYMostrarTotal()
    {
        if (_venta != null)
        {
            decimal total = _venta.Precio * _venta.Cantidad;
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
            await DisplayAlert("Error", $"Error al abrir la p�gina de edici�n: {ex.Message}", "OK");
        }
    }

    private async void OnEliminarClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Confirmaci�n",
            $"�Est�s seguro de eliminar la venta \"{_venta.Descripcion}\" del d�a {_venta.Fecha:dd/MM/yyyy}?",
            "S�", "No");

        if (confirm)
        {
            try
            {
                await _repository.DeleteVentaAsync(_venta);
                await DisplayAlert("�xito", "Venta eliminada correctamente", "OK");

                // Volver a la p�gina anterior
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

        // Actualizar los datos en caso de que hayan sido modificados
        if (_venta != null)
        {
            CalcularYMostrarTotal();
        }
    }
}