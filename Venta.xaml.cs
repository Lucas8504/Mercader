namespace Mercader;

public partial class Venta : ContentPage
{
    public Venta()
    {
        InitializeComponent();
        
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
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
        if (selectedItem is Ventas venta)
        {
            await Navigation.PushAsync(new EditarVentaPage(venta));
        }
        else
        {
            await DisplayAlert("Error", "No se pudo editar la venta: el elemento seleccionado no es una venta válida.", "OK");
        }
    }

    private async Task EliminarVenta(object selectedItem)
    {
       
        if (selectedItem is Ventas venta)
        {
            bool confirm = await DisplayAlert("Confirmación", $"¿Realmente deseas eliminar la venta de \"{venta.Descripcion}\" del dia {venta.Fecha}?", "Sí", "No");
            if (confirm)
            {
                await App.DataRepo.DeleteVentaAsync(venta);
                await DisplayAlert("Éxito", "Venta eliminada correctamente", "OK");
                CargarVentas();
            }
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
