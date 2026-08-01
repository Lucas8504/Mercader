using System.Collections.ObjectModel;
using Mercader.Domain.Entities;
using Mercader.Data.Interfaces;

namespace Mercader;

public partial class EditarEncargoPage : ContentPage
{
    private readonly IDataRepository _repository;
    private readonly Encargo _encargo;
    private readonly ObservableCollection<ArticuloEncargo> _articulos = new();

    public EditarEncargoPage(Encargo encargo, IDataRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _encargo = encargo ?? throw new ArgumentNullException(nameof(encargo));
        InitializeComponent();
        BindableLayout.SetItemsSource(ArticulosStack, _articulos);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        NombreEntry.Text = _encargo.Nombre;
        ContactoEntry.Text = CleanPhoneNumber(_encargo.Contacto);
        DescripcionEntry.Text = _encargo.Descripcion;
        FechaEntregaPicker.SelectedDate = _encargo.FechaEntrega;
        FechaEntregaLabel.Text = _encargo.FechaEntrega.ToString("dd/MM/yyyy");

        // Cargar artículos existentes
        var articulos = await _repository.GetArticulosEncargoAsync(_encargo.Id);
        foreach (var articulo in articulos)
            _articulos.Add(articulo);

        ActualizarTotal();
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
        if (string.IsNullOrWhiteSpace(NombreEntry.Text))
        {
            await DisplayAlert("Error", "El nombre del cliente es obligatorio", "OK");
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

        // Actualizar encargo
        _encargo.Nombre = NombreEntry.Text.Trim();
        _encargo.Contacto = ContactoEntry.Text.Trim();
        _encargo.Descripcion = DescripcionEntry.Text?.Trim();
        _encargo.FechaEntrega = FechaEntregaPicker.SelectedDate ?? _encargo.FechaEntrega;
        _encargo.Precio = _articulos.Sum(a => a.Total);
        _encargo.Cantidad = 1;

        await _repository.SaveEncargoAsync(_encargo);

        // Eliminar artículos viejos y guardar los nuevos
        var articulosViejos = await _repository.GetArticulosEncargoAsync(_encargo.Id);
        foreach (var viejo in articulosViejos)
            await _repository.DeleteArticuloEncargoAsync(viejo);

        foreach (var articulo in _articulos)
        {
            articulo.Id = 0; // Reset ID para que sea un insert
            articulo.EncargoId = _encargo.Id;
            await _repository.SaveArticuloEncargoAsync(articulo);
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

    private string CleanPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return phoneNumber;

        return new string(phoneNumber.Where(char.IsDigit).ToArray());
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
}
