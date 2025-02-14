namespace Mercader;

public partial class EditarEncargoPage : ContentPage
{
    private Encargo _encargo;

    public EditarEncargoPage(Encargo encargo)
    {
        InitializeComponent();
        _encargo = encargo;
        NombreEntry.Text = _encargo.Nombre;
        DescripcionEntry.Text = _encargo.Descripcion;
        FechaEntregaDatePicker.Date = _encargo.FechaEntrega;
        PrecioEntry.Text = _encargo.Precio.ToString();
        CantidadEntry.Text = _encargo.Cantidad.ToString();
    }

    private async void OnGuardarClicked(object sender, EventArgs e)
    {
        _encargo.Nombre = NombreEntry.Text;
        _encargo.Descripcion = DescripcionEntry.Text;
        _encargo.FechaEntrega = FechaEntregaDatePicker.Date;
        _encargo.Precio = decimal.Parse(PrecioEntry.Text);
        _encargo.Cantidad = decimal.Parse(CantidadEntry.Text);

        await App.DataRepo.SaveEncargoAsync(_encargo);
        await DisplayAlert("Éxito", "Encargo actualizado correctamente", "OK");
        await Navigation.PopAsync();
    }
}
