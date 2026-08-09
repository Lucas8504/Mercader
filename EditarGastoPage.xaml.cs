using System.Windows.Input;
using Mercader.Data.Interfaces;
using Mercader.Domain.Entities;
using Mercader.Services.Interfaces;
using Mercader.Core.ViewModels;
using System.IO;

namespace Mercader;

public partial class EditarGastoPage : ContentPage
{
    private readonly IDataRepository _repository;
    private readonly IImageStorageService _imageStorageService;
    private readonly EditarGastoViewModel _viewModel;
    private readonly Gasto _gasto;

    public EditarGastoPage(Gasto gasto, IDataRepository repository, IImageStorageService imageStorageService, EditarGastoViewModel viewModel)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _imageStorageService = imageStorageService ?? throw new ArgumentNullException(nameof(imageStorageService));
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _gasto = gasto ?? throw new ArgumentNullException(nameof(gasto));

        InitializeComponent();
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync(_gasto);
    }

    private async void OnGuardarClicked(object sender, EventArgs e)
    {
        var success = await _viewModel.SaveAsync(_gasto, _repository);
        if (!success)
        {
            if (string.IsNullOrWhiteSpace(_viewModel.Descripcion))
                await DisplayAlert("Error", "La descripción es requerida", "OK");
            else if (!_viewModel.HasArticulos)
                await DisplayAlert("Error", "Debe agregar al menos un artículo", "OK");
            else
                await DisplayAlert("Error", "Cada artículo debe tener una descripción", "OK");
            return;
        }

        await DisplayAlert("Éxito", "Gasto actualizado correctamente", "OK");
        await Navigation.PopAsync();
    }

    private async void OnCancelarClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert(
            "Confirmar",
            "¿Estás seguro de que deseas cancelar? Se perderán los cambios no guardados.",
            "Sí",
            "No");

        if (confirm)
            await Navigation.PopAsync();
    }

    // ===== IMAGE HANDLING =====

    private async void OnPickImageClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is ArticuloGasto articulo)
        {
            await PickImageAsync(articulo);
        }
    }

    private async Task PickImageAsync(ArticuloGasto articulo)
    {
        try
        {
            var result = await MediaPicker.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Seleccionar imagen"
            });

            if (result != null)
            {
                using var stream = await result.OpenReadAsync();
                var path = await _imageStorageService.CompressAndSaveAsync(stream);
                if (path != null)
                {
                    articulo.ImagenPath = path;
                    _viewModel.RefreshArticulosBinding();
                }
            }
        }
        catch (Exception)
        {
            // User cancelled or error - silently ignore
        }
    }

    private async void OnClearImageClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is ArticuloGasto articulo)
        {
            if (!string.IsNullOrWhiteSpace(articulo.ImagenPath))
            {
                await _imageStorageService.DeleteFileAsync(articulo.ImagenPath);
                articulo.ImagenPath = null;
                _viewModel.RefreshArticulosBinding();
            }
        }
    }

    // ===== AUTOCOMPLETADO DE ARTÍCULOS =====

    private void OnArticuloFieldChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is Entry entry)
        {
            var filtradas = _viewModel.GetFilteredArticuloSuggestions(e.NewTextValue?.Trim() ?? "", entry.BindingContext);

            if (filtradas.Count > 0)
            {
                ArticuloSuggestionsView.ItemsSource = filtradas;
                ArticuloSuggestionsFrame.IsVisible = true;
            }
            else
            {
                ArticuloSuggestionsFrame.IsVisible = false;
            }

            // Recalcular total cuando cambian precio o cantidad
            _viewModel.RecalcularTotal();
        }
    }

    private void OnArticuloSuggestionSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is string descripcion)
        {
            _viewModel.ApplyArticuloSuggestion(descripcion);
            ArticuloSuggestionsFrame.IsVisible = false;
        }
    }

    private void OnArticuloSuggestionTapped(object sender, TappedEventArgs e)
    {
        if (sender is Grid grid && grid.BindingContext is string descripcion)
        {
            _viewModel.ApplyArticuloSuggestion(descripcion);
            ArticuloSuggestionsFrame.IsVisible = false;
        }
    }

    private async void OnArticuloDismissSuggestionTapped(object sender, TappedEventArgs e)
    {
        if (sender is Label label && label.BindingContext is string descripcion)
        {
            await _repository.DismissAutocompleteDescriptionAsync(descripcion, "ArticuloGasto");
            ArticuloSuggestionsFrame.IsVisible = false;
        }
    }
}
