using Mercader.Domain.Entities;

namespace Mercader;

public partial class EditarEncargoPage : ContentPage
{
    private readonly DataRepository _repo;
    private Encargo _encargo;

    public EditarEncargoPage(Encargo encargo, DataRepository repo)
    {
        InitializeComponent();
        _encargo = encargo;
        CargarDatos();
        SuscribirEventos();
        _repo = repo;
    }

    private void CargarDatos()
    {
        // Carga los datos actuales del encargo en los campos
        NombreEntry.Text = _encargo.Nombre;
        ContactoEntry.Text = CleanPhoneNumber(_encargo.Contacto);
        DescripcionEntry.Text = _encargo.Descripcion;
        FechaEntregaDatePicker.Date = _encargo.FechaEntrega;
        PrecioEntry.Text = _encargo.Precio.ToString("F2");
        CantidadEntry.Text = _encargo.Cantidad.ToString();

        // Calcular y mostrar el total actual
        CalcularTotal();
    }

    private void SuscribirEventos()
    {
        // Suscribirse a los eventos de cambio de texto para calcular el total en tiempo real
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
            // Intentar parsear precio y cantidad
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
        // Validar campos obligatorios
        if (string.IsNullOrWhiteSpace(NombreEntry.Text))
        {
            await DisplayAlert("Error", "El nombre del cliente es obligatorio", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(ContactoEntry.Text))
        {
            await DisplayAlert("Error", "Por favor, ingrese un n�mero de tel�fono", "OK");
            return;
        }

        // Validaci�n b�sica de formato de tel�fono
        if (!IsValidPhoneNumber(ContactoEntry.Text))
        {
            await DisplayAlert("Error", "Por favor, ingrese un n�mero de tel�fono v�lido", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(DescripcionEntry.Text))
        {
            await DisplayAlert("Error", "La descripci�n del producto es obligatoria", "OK");
            return;
        }

        try
        {
            // Validar y parsear precio
            if (!decimal.TryParse(PrecioEntry.Text, out decimal precio) || precio <= 0)
            {
                await DisplayAlert("Error", "El precio debe ser un n�mero v�lido mayor a 0", "OK");
                return;
            }

            // Validar y parsear cantidad
            if (!decimal.TryParse(CantidadEntry.Text, out decimal cantidad) || cantidad <= 0)
            {
                await DisplayAlert("Error", "La cantidad debe ser un n�mero v�lido mayor a 0", "OK");
                return;
            }

            // Actualizar el encargo con los nuevos datos
            _encargo.Nombre = NombreEntry.Text.Trim();
            _encargo.Contacto = ContactoEntry.Text.Trim();
            _encargo.Descripcion = DescripcionEntry.Text.Trim();
            _encargo.FechaEntrega = FechaEntregaDatePicker.Date;
            _encargo.Precio = precio;
            _encargo.Cantidad = cantidad;

            // Guardar en la base de datos
            await _repo.SaveEncargoAsync(_encargo);

            // Mostrar mensaje de �xito
            await DisplayAlert("�xito", "Encargo actualizado correctamente", "OK");

            // Volver a la p�gina anterior
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

    private string CleanPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return phoneNumber;

        // Remover todos los caracteres que no sean n�meros
        return new string(phoneNumber.Where(char.IsDigit).ToArray());
    }

    private async void OnCancelarClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert(
            "Confirmar",
            "�Est�s seguro de que deseas cancelar? Se perder�n los cambios no guardados.",
            "S�",
            "No");

        if (confirm)
        {
            await Navigation.PopAsync();
        }
    }
}
