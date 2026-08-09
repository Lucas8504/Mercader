using System.Windows.Input;
using Mercader.Data.Interfaces;
using Mercader.Domain.Entities;
using Mercader.Services.Interfaces;
using Mercader.Core.ViewModels;
#if ANDROID
using Mercader.Platforms.Android;
#endif

namespace Mercader;

public partial class GastoModal : ContentPage
{
    private readonly IDataRepository _repository;
    private readonly IImageStorageService _imageStorageService;
    private readonly GastoModalViewModel _viewModel;
    private List<string> _todasLasDescripciones = new();

    public Gasto Gasto { get; private set; } = null!;

    public GastoModal(IDataRepository repository, IImageStorageService imageStorageService, GastoModalViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(imageStorageService);
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();
        _repository = repository;
        _imageStorageService = imageStorageService;
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadArticuloSuggestionsAsync();
        await LoadGastoDescriptionsAsync();
    }

    private async Task LoadGastoDescriptionsAsync()
    {
        try
        {
            _todasLasDescripciones = await _repository.GetDistinctGastosDescriptionsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AUTOCOMPLETE] Error al cargar sugerencias de gasto: {ex.Message}");
        }
    }

    private async void OnAgregarGastoClicked(object sender, EventArgs e)
    {
        var descValidation = ValidationService.ValidateRequired(DescripcionGastoEntry.Text, "una descripción");
        if (!descValidation.IsValid)
        {
            await DisplayAlert("Error", descValidation.ErrorMessage, "OK");
            return;
        }

        if (!_viewModel.HasArticulos)
        {
            await DisplayAlert("Error", "Debe agregar al menos un artículo", "OK");
            return;
        }

        if (!_viewModel.AllArticulosHaveDescription)
        {
            await DisplayAlert("Error", "Cada artículo debe tener una descripción", "OK");
            return;
        }

        try
        {
            Gasto = new Gasto
            {
                Descripcion = DescripcionGastoEntry.Text,
                Cantidad = 1,
                Monto = _viewModel.Total,
                Fecha = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al crear gasto: {ex.Message}", "OK");
            return;
        }

        await SaveGastoAsync();
    }

    private async Task SaveGastoAsync()
    {
        await _repository.SaveGastoAsync(Gasto);

        await _viewModel.SaveArticulosAsync(Gasto.Id);

        // Rehabilitar descripción en autocompletado si estaba descartada
        if (!string.IsNullOrWhiteSpace(Gasto.Descripcion))
            await _repository.ReinstateAutocompleteDescriptionAsync(Gasto.Descripcion, "Gasto");

#if ANDROID
        KeyboardHelper.Close();
#endif
        await Navigation.PopModalAsync();
    }

    private async void Cancelar(object sender, EventArgs e)
    {
#if ANDROID
        KeyboardHelper.Close();
#endif
        await Navigation.PopModalAsync();
    }

    // ===== IMAGE HANDLING =====

    private void OnEmptySlotTapped(object sender, TappedEventArgs e)
    {
        if (sender is Frame frame && frame.BindingContext is ArticuloGasto articulo)
        {
            if (int.TryParse(frame.StyleId, out int slotIndex))
            {
                PickImageAsync(articulo, slotIndex);
            }
        }
    }

    private async void OnClearSlotClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is ArticuloGasto articulo)
        {
            if (int.TryParse(btn.StyleId, out int slotIndex))
            {
                await ClearSlotAsync(articulo, slotIndex);
            }
        }
    }

    private async Task PickImageAsync(ArticuloGasto articulo, int slotIndex)
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
                    articulo.SetImageAtSlot(slotIndex, path);
                    _viewModel.RefreshArticulosBinding();
                }
            }
        }
        catch (Exception)
        {
            // User cancelled or error - silently ignore
        }
    }

    private async Task ClearSlotAsync(ArticuloGasto articulo, int slotIndex)
    {
        var path = articulo.GetImagePath(slotIndex);
        if (!string.IsNullOrWhiteSpace(path))
        {
            await _imageStorageService.DeleteFileAsync(path);
            articulo.ClearSlot(slotIndex);
            _viewModel.RefreshArticulosBinding();
        }
    }

    // ===== AUTOCOMPLETADO PARA GASTO (descripción principal) =====

    private void OnDescripcionGastoTextChanged(object sender, TextChangedEventArgs e)
    {
        var texto = e.NewTextValue?.Trim() ?? "";

        if (texto.Length == 0 || _todasLasDescripciones.Count == 0)
        {
            GastoSuggestionsFrame.IsVisible = false;
            return;
        }

        var filtradas = _todasLasDescripciones
            .Where(d => d.Contains(texto, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (filtradas.Count > 0)
        {
            GastoSuggestionsView.ItemsSource = filtradas;
            GastoSuggestionsFrame.IsVisible = true;
        }
        else
        {
            GastoSuggestionsFrame.IsVisible = false;
        }
    }

    private void OnGastoSuggestionSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is string descripcion)
        {
            DescripcionGastoEntry.Text = descripcion;
            GastoSuggestionsFrame.IsVisible = false;
        }
    }

    private void OnGastoSuggestionTapped(object sender, TappedEventArgs e)
    {
        if (sender is Grid grid && grid.BindingContext is string descripcion)
        {
            DescripcionGastoEntry.Text = descripcion;
            GastoSuggestionsFrame.IsVisible = false;
        }
    }

    private async void OnGastoDismissSuggestionTapped(object sender, TappedEventArgs e)
    {
        if (sender is Label label && label.BindingContext is string descripcion)
        {
            try
            {
                await _repository.DismissAutocompleteDescriptionAsync(descripcion, "Gasto");
                _todasLasDescripciones.Remove(descripcion);

                if (GastoSuggestionsFrame.IsVisible && GastoSuggestionsView.ItemsSource is List<string> filtradas)
                {
                    filtradas.Remove(descripcion);
                    if (filtradas.Count == 0)
                        GastoSuggestionsFrame.IsVisible = false;
                    else
                        GastoSuggestionsView.ItemsSource = filtradas.ToList();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AUTOCOMPLETE] Error al descartar sugerencia: {ex.Message}");
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
