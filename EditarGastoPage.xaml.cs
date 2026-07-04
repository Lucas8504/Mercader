using Mercader.Domain.Entities;
using Mercader.Data.Interfaces;

namespace Mercader;

public partial class EditarGastoPage : ContentPage
{
    private readonly IDataRepository _repository;
    private Gasto _gasto;

    public EditarGastoPage(Gasto gasto, IDataRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
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

        await _repository.SaveGastoAsync(_gasto);
        await DisplayAlert("Exito", "Gasto actualizado correctamente", "OK");
        await Navigation.PopAsync();
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
