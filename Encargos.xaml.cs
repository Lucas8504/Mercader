namespace Mercader;

public partial class Encargos : ContentPage
{
    private MainPage? _mainPage;

    public Encargos()
    {
        InitializeComponent();
        CargarEncargos();
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        if (Shell.Current.Navigation.NavigationStack.LastOrDefault() is Encargos page)
        {
            if (Shell.Current.Navigation.NavigationStack.FirstOrDefault() is MainPage mainPage)
            {
                _mainPage = mainPage;
                CargarEncargos();
            }
        }
    }

    private void CargarEncargos()
    {
        try
        {
            var encargos = _mainPage?.balance.Encargos;
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
        // Implementa la lógica para editar el encargo
        Console.WriteLine("Editar encargo: " + selectedItem);
        await Task.CompletedTask;
    }

    private async Task EliminarEncargo(object selectedItem)
    {
        try
        {
            if (selectedItem is Encargo encargo)
            {
                _mainPage?.balance.Encargos.Remove(encargo);
                await App.DataRepo.DeleteEncargoAsync(encargo);
                _mainPage?.ActualizarEtiquetaEncargos();
                CargarEncargos();
            }
            else
            {
                await DisplayAlert("Error", "No se pudo eliminar el encargo: el elemento seleccionado no es un encargo válido.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo eliminar el encargo: {ex.Message}", "OK");
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

