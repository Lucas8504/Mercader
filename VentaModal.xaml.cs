using System.Windows.Input;
using Mercader.Data.Interfaces;
using Mercader.Domain.Entities;
using Mercader.Services.Interfaces;
using Mercader.Core.ViewModels;
#if ANDROID
using Mercader.Platforms.Android;
#endif

namespace Mercader;

public partial class VentaModal : ContentPage
{
    private readonly IDataRepository _repository;
    private readonly IImageStorageService _imageStorageService;
    private readonly VentaModalViewModel _viewModel;
    private List<string> _todasLasDescripciones = new();

    public Ventas Venta { get; private set; } = null!;

    public VentaModal(IDataRepository repository, IImageStorageService imageStorageService, VentaModalViewModel viewModel)
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
        await LoadVentaDescriptionsAsync();
    }

    private async Task LoadVentaDescriptionsAsync()
    {
        try
        {
            _todasLasDescripciones = await _repository.GetDistinctVentasDescriptionsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AUTOCOMPLETE] Error al cargar sugerencias de venta: {ex.Message}");
        }
    }

    private async void OnAgregarVentaClicked(object sender, EventArgs e)
    {
        // Validar usando ValidationService
        var descValidation = ValidationService.ValidateRequired(DescripcionV_Entry.Text, "una descripción");
        if (!descValidation.IsValid)
        {
            await DisplayAlert("Error", descValidation.ErrorMessage, "OK");
            return;
        }

        var precioResult = ValidationService.ParseDecimal(PrecioEntry.Text);
        if (!precioResult.Success)
        {
            await DisplayAlert("Error", precioResult.Error ?? "Precio inválido", "OK");
            return;
        }

        var cantidadResult = ValidationService.ParseDecimal(CantidadEntry.Text);
        if (!cantidadResult.Success)
        {
            await DisplayAlert("Error", cantidadResult.Error ?? "Cantidad inválida", "OK");
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
            Venta = new Ventas
            {
                Precio = _viewModel.Total,
                Cantidad = 1,
                Descripcion = DescripcionV_Entry.Text,
                Fecha = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al crear venta: {ex.Message}", "OK");
            return;
        }

        await SaveVentaAsync();
    }

    private async Task SaveVentaAsync()
    {
        await _repository.SaveVentasAsync(Venta);

        await _viewModel.SaveArticulosAsync(Venta.Id);

        // Rehabilitar descripción en autocompletado si estaba descartada
        if (!string.IsNullOrWhiteSpace(Venta.Descripcion))
            await _repository.ReinstateAutocompleteDescriptionAsync(Venta.Descripcion, "Venta");

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

    private async void OnOpenImageGalleryClicked(object sender, TappedEventArgs e)
    {
        if (sender is Frame frame && frame.BindingContext is ArticuloVenta articulo)
        {
            var modal = new ImageGalleryModal(articulo, _imageStorageService);
            await Navigation.PushModalAsync(modal);
            _viewModel.RefreshArticulosBinding();
        }
    }

    // ===== AUTOCOMPLETADO PARA VENTA (descripción principal) =====

    private void OnDescripcionTextChanged(object sender, TextChangedEventArgs e)
    {
        var texto = e.NewTextValue?.Trim() ?? "";

        if (texto.Length == 0 || _todasLasDescripciones.Count == 0)
        {
            SuggestionsFrame.IsVisible = false;
            return;
        }

        var filtradas = _todasLasDescripciones
            .Where(d => d.Contains(texto, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (filtradas.Count > 0)
        {
            SuggestionsView.ItemsSource = filtradas;
            SuggestionsFrame.IsVisible = true;
        }
        else
        {
            SuggestionsFrame.IsVisible = false;
        }
    }

    private void OnSuggestionSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is string descripcion)
        {
            DescripcionV_Entry.Text = descripcion;
            SuggestionsFrame.IsVisible = false;
        }
    }

    private void OnSuggestionTapped(object sender, TappedEventArgs e)
    {
        if (sender is Grid grid && grid.BindingContext is string descripcion)
        {
            DescripcionV_Entry.Text = descripcion;
            SuggestionsFrame.IsVisible = false;
        }
    }

    private async void OnDismissSuggestionTapped(object sender, TappedEventArgs e)
    {
        if (sender is Label label && label.BindingContext is string descripcion)
        {
            try
            {
                await _repository.DismissAutocompleteDescriptionAsync(descripcion, "Venta");

                _todasLasDescripciones.Remove(descripcion);

                if (SuggestionsFrame.IsVisible && SuggestionsView.ItemsSource is List<string> filtradas)
                {
                    filtradas.Remove(descripcion);
                    if (filtradas.Count == 0)
                        SuggestionsFrame.IsVisible = false;
                    else
                        SuggestionsView.ItemsSource = filtradas.ToList();
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
            await _repository.DismissAutocompleteDescriptionAsync(descripcion, "ArticuloVenta");
            ArticuloSuggestionsFrame.IsVisible = false;
        }
    }
}
