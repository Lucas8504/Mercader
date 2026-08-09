using System.Windows.Input;
using Mercader.Data.Interfaces;
using Mercader.Domain.Entities;
using Mercader.Services.Interfaces;
using Mercader.Core.ViewModels;
using System.IO;

namespace Mercader;

public partial class EditarEncargoPage : ContentPage
{
    private readonly IDataRepository _repository;
    private readonly IImageStorageService _imageStorageService;
    private readonly EditarEncargoViewModel _viewModel;
    private readonly Encargo _encargo;

    public EditarEncargoPage(Encargo encargo, IDataRepository repository, IImageStorageService imageStorageService, EditarEncargoViewModel viewModel)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _imageStorageService = imageStorageService ?? throw new ArgumentNullException(nameof(imageStorageService));
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _encargo = encargo ?? throw new ArgumentNullException(nameof(encargo));

        InitializeComponent();
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync(_encargo);
    }

    private DateTime _fechaOriginal;

    private void OnFechaEntregaTapped(object? sender, TappedEventArgs e)
    {
        _fechaOriginal = FechaEntregaPicker.SelectedDate ?? _encargo.FechaEntrega;
        FechaEntregaPicker.IsOpen = true;
    }

    private void OnFechaEntregaPickerClosed(object? sender, EventArgs e)
    {
        if (FechaEntregaPicker.SelectedDate.HasValue)
            FechaEntregaLabel.Text = FechaEntregaPicker.SelectedDate.Value.ToString("dd/MM/yyyy");
    }

    private void OnFechaEntregaPickerOk(object? sender, EventArgs e)
    {
        if (FechaEntregaPicker.SelectedDate.HasValue)
            FechaEntregaLabel.Text = FechaEntregaPicker.SelectedDate.Value.ToString("dd/MM/yyyy");
        FechaEntregaPicker.IsOpen = false;
    }

    private void OnFechaEntregaPickerCancelado(object? sender, EventArgs e)
    {
        FechaEntregaPicker.SelectedDate = _fechaOriginal;
        FechaEntregaPicker.IsOpen = false;
    }

    private async void OnGuardarClicked(object sender, EventArgs e)
    {
        var success = await _viewModel.SaveAsync(_encargo, _repository);
        if (!success)
        {
            if (string.IsNullOrWhiteSpace(_viewModel.Nombre))
                await DisplayAlert("Error", "El nombre del cliente es obligatorio", "OK");
            else if (string.IsNullOrWhiteSpace(_viewModel.Contacto) || !IsValidPhoneNumber(_viewModel.Contacto))
                await DisplayAlert("Error", "Por favor, ingrese un número de teléfono válido", "OK");
            else if (!_viewModel.HasArticulos)
                await DisplayAlert("Error", "Debe agregar al menos un artículo", "OK");
            else
                await DisplayAlert("Error", "Cada artículo debe tener una descripción", "OK");
            return;
        }

        await DisplayAlert("Éxito", "Encargo actualizado correctamente", "OK");
        await Navigation.PopAsync();
    }

    private bool IsValidPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        string cleanedNumber = phoneNumber.Replace(" ", "")
                                            .Replace("-", "")
                                            .Replace("(", "")
                                            .Replace(")", "")
                                            .Replace("+", "");

        return cleanedNumber.All(char.IsDigit) &&
               cleanedNumber.Length >= 7 &&
               cleanedNumber.Length <= 15;
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
        if (sender is Button btn && btn.BindingContext is ArticuloEncargo articulo)
        {
            await PickImageAsync(articulo);
        }
    }

    private async Task PickImageAsync(ArticuloEncargo articulo)
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
        if (sender is Button btn && btn.BindingContext is ArticuloEncargo articulo)
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
            await _repository.DismissAutocompleteDescriptionAsync(descripcion, "ArticuloEncargo");
            ArticuloSuggestionsFrame.IsVisible = false;
        }
    }
}
