using System.Collections.ObjectModel;
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
    private readonly ObservableCollection<ArticuloEncargo> _articulos = new();

    public EncModal(IDataRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        InitializeComponent();
        _repository = repository;
        BindableLayout.SetItemsSource(ArticulosStack, _articulos);

        FechaEntregaPicker.SelectedDate = DateTime.Now;
        FechaEntregaLabel.Text = DateTime.Now.ToString("dd/MM/yyyy");
    }

    private DateTime _fechaOriginal;

    private void OnFechaEntregaTapped(object? sender, TappedEventArgs e)
    {
        _fechaOriginal = FechaEntregaPicker.SelectedDate ?? DateTime.Now;
        FechaEntregaPicker.IsOpen = true;
    }

    private void OnFechaEntregaPickerClosed(object? sender, EventArgs e)
    {
        if (FechaEntregaPicker.SelectedDate.HasValue)
        {
            FechaEntregaLabel.Text = FechaEntregaPicker.SelectedDate.Value.ToString("dd/MM/yyyy");
        }
    }

    private void OnFechaEntregaPickerOk(object? sender, EventArgs e)
    {
        if (FechaEntregaPicker.SelectedDate.HasValue)
        {
            FechaEntregaLabel.Text = FechaEntregaPicker.SelectedDate.Value.ToString("dd/MM/yyyy");
        }
        FechaEntregaPicker.IsOpen = false;
    }

    private void OnFechaEntregaPickerCancelado(object? sender, EventArgs e)
    {
        FechaEntregaPicker.SelectedDate = _fechaOriginal;
        FechaEntregaPicker.IsOpen = false;
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
            Encargo = new Encargo
            {
                Nombre = EncargoEntry!.Text,
                Contacto = CleanPhoneNumber(ContactoEntry!.Text),
                Cantidad = 1,
                Precio = _articulos.Sum(a => a.Total),
                Descripcion = DescripcionEntry.Text,
                FechaEntrega = FechaEntregaPicker.SelectedDate ?? DateTime.Now,
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

        // Guardar artículos
        foreach (var articulo in _articulos)
        {
            articulo.EncargoId = Encargo.Id;
            await _repository.SaveArticuloEncargoAsync(articulo);
        }

        // Rehabilitar nombre y descripción en autocompletado si estaban descartados
        if (!string.IsNullOrWhiteSpace(Encargo.Nombre))
            await _repository.ReinstateAutocompleteDescriptionAsync(Encargo.Nombre, "EncargoNombre");
        if (!string.IsNullOrWhiteSpace(Encargo.Descripcion))
            await _repository.ReinstateAutocompleteDescriptionAsync(Encargo.Descripcion, "Encargo");

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

    // ===== SELECTOR DE CONTACTOS =====

    private async void OnContactosButtonClicked(object sender, EventArgs e)
    {
        try
        {
            // Verificar permisos de contactos
            var status = await Permissions.CheckStatusAsync<Permissions.ContactsRead>();
            
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.ContactsRead>();
                
                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlert("Permiso requerido", 
                        "Para acceder a los contactos, necesitamos permiso. Puedes habilitarlo en Ajustes.", "OK");
                    return;
                }
            }

            // Abrir selector de contactos del dispositivo
            var contact = await Microsoft.Maui.ApplicationModel.Communication.Contacts.PickContactAsync();
            
            if (contact != null)
            {
                // Usar el nombre completo del contacto
                EncargoEntry.Text = contact.DisplayName;
                
                // Si el contacto tiene un teléfono, también llenarlo
                var phone = contact.Phones?.FirstOrDefault();
                if (phone != null && string.IsNullOrWhiteSpace(ContactoEntry.Text))
                {
                    ContactoEntry.Text = phone.PhoneNumber;
                }
            }
        }
        catch (FeatureNotSupportedException)
        {
            await DisplayAlert("Error", "Los contactos no son compatibles con este dispositivo.", "OK");
        }
        catch (PermissionException)
        {
            await DisplayAlert("Permiso denegado", 
                "No se pudo acceder a los contactos. Habilitá el permiso en Ajustes.", "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CONTACTS] Error al seleccionar contacto: {ex.Message}");
            await DisplayAlert("Error", "No se pudo acceder a los contactos.", "OK");
        }
    }

    // ===== MULTI-ARTÍCULO =====

    private void OnAgregarArticuloClicked(object sender, EventArgs e)
    {
        var articulo = new ArticuloEncargo { Orden = _articulos.Count + 1 };
        _articulos.Add(articulo);
        ActualizarTotal();
    }

    private void OnSubirArticulo(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is ArticuloEncargo articulo)
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
        if (sender is Button btn && btn.BindingContext is ArticuloEncargo articulo)
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
        if (sender is Button btn && btn.BindingContext is ArticuloEncargo articulo)
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
                await _repository.DismissAutocompleteDescriptionAsync(nombre, "EncargoNombre");
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
                await _repository.DismissAutocompleteDescriptionAsync(descripcion, "Encargo");
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