namespace Mercader;

public partial class DetalleEncargo : ContentPage
{
    private Encargo _encargo;

    public DetalleEncargo(Encargo encargo)
    {
        InitializeComponent();
        _encargo = encargo;
        BindingContext = _encargo;
        CalcularTotal();
    }

    private void CalcularTotal()
    {
        if (_encargo != null)
        {
            var total = _encargo.Precio * _encargo.Cantidad;
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

        if (confirm)
        {
            try
            {
                var venta = new Ventas
                {
                    Descripcion = _encargo.Descripcion,
                    Precio = _encargo.Precio,
                    Cantidad = _encargo.Cantidad,
                    Fecha = DateTime.Now
                };

                await App.DataRepo.SaveVentasAsync(venta);
                await App.DataRepo.DeleteEncargoAsync(_encargo);

                await DisplayAlert("Éxito", "Venta concretada correctamente", "OK");
                await Navigation.PopAsync(); // Volver a la página anterior
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al concretar la venta: {ex.Message}", "OK");
            }
        }
    }

    private async void OnEditarClicked(object sender, EventArgs e)
    {
        try
        {
            await Navigation.PushAsync(new EditarEncargoPage(_encargo));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al abrir la página de edición: {ex.Message}", "OK");
        }
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
                await App.DataRepo.DeleteEncargoAsync(_encargo);
                await DisplayAlert("Éxito", "Encargo eliminado correctamente", "OK");
                await Navigation.PopAsync(); // Volver a la página anterior
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