namespace Mercader;

public partial class Gastos : ContentPage
{
    public Gastos()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        CargarGastos();
    }

    private async void CargarGastos()
    {
        try
        {
            var gastos = await App.DataRepo.GetGastosAsync();
            GastosCollectionView.ItemsSource = gastos;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al cargar los gastos: {ex.Message}");
        }
    }

    // Método para manejar el tap en lugar de selección (si necesitas navegación a detalles)
    private async void OnItemTapped(object sender, EventArgs e)
    {
        var frame = sender as Frame;
        var gasto = frame?.BindingContext as Gasto;

        if (gasto != null)
        {
            // Aquí puedes navegar a una página de detalles si la tienes
            // await Navigation.PushAsync(new DetalleGasto(gasto));

            // O mostrar información del gasto
            await DisplayAlert("Detalle del Gasto",
                $"Descripción: {gasto.Descripcion}\nMonto: ${gasto.Monto:F2}\nCantidad: {gasto.Cantidad}\nFecha: {gasto.Fecha:dd/MM/yyyy}",
                "OK");
        }
    }

    // Métodos para los SwipeItems
    private async void OnEditSwipeItemInvoked(object sender, EventArgs e)
    {
        var swipeItem = sender as SwipeItem;
        var item = swipeItem?.BindingContext;
        if (item is Gasto gasto)
        {
            await EditarGasto(gasto);
        }
    }

    private async void OnDeleteSwipeItemInvoked(object sender, EventArgs e)
    {
        var swipeItem = sender as SwipeItem;
        var item = swipeItem?.BindingContext;
        if (item is Gasto gasto)
        {
            await EliminarGasto(gasto);
        }
    }

    // Métodos auxiliares
    private async Task EditarGasto(Gasto gasto)
    {
        try
        {
            await Navigation.PushAsync(new EditarGastoPage(gasto));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al abrir la página de edición: {ex.Message}", "OK");
        }
    }

    private async Task EliminarGasto(Gasto gasto)
    {
        bool confirm = await DisplayAlert("Confirmación",
            $"¿Estás seguro de eliminar el gasto \"{gasto.Descripcion}\" del día {gasto.Fecha:dd/MM/yyyy}?",
            "Sí", "No");

        if (confirm)
        {
            try
            {
                await App.DataRepo.DeleteGastoAsync(gasto);
                await DisplayAlert("Éxito", "Gasto eliminado correctamente", "OK");
                CargarGastos();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al eliminar el gasto: {ex.Message}", "OK");
            }
        }
    }
}