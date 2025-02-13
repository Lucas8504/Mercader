namespace Mercader;

public partial class EditarEncargoPage : ContentPage
{
    private Encargo _encargo;

    public EditarEncargoPage(Encargo encargo)
    {
        InitializeComponent();
        _encargo = encargo;
        NombreEntry.Text = _encargo.Nombre;
        FechaDatePicker.Date = _encargo.Fecha;
    }

    private async void OnGuardarClicked(object sender, EventArgs e)
    {
        _encargo.Nombre = NombreEntry.Text;
        _encargo.Fecha = FechaDatePicker.Date;

        await App.DataRepo.SaveEncargoAsync(_encargo);
        await DisplayAlert("Éxito", "Encargo actualizado correctamente", "OK");
        await Navigation.PopAsync();
    }
}
