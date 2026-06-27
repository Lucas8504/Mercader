using Mercader.Domain.Entities;
using Mercader.Data.Interfaces;
#if ANDROID
using Mercader.Platforms.Android;
#endif

namespace Mercader;

public partial class EncModal : ContentPage
{
    public Encargo Encargo { get; private set; } = null!;
    private readonly IDataRepository _repository;
    private List<string> _todasLasDescripciones = new();
    private List<string> _todosLosNombres = new();

    public EncModal(IDataRepository repository)
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
            _todasLasDescripciones = await _repository.GetDistinctEncargosDescriptionsAsync();
            _todosLosNombres = await _repository.GetDistinctEncargosNombresAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AUTOCOMPLETE] Error al cargar sugerencias: {ex.Message}");
        }
    }

    private async void OnAgregarEncargoClicked(object sender, EventArgs e)
    {
        var nombreValidation = ValidationService.ValidateRequired(EncargoEntry.Text, "un nombre");
        if (!nombreValidation.IsValid)
        {
            await DisplayAlert("Error", nombreValidation.ErrorMessage, "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(ContactoEntry.Text))
        {
            await DisplayAlert("Error", "Por favor, ingrese un número de teléfono", "OK");
            return;
        }

        if (!IsValidPhoneNumber(ContactoEntry.Text))
        {
            await DisplayAlert("Error", "Por favor, ingrese un número de teléfono válido", "OK");
            return;
        }

        var descValidation = ValidationService.ValidateRequired(DescripcionEntry.Text, "una descripción");
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
            Encargo = new Encargo
            {
                Nombre = EncargoEntry!.Text,
                Contacto = CleanPhoneNumber(ContactoEntry!.Text),
                Cantidad = cantidadResult.Value ?? 0,
                Precio = precioResult.Value ?? 0,
                Descripcion = DescripcionEntry.Text,
                FechaEntrega = FechaEntregaDatePicker.Date,
                Fecha = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al crear encargo: {ex.Message}", "OK");
            return;
        }

        await SaveEncargoAsync();
    }

    private async Task SaveEncargoAsync()
    {
        await _repository.SaveEncargoAsync(Encargo);

        // Rehabilitar nombre y descripción en autocompletado si estaban descartados
        if (!string.IsNullOrWhiteSpace(Encargo.Nombre))
            await _repository.ReinstateAutocompleteDescriptionAsync(Encargo.Nombre);
        if (!string.IsNullOrWhiteSpace(Encargo.Descripcion))
            await _repository.ReinstateAutocompleteDescriptionAsync(Encargo.Descripcion);

#if ANDROID
        KeyboardHelper.Close();
#endif

        await Navigation.PopModalAsync();
    }

    /// <summary>
    /// Valida que el n�mero de tel�fono tenga un formato b�sico v�lido
    /// </summary>
    /// <param name="phoneNumber">N�mero de tel�fono a validar</param>
    /// <returns>True si el formato es v�lido, False en caso contrario</returns>
    private bool IsValidPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        // Remover espacios, guiones, par�ntesis y el signo +
        string cleanedNumber = phoneNumber.Replace(" ", "")
                                        .Replace("-", "")
                                        .Replace("(", "")
                                        .Replace(")", "")
                                        .Replace("+", "");

        // Verificar que solo contenga n�meros y tenga entre 7 y 15 d�gitos
        return cleanedNumber.All(char.IsDigit) &&
               cleanedNumber.Length >= 7 &&
               cleanedNumber.Length <= 15;
    }

    /// <summary>
    /// Limpia el n�mero de tel�fono removiendo caracteres especiales
    /// </summary>
    /// <param name="phoneNumber">N�mero de tel�fono a limpiar</param>
    /// <returns>N�mero de tel�fono solo con d�gitos</returns>
    private string CleanPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return phoneNumber;

        // Remover todos los caracteres que no sean n�meros
        return new string(phoneNumber.Where(char.IsDigit).ToArray());
    }

    private async void Cancelar(object sender, EventArgs e)
    {
#if ANDROID
        KeyboardHelper.Close();
#endif

        await Navigation.PopModalAsync();
    }

    // ===== AUTOCOMPLETADO =====

    // --- Autocompletado para Nombre del cliente ---

    private void OnNombreEncargoTextChanged(object sender, TextChangedEventArgs e)
    {
        var texto = e.NewTextValue?.Trim() ?? "";

        if (texto.Length == 0 || _todosLosNombres.Count == 0)
        {
            NombreSuggestionsFrame.IsVisible = false;
            return;
        }

        var filtradas = _todosLosNombres
            .Where(n => n.Contains(texto, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (filtradas.Count > 0)
        {
            NombreSuggestionsView.ItemsSource = filtradas;
            NombreSuggestionsFrame.IsVisible = true;
        }
        else
        {
            NombreSuggestionsFrame.IsVisible = false;
        }
    }

    private void OnNombreSuggestionSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is string nombre)
        {
            EncargoEntry.Text = nombre;
            NombreSuggestionsFrame.IsVisible = false;
        }
    }

    private void OnNombreSuggestionTapped(object sender, TappedEventArgs e)
    {
        if (sender is Grid grid && grid.BindingContext is string nombre)
        {
            EncargoEntry.Text = nombre;
            NombreSuggestionsFrame.IsVisible = false;
        }
    }

    private async void OnNombreDismissSuggestionTapped(object sender, TappedEventArgs e)
    {
        if (sender is Label label && label.BindingContext is string nombre)
        {
            try
            {
                await _repository.DismissAutocompleteDescriptionAsync(nombre);
                _todosLosNombres.Remove(nombre);

                if (NombreSuggestionsFrame.IsVisible && NombreSuggestionsView.ItemsSource is List<string> filtradas)
                {
                    filtradas.Remove(nombre);
                    if (filtradas.Count == 0)
                        NombreSuggestionsFrame.IsVisible = false;
                    else
                        NombreSuggestionsView.ItemsSource = filtradas.ToList();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AUTOCOMPLETE] Error al descartar sugerencia: {ex.Message}");
            }
        }
    }

    // --- Autocompletado para Descripción ---

    private void OnDescripcionEncargoTextChanged(object sender, TextChangedEventArgs e)
    {
        var texto = e.NewTextValue?.Trim() ?? "";

        if (texto.Length == 0 || _todasLasDescripciones.Count == 0)
        {
            EncargoSuggestionsFrame.IsVisible = false;
            return;
        }

        var filtradas = _todasLasDescripciones
            .Where(d => d.Contains(texto, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (filtradas.Count > 0)
        {
            EncargoSuggestionsView.ItemsSource = filtradas;
            EncargoSuggestionsFrame.IsVisible = true;
        }
        else
        {
            EncargoSuggestionsFrame.IsVisible = false;
        }
    }

    private void OnEncargoSuggestionSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is string descripcion)
        {
            DescripcionEntry.Text = descripcion;
            EncargoSuggestionsFrame.IsVisible = false;
        }
    }

    private void OnEncargoSuggestionTapped(object sender, TappedEventArgs e)
    {
        if (sender is Grid grid && grid.BindingContext is string descripcion)
        {
            DescripcionEntry.Text = descripcion;
            EncargoSuggestionsFrame.IsVisible = false;
        }
    }

    private async void OnEncargoDismissSuggestionTapped(object sender, TappedEventArgs e)
    {
        if (sender is Label label && label.BindingContext is string descripcion)
        {
            try
            {
                await _repository.DismissAutocompleteDescriptionAsync(descripcion);
                _todasLasDescripciones.Remove(descripcion);

                if (EncargoSuggestionsFrame.IsVisible && EncargoSuggestionsView.ItemsSource is List<string> filtradas)
                {
                    filtradas.Remove(descripcion);
                    if (filtradas.Count == 0)
                        EncargoSuggestionsFrame.IsVisible = false;
                    else
                        EncargoSuggestionsView.ItemsSource = filtradas.ToList();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AUTOCOMPLETE] Error al descartar sugerencia: {ex.Message}");
            }
        }
    }
}