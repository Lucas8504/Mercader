using System.Collections.ObjectModel;
using Mercader.Domain.Entities;
using Mercader.Data.Interfaces;

namespace Mercader;

public partial class EditarVentaPage : ContentPage
{
    private readonly IDataRepository _repository;
    private readonly Ventas _venta;
    private readonly ObservableCollection<ArticuloVenta> _articulos = new();
    private List<string> _todasLasDescripcionesArticulos = new();
    private ArticuloVenta? _currentArticulo;

    public EditarVentaPage(Ventas venta, IDataRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _venta = venta ?? throw new ArgumentNullException(nameof(venta));
        InitializeComponent();
        BindableLayout.SetItemsSource(ArticulosStack, _articulos);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        DescripcionEntry.Text = _venta.Descripcion;

        // Cargar artículos existentes
        var articulos = await _repository.GetArticulosVentaAsync(_venta.Id);
        foreach (var articulo in articulos)
            _articulos.Add(articulo);

        // Cargar sugerencias de autocompletado
        try
        {
            _todasLasDescripcionesArticulos = await _repository.GetDistinctArticulosVentaDescriptionsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AUTOCOMPLETE] Error al cargar sugerencias: {ex.Message}");
        }

        ActualizarTotal();
    }

    private async void OnGuardarClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(DescripcionEntry.Text))
        {
            await DisplayAlert("Error", "La descripción es requerida", "OK");
            return;
        }

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

        // Actualizar venta
        _venta.Descripcion = DescripcionEntry.Text;
        _venta.Precio = _articulos.Sum(a => a.Total);
        _venta.Cantidad = 1;

        await _repository.SaveVentasAsync(_venta);

        // Eliminar artículos viejos y guardar los nuevos
        var articulosViejos = await _repository.GetArticulosVentaAsync(_venta.Id);
        foreach (var viejo in articulosViejos)
            await _repository.DeleteArticuloVentaAsync(viejo);

        foreach (var articulo in _articulos)
        {
            articulo.Id = 0; // Reset ID para que sea un insert
            articulo.VentaId = _venta.Id;
            await _repository.SaveArticuloVentaAsync(articulo);
        }

        await DisplayAlert("Éxito", "Venta actualizada correctamente", "OK");
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
