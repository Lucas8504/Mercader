namespace Mercader;

public partial class EditarGastoPage : ContentPage
{
    private Gasto _gasto;

    public EditarGastoPage(Gasto gasto)
    {
        InitializeComponent();
        _gasto = gasto;
        DescripcionEntry.Text = _gasto.Descripcion;
        PrecioEntry.Text = _gasto.Monto.ToString();
        CantidadEntry.Text = _gasto.Cantidad.ToString();
        
    }

    private async void OnGuardarClicked(object sender, EventArgs e)
    {
        _gasto.Descripcion = DescripcionEntry.Text;
        _gasto.Monto = decimal.Parse(PrecioEntry.Text);
        _gasto.Cantidad = decimal.Parse(CantidadEntry.Text);
        

        await App.DataRepo.SaveGastoAsync(_gasto);
        await DisplayAlert("Éxito", "Gasto actualizado correctamente", "OK");
        await Navigation.PopAsync();
    }
}
