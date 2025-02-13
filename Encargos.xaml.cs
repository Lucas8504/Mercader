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
                    await EditarEncargo(selectedItem!);
                    break;
                case "Eliminar":
                    await EliminarEncargo(selectedItem!);
                    break;
            }
        }
    }

    private async Task EditarEncargo(object selectedItem)
    {
        if (selectedItem is Encargo encargo)
        {
            await Navigation.PushAsync(new EditarEncargoPage(encargo));
        }
        else
        {
            await DisplayAlert("Error", "No se pudo editar el encargo: el elemento seleccionado no es un encargo válido.", "OK");
        }
    }

    private async Task EliminarEncargo(object selectedItem)
    {
        
        if (selectedItem is Encargo encargo)
        {
            bool confirm = await DisplayAlert("Confirmación", $"¿Realmente deseas eliminar el encargo de \"{encargo.Nombre}\" hecho el dia: {encargo.Fecha}?", "Sí", "No");
            if (confirm)
            {
                await App.DataRepo.DeleteEncargoAsync(encargo);
                await DisplayAlert("Éxito", "Encargo eliminado correctamente", "OK");
                CargarEncargos();
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

