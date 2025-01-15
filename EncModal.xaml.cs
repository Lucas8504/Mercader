namespace Mercader;

public partial class EncModal : ContentPage
{
    private readonly MainPage _mainPage;  // Agregar esta línea
    private Encargo _encargo = null!; // Null forgiving operator
    public EncModal(MainPage mainPage)
    {
        ArgumentNullException.ThrowIfNull(mainPage);


        InitializeComponent();
        this._mainPage = mainPage;

        _encargo = new()
        {
            Nombre = string.Empty,
            Precio = 0,
            Cantidad = 0,
            Descripcion = string.Empty,
            Fecha = DateTime.Now
        };
    }

    private async void OnAgregarEncargoClicked(object sender, EventArgs e)
    {
        if (!ValidateGEntries())
            return;

        try
        {
            _encargo = new Encargo
            {
                Nombre = NombreEntry.Text,
                Cantidad = decimal.Parse(CantidadEntry!.Text!, CultureInfo.InvariantCulture),
                Precio = decimal.Parse(PrecioEntry!.Text!, CultureInfo.InvariantCulture),
                Descripcion = DescripcionEntry.Text,
                Fecha = DateTime.Now
            };
            // Usar directamente la referencia a mainPage
            _mainPage.balance.Encargos.Add(_encargo);
            _mainPage.ActualizarEtiquetaEncargos();
            await Navigation.PopModalAsync();
        }
        catch (FormatException)
        {
            await DisplayAlert("Error", "Por favor, ingrese valores numéricos válidos", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al agregar encargo: {ex.Message}", "OK");
        }

    }

    private async Task SaveEncargoAsync()
    {
        _mainPage.balance.Encargos.Add(_encargo);
        await App.DataRepo.SaveEncargoAsync(_encargo);
        _mainPage.ActualizarEtiquetaEncargos();
        await Navigation.PopModalAsync();
    }

    private bool ValidateGEntries()
    {
        if (string.IsNullOrWhiteSpace(NombreEntry.Text))
        {
            DisplayAlert("Error", "Por favor, ingrese un nombre", "OK");
            return false;
        }
        if (string.IsNullOrWhiteSpace(PrecioEntry.Text))
        {
            DisplayAlert("Error", "Por favor, ingrese un precio", "OK");
            return false;
        }
        if (string.IsNullOrWhiteSpace(CantidadEntry.Text))
        {
            DisplayAlert("Error", "Por favor, ingrese una cantidad", "OK");
            return false;
        }
        if (string.IsNullOrWhiteSpace(DescripcionEntry.Text))
        {
            DisplayAlert("Error", "Por favor, ingrese una descripción", "OK");
            return false;
        }
        return true;
    }


    private async void Cancelar(object sender, EventArgs e)
    {

        await Navigation.PopModalAsync();

    }

}