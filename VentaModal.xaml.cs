using System.Collections.ObjectModel;
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
    private List<string> _todasLasDescripcionesArticulos = new();
    private readonly ObservableCollection<ArticuloVenta> _articulos = new();
    private ArticuloVenta? _currentArticulo;
    public Ventas Venta { get; private set; } = null!;


    public VentaModal(IDataRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);

        InitializeComponent();
        _repository = repository;
        BindableLayout.SetItemsSource(ArticulosStack, _articulos);
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
            _todasLasDescripcionesArticulos = await _repository.GetDistinctArticulosVentaDescriptionsAsync();
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

        // Validar artículos
        if (_articulos.Count == 0)
        {
            await DisplayAlert("Error", "Debe agregar al menos un artículo", "OK");
            return;
        }

        var articuloInvalido = _articulos.FirstOrDefault(a => string.IsNullOrWhiteSpace(a.Descripcion));
        if (articuloInvalido is not null)
        {
            await DisplayAlert("Error", "Cada artículo debe tener una descripción", "OK");
            return;
        }

        try
        {
            Venta = new Ventas
            {
                Precio = _articulos.Sum(a => a.Total),
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

        // Guardar artículos
        foreach (var articulo in _articulos)
        {
            articulo.VentaId = Venta.Id;
            await _repository.SaveArticuloVentaAsync(articulo);
        }

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

    // ===== MULTI-ARTÍCULO =====

    private void OnAgregarArticuloClicked(object sender, EventArgs e)
    {
        var articulo = new ArticuloVenta { Orden = _articulos.Count + 1 };
        _articulos.Add(articulo);
        ActualizarTotal();
    }

    private void OnSubirArticulo(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is ArticuloVenta articulo)
        {
            var idx = _articulos.IndexOf(articulo);
            if (idx <= 0) return;
            _articulos.Move(idx, idx - 1);
            ReordenarArticulos();
            ActualizarTotal();
        }
    }

    private void OnBajarArticulo(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is ArticuloVenta articulo)
        {
            var idx = _articulos.IndexOf(articulo);
            if (idx < 0 || idx >= _articulos.Count - 1) return;
            _articulos.Move(idx, idx + 1);
            ReordenarArticulos();
            ActualizarTotal();
        }
    }

    private void OnEliminarArticulo(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is ArticuloVenta articulo)
        {
            _articulos.Remove(articulo);
            ReordenarArticulos();
            ActualizarTotal();
        }
    }

    private void ReordenarArticulos()
    {
        for (int i = 0; i < _articulos.Count; i++)
            _articulos[i].Orden = i + 1;
    }

    private void ActualizarTotal()
    {
        var total = _articulos.Sum(a => a.Total);
        TotalLabel.Text = $"${total:N0}";
    }

    private void OnArticuloFieldChanged(object? sender, TextChangedEventArgs e)
    {
        ActualizarTotal();
        
        // Autocompletado de artículos
        if (sender is Entry entry && entry.BindingContext is ArticuloVenta articulo)
        {
            _currentArticulo = articulo;
            var texto = e.NewTextValue?.Trim() ?? "";
            
            if (texto.Length == 0 || _todasLasDescripcionesArticulos.Count == 0)
            {
                ArticuloSuggestionsFrame.IsVisible = false;
                return;
            }
            
            var filtradas = _todasLasDescripcionesArticulos
                .Where(d => d.Contains(texto, StringComparison.OrdinalIgnoreCase))
                .ToList();
            
            if (filtradas.Count > 0)
            {
                ArticuloSuggestionsView.ItemsSource = filtradas;
                ArticuloSuggestionsFrame.IsVisible = true;
            }
            else
            {
                ArticuloSuggestionsFrame.IsVisible = false;
            }
        }
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

    // ===== AUTOCOMPLETADO DE ARTÍCULOS =====

    private void OnArticuloSuggestionSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is string descripcion && _currentArticulo is not null)
        {
            _currentArticulo.Descripcion = descripcion;
            ArticuloSuggestionsFrame.IsVisible = false;
            ActualizarTotal();
        }
    }

    private void OnArticuloSuggestionTapped(object sender, TappedEventArgs e)
    {
        if (sender is Grid grid && grid.BindingContext is string descripcion && _currentArticulo is not null)
        {
            _currentArticulo.Descripcion = descripcion;
            ArticuloSuggestionsFrame.IsVisible = false;
            ActualizarTotal();
        }
    }

    private async void OnArticuloDismissSuggestionTapped(object sender, TappedEventArgs e)
    {
        if (sender is Label label && label.BindingContext is string descripcion)
        {
            try
            {
                await _repository.DismissAutocompleteDescriptionAsync(descripcion, "ArticuloVenta");
                _todasLasDescripcionesArticulos.Remove(descripcion);

                if (ArticuloSuggestionsFrame.IsVisible && ArticuloSuggestionsView.ItemsSource is List<string> filtradas)
                {
                    filtradas.Remove(descripcion);
                    if (filtradas.Count == 0)
                        ArticuloSuggestionsFrame.IsVisible = false;
                    else
                        ArticuloSuggestionsView.ItemsSource = filtradas.ToList();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AUTOCOMPLETE] Error al descartar sugerencia de artículo: {ex.Message}");
            }
        }
    }

}