namespace Mercader;

public partial class Venta : ContentPage
{
    public Venta()
    {
        InitializeComponent();
        CargarVentas();
    }

    private async void CargarVentas()
    {
        try
        {
            var ventas = await App.DataRepo.GetVentasAsync();
            VentasCollectionView.ItemsSource = ventas;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al cargar los encargos: {ex.Message}");
        }
    }

    private async void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selectedItem = e.CurrentSelection.FirstOrDefault();
        if (selectedItem != null)
        {
            await HandleSelectedItem(selectedItem);
            ((CollectionView)sender).SelectedItem = null;
        }

        async Task HandleSelectedItem(object? selectedItem)
        {
            string action = await DisplayActionSheet("Opciones", "Cancelar", null, "Editar", "Eliminar");
            switch (action)
            {
                case "Editar":
                    await EditarVenta(selectedItem!);
                    break;
                case "Eliminar":
                    await EliminarVenta(selectedItem!);
                    break;
            }
        }
    }

    private async Task EditarVenta(object selectedItem)
    {
        // Implementa la lógica para editar la venta
        Console.WriteLine("Editar venta: " + selectedItem);
        await Task.CompletedTask;
    }

    private async Task EliminarVenta(object selectedItem)
    {
        try
        {
            if (selectedItem is Ventas venta)
            {
                await App.DataRepo.DeleteVentaAsync(venta);
                await DisplayAlert("Éxito", "Venta eliminada correctamente", "OK");
                CargarVentas(); // Recargar la lista de ventas
            }
            else
            {
                await DisplayAlert("Error", "No se pudo eliminar la venta: el elemento seleccionado no es una venta válida.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo eliminar la venta: {ex.Message}", "OK");
        }
    }

    private void OnEditSwipeItemInvoked(object sender, EventArgs e)
    {
        var swipeItem = sender as SwipeItem;
        var item = swipeItem?.BindingContext;
        // Lógica para editar el elemento
    }

    private void OnDeleteSwipeItemInvoked(object sender, EventArgs e)
    {
        var swipeItem = sender as SwipeItem;
        var item = swipeItem?.BindingContext;
        // Lógica para eliminar el elemento
    }
}
