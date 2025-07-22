namespace Mercader;

public partial class EditarEncargoPage : ContentPage
{
    private Encargo _encargo;

    public EditarEncargoPage(Encargo encargo)
    {
        InitializeComponent();
        _encargo = encargo;
        CargarDatos();
        SuscribirEventos();
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
            await DisplayAlert("Error", "Por favor, ingrese un número de teléfono", "OK");
            return;
        }

        // Validación básica de formato de teléfono
        if (!IsValidPhoneNumber(ContactoEntry.Text))
        {
            await DisplayAlert("Error", "Por favor, ingrese un número de teléfono válido", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(DescripcionEntry.Text))
        {
            await DisplayAlert("Error", "La descripción del producto es obligatoria", "OK");
            return;
        }

        try
        {
            // Validar y parsear precio
            if (!decimal.TryParse(PrecioEntry.Text, out decimal precio) || precio <= 0)
            {
                await DisplayAlert("Error", "El precio debe ser un número válido mayor a 0", "OK");
                return;
            }

            // Validar y parsear cantidad
            if (!decimal.TryParse(CantidadEntry.Text, out decimal cantidad) || cantidad <= 0)
            {
                await DisplayAlert("Error", "La cantidad debe ser un número válido mayor a 0", "OK");
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
            await App.DataRepo.SaveEncargoAsync(_encargo);

            // Mostrar mensaje de éxito
            await DisplayAlert("Éxito", "Encargo actualizado correctamente", "OK");

            // Volver a la página anterior
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

        // Remover espacios, guiones, paréntesis y el signo +
        string cleanedNumber = phoneNumber.Replace(" ", "")
                                        .Replace("-", "")
                                        .Replace("(", "")
                                        .Replace(")", "")
                                        .Replace("+", "");

        // Verificar que solo contenga números y tenga entre 7 y 15 dígitos
        return cleanedNumber.All(char.IsDigit) &&
               cleanedNumber.Length >= 7 &&
               cleanedNumber.Length <= 15;
    }

    private string CleanPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return phoneNumber;

        // Remover todos los caracteres que no sean números
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
        {
            await Navigation.PopAsync();
        }
    }
}
