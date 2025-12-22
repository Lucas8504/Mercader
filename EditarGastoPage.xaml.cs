using static Microsoft.IO.RecyclableMemoryStreamManager;

namespace Mercader;

public partial class EditarGastoPage : ContentPage
{
    private readonly DataRepository _repo;
    private Gasto _gasto;

    public EditarGastoPage(Gasto gasto, DataRepository repo)
    {
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _gasto = gasto ?? throw new ArgumentNullException(nameof(gasto));

        InitializeComponent();
       
        DescripcionEntry.Text = _gasto.Descripcion;
        PrecioEntry.Text = _gasto.Monto.ToString();
        CantidadEntry.Text = _gasto.Cantidad.ToString();
        
    }

    private async void OnGuardarClicked(object sender, EventArgs e)
    {
        _gasto.Descripcion = DescripcionEntry.Text;
        _gasto.Monto = decimal.Parse(PrecioEntry.Text);
        _gasto.Cantidad = decimal.Parse(CantidadEntry.Text);
        

        await _repo.SaveGastoAsync(_gasto);
        await DisplayAlert("Éxito", "Gasto actualizado correctamente", "OK");
        await Navigation.PopAsync();
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
