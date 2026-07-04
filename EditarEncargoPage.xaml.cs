using Mercader.Domain.Entities;
using Mercader.Data.Interfaces;

namespace Mercader;

public partial class EditarEncargoPage : ContentPage
{
    private readonly IDataRepository _repository;
    private Encargo _encargo;

    public EditarEncargoPage(Encargo encargo, IDataRepository repository)
    {
        InitializeComponent();
        _encargo = encargo;
        _repository = repository;
        CargarDatos();
        SuscribirEventos();
    }

    private void CargarDatos()
    {
        NombreEntry.Text = _encargo.Nombre;
        ContactoEntry.Text = CleanPhoneNumber(_encargo.Contacto);
        DescripcionEntry.Text = _encargo.Descripcion;
        FechaEntregaDatePicker.Date = _encargo.FechaEntrega;
        PrecioEntry.Text = _encargo.Precio.ToString("F2");
        CantidadEntry.Text = _encargo.Cantidad.ToString();
        CalcularTotal();
    }

    private void SuscribirEventos()
    {
        PrecioEntry.TextChanged += OnPrecioOCantidadChanged!;
        CantidadEntry.TextChanged += OnPrecioOCantidadChanged!;
    }

    private void OnPrecioOCantidadChanged(object? sender, TextChangedEventArgs e)
    {
        CalcularTotal();
    }

    private void CalcularTotal()
    {
        try
        {
            decimal precio = 0;
            decimal cantidad = 0;

            if (decimal.TryParse(PrecioEntry.Text, out precio) &&
                decimal.TryParse(CantidadEntry.Text, out cantidad))
            {
                var total = precio * cantidad;
                LabelTotalCalculado.Text = $"${total:F2}";
            }
            else
            {
                LabelTotalCalculado.Text = "$0.00";
            }
        }
        catch
        {
            LabelTotalCalculado.Text = "$0.00";
        }
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
            await DisplayAlert("Error", "Por favor, ingrese un numero de telefono", "OK");
            return;
        }

        if (!IsValidPhoneNumber(ContactoEntry.Text))
        {
            await DisplayAlert("Error", "Por favor, ingrese un numero de telefono valido", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(DescripcionEntry.Text))
        {
            await DisplayAlert("Error", "La descripcion del producto es obligatoria", "OK");
            return;
        }

        try
        {
            if (!decimal.TryParse(PrecioEntry.Text, out decimal precio) || precio <= 0)
            {
                await DisplayAlert("Error", "El precio debe ser un numero valido mayor a 0", "OK");
                return;
            }

            if (!decimal.TryParse(CantidadEntry.Text, out decimal cantidad) || cantidad <= 0)
            {
                await DisplayAlert("Error", "La cantidad debe ser un numero valido mayor a 0", "OK");
                return;
            }

            _encargo.Nombre = NombreEntry.Text.Trim();
            _encargo.Contacto = ContactoEntry.Text.Trim();
            _encargo.Descripcion = DescripcionEntry.Text.Trim();
            _encargo.FechaEntrega = FechaEntregaDatePicker.Date;
            _encargo.Precio = precio;
            _encargo.Cantidad = cantidad;

            await _repository.SaveEncargoAsync(_encargo);
            await DisplayAlert("Exito", "Encargo actualizado correctamente", "OK");
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al guardar el encargo: {ex.Message}", "OK");
        }
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
            "Esta seguro de que deseas cancelar? Se perderan los cambios no guardados.",
            "Si",
            "No");

        if (confirm)
        {
            await Navigation.PopAsync();
        }
    }
}
