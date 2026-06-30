using System.Globalization;
using Mercader.Domain.Entities;
using Mercader.Data.Interfaces;
#if ANDROID
using Mercader.Platforms.Android;
#endif


namespace Mercader;

public partial class VentaModal : ContentPage
{
    
    private readonly IDataRepository _repository;
    private List<string> _todasLasDescripciones = new();
    public Ventas Venta { get; private set; } = null!;


    public VentaModal(IDataRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);

        InitializeComponent();
        _repository = repository;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarSugerenciasAsync();
    }

    private async Task CargarSugerenciasAsync()
    {
        try
        {
            _todasLasDescripciones = await _repository.GetDistinctVentasDescriptionsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AUTOCOMPLETE] Error al cargar sugerencias: {ex.Message}");
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

        try
        {
            Venta = new Ventas
            {
                Precio = precioResult.Value ?? 0,
                Cantidad = cantidadResult.Value ?? 0,
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

    // ===== AUTOCOMPLETADO =====

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
                // Persistir que no se muestre más
                await _repository.DismissAutocompleteDescriptionAsync(descripcion, "Venta");

                // Remover de la lista en memoria
                _todasLasDescripciones.Remove(descripcion);

                // Refrescar la vista de sugerencias si está visible
                if (SuggestionsFrame.IsVisible && SuggestionsView.ItemsSource is List<string> filtradas)
                {
                    filtradas.Remove(descripcion);
                    if (filtradas.Count == 0)
                        SuggestionsFrame.IsVisible = false;
                    else
                        SuggestionsView.ItemsSource = filtradas.ToList(); // refrescar
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AUTOCOMPLETE] Error al descartar sugerencia: {ex.Message}");
            }
        }
    }

}