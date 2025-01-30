namespace Mercader;

public partial class Gastos : ContentPage
{
    public Gastos()
    {
        InitializeComponent();
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
                    await EditarGasto(selectedItem!);
                    break;
                case "Eliminar":
                    await EliminarGasto(selectedItem!);
                    break;
            }
        }
    }
    private async Task EditarGasto(object? selectedItem)
    {
        // Implementa la lógica para editar la gasto
        Console.WriteLine("Editar gasto: " + selectedItem);
        await Task.CompletedTask;
    }

    private async Task EliminarGasto(object? selectedItem)
    {
        try
        {
            if (selectedItem is Gastos gasto)
            {
                await App.DataRepo.DeleteGastoAsync(gasto);
                await CargarGastos();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al eliminar el gasto: {ex.Message}");
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
