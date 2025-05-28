namespace Mercader;

public partial class DetalleGasto : ContentPage
{
    private Gasto _gasto;

    public DetalleGasto(Gasto gasto)
    {
        InitializeComponent();
        _gasto = gasto;
        BindingContext = gasto;
        CalcularYMostrarTotal();
    }

    private void CalcularYMostrarTotal()
    {
        if (_gasto != null)
        {
            decimal total = _gasto.Monto * _gasto.Cantidad;
            LabelTotal.Text = $"${total:F2}";
        }
    }

    private async void OnEditarClicked(object sender, EventArgs e)
    {
        try
        {
            await Navigation.PushAsync(new EditarGastoPage(_gasto));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al abrir la página de edición: {ex.Message}", "OK");
        }
    }

    private async void OnEliminarClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Confirmación",
            $"¿Estás seguro de eliminar el gasto \"{_gasto.Descripcion}\" del día {_gasto.Fecha:dd/MM/yyyy}?",
            "Sí", "No");

        if (confirm)
        {
            try
            {
                await App.DataRepo.DeleteGastoAsync(_gasto);
                await DisplayAlert("Éxito", "Gasto eliminado correctamente", "OK");

                // Volver a la página anterior
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al eliminar el gasto: {ex.Message}", "OK");
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
        if (_gasto != null)
        {
            CalcularYMostrarTotal();
        }
    }
}