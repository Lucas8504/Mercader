using System.Collections.ObjectModel;
using Mercader.Domain.Entities;
using Mercader.Data.Interfaces;
#if ANDROID
using Mercader.Platforms.Android;
#endif

namespace Mercader;

public partial class GastoModal : ContentPage
{
    private readonly IDataRepository _repository;
    private List<string> _todasLasDescripciones = new();
    private readonly ObservableCollection<ArticuloGasto> _articulos = new();
    public Gasto Gasto { get; private set; } = null!;

    public GastoModal(IDataRepository repository)
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
            _todasLasDescripciones = await _repository.GetDistinctGastosDescriptionsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AUTOCOMPLETE] Error al cargar sugerencias: {ex.Message}");
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

        var montoResult = ValidationService.ParseDecimal(MontoGastoEntry.Text);
        if (!montoResult.Success)
        {
            await DisplayAlert("Error", montoResult.Error ?? "Monto inválido", "OK");
            return;
        }

        var cantidadResult = ValidationService.ParseDecimal(CantidadG_Entry.Text);
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
            Gasto = new Gasto
            {
                Descripcion = DescripcionGastoEntry.Text,
                Cantidad = 1,
                Monto = _articulos.Sum(a => a.Total),
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

        // Guardar artículos
        foreach (var articulo in _articulos)
        {
            articulo.GastoId = Gasto.Id;
            await _repository.SaveArticuloGastoAsync(articulo);
        }

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

    // ===== MULTI-ARTÍCULO =====

    private void OnAgregarArticuloClicked(object sender, EventArgs e)
    {
        var articulo = new ArticuloGasto { Orden = _articulos.Count + 1 };
        _articulos.Add(articulo);
        ActualizarTotal();
    }

    private void OnSubirArticulo(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is ArticuloGasto articulo)
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
        if (sender is Button btn && btn.BindingContext is ArticuloGasto articulo)
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
        if (sender is Button btn && btn.BindingContext is ArticuloGasto articulo)
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

    // ===== AUTOCOMPLETADO =====

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
}
