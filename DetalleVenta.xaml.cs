namespace Mercader;

public partial class DetalleVenta : ContentPage
{
    private Ventas _venta;

    public DetalleVenta(Ventas venta)
    {
        InitializeComponent();
        _venta = venta;
        BindingContext = venta;
        CalcularYMostrarTotal();
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
            await Navigation.PushAsync(new EditarVentaPage(_venta));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al abrir la página de edición: {ex.Message}", "OK");
        }
    }

    private async void OnEliminarClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Confirmación",
            $"¿Estás seguro de eliminar la venta \"{_venta.Descripcion}\" del día {_venta.Fecha:dd/MM/yyyy}?",
            "Sí", "No");

        if (confirm)
        {
            try
            {
                await App.DataRepo.DeleteVentaAsync(_venta);
                await DisplayAlert("Éxito", "Venta eliminada correctamente", "OK");

                // Volver a la página anterior
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