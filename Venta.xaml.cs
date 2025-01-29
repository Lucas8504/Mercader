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
        // Manejar la selección del elemento
        var selectedItem = e.CurrentSelection.FirstOrDefault();
        if (selectedItem != null)
        {
            // Lógica para editar el elemento seleccionado

            await HandleSelectedItem(selectedItem);

            // Deseleccionar el elemento después de la acción
            ((CollectionView)sender).SelectedItem = null;

        }

        async Task HandleSelectedItem(object? selectedItem)
        {
            string action = await DisplayActionSheet("Opciones", "Cancelar", null, "Editar", "Eliminar");
            switch (action)
            {
                case "Editar":
                    // Lógica para editar el elemento seleccionado
                    await EditarVenta(selectedItem!);
                    break;
                case "Eliminar":
                    // Lógica para eliminar el elemento seleccionado
                    await EliminarVenta(selectedItem!);
                    break;
            }
        }
    }

    private Task EditarVenta(object selectedItem)
    {
        // Implementa la lógica para editar la venta
        Console.WriteLine("Editar venta: " + selectedItem);
        return Task.CompletedTask;
    }

    private Task EliminarVenta(object selectedItem)
    {
        // Implementa la lógica para eliminar la venta
        Console.WriteLine("Eliminar venta: " + selectedItem);
        return Task.CompletedTask;
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