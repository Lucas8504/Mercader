namespace Mercader;

public partial class Encargos : ContentPage
{
    public Encargos()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        CargarEncargos();
    }

    private async void CargarEncargos()
    {
        try
        {
            var encargos = await App.DataRepo.GetEncargosAsync();
            EncargosCollectionView.ItemsSource = encargos;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al cargar los encargos: {ex.Message}");
        }
    }

    // Método modificado para navegar a la página de detalles
    private async void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selectedItem = e.CurrentSelection.FirstOrDefault();
        if (selectedItem != null && selectedItem is Encargo encargo)
        {
            // Navegar a la página de detalles
            await Navigation.PushAsync(new DetalleEncargo(encargo));

            // Limpiar la selección
            ((CollectionView)sender).SelectedItem = null;
        }
    }

    // Métodos para los SwipeItems (mantener funcionalidad existente)
    private async void OnEditSwipeItemInvoked(object sender, EventArgs e)
    {
        var swipeItem = sender as SwipeItem;
        var item = swipeItem?.BindingContext;
        if (item is Encargo encargo)
        {
            await EditarEncargo(encargo);
        }
    }

    private async void OnDeleteSwipeItemInvoked(object sender, EventArgs e)
    {
        var swipeItem = sender as SwipeItem;
        var item = swipeItem?.BindingContext;
        if (item is Encargo encargo)
        {
            await EliminarEncargo(encargo);
        }
    }

    private async void OnConcretarVentaSwipeItemInvoked(object sender, EventArgs e)
    {
        var swipeItem = sender as SwipeItem;
        var item = swipeItem?.BindingContext;
        if (item is Encargo encargo)
        {
            await ConcretarVenta(encargo);
        }
    }

    // Métodos auxiliares (mantener funcionalidad existente)
    private async Task EditarEncargo(Encargo encargo)
    {
        // Implementa la lógica para editar el encargo
        Console.WriteLine("Editar encargo: " + encargo.Nombre);
        await DisplayAlert("Información", "Función de editar en desarrollo", "OK");
    }

    private async Task EliminarEncargo(Encargo encargo)
    {
        bool confirm = await DisplayAlert("Confirmación", $"¿Realmente deseas eliminar el encargo de \"{encargo.Nombre}\" hecho el día: {encargo.Fecha}?", "Sí", "No");
        if (confirm)
        {
            try
            {
                await App.DataRepo.DeleteEncargoAsync(encargo);
                await DisplayAlert("Éxito", "Encargo eliminado correctamente", "OK");
                CargarEncargos();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al eliminar el encargo: {ex.Message}", "OK");
            }
        }
    }

    private async Task ConcretarVenta(Encargo encargo)
    {
        bool confirm = await DisplayAlert("Confirmación", $"¿Realmente deseas concretar la venta de {encargo.Descripcion} para \"{encargo.Nombre}\" pedido el día: {encargo.Fecha}?", "Sí", "No");
        if (confirm)
        {
            try
            {
                var venta = new Ventas
                {
                    Descripcion = encargo.Descripcion,
                    Precio = encargo.Precio,
                    Cantidad = encargo.Cantidad,
                    Fecha = DateTime.Now
                };

                await App.DataRepo.SaveVentasAsync(venta);
                await App.DataRepo.DeleteEncargoAsync(encargo);
                await DisplayAlert("Éxito", "Venta concretada correctamente", "OK");
                CargarEncargos();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al concretar la venta: {ex.Message}", "OK");
            }
        }
    }
}