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

    private async Task EditarGasto(object selectedItem)
    {
        if (selectedItem is Gasto gasto)
        {
            await Navigation.PushAsync(new EditarGastoPage(gasto));
        }
        else
        {
            await DisplayAlert("Error", "No se pudo editar el gasto: el elemento seleccionado no es un gasto válido.", "OK");
        }
    }

    private async Task EliminarGasto(object selectedItem)
    {
        
        
       if (selectedItem is Gasto gasto)
       {
            var confirm = await DisplayAlert("Confirmar", $"¿Estás seguro de eliminar el gasto \"{gasto.Descripcion}\" del dia {gasto.Fecha}?", "Sí", "No");
            if (confirm)
            {
              
                    await App.DataRepo.DeleteGastoAsync(gasto);
                    await DisplayAlert("Éxito", "Gasto eliminado correctamente", "Aceptar");
                    CargarGastos();
               
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
