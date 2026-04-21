using Mercader.Domain.Entities;
using Mercader.Data.Interfaces;

namespace Mercader;

public partial class EditarVentaPage : ContentPage
{
    private readonly IDataRepository _repository;
    private readonly Ventas _venta;

    public EditarVentaPage(Ventas venta, IDataRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _venta = venta ?? throw new ArgumentNullException(nameof(venta));

        InitializeComponent();

        DescripcionEntry.Text = _venta.Descripcion;
        PrecioEntry.Text = _venta.Precio.ToString();
        CantidadEntry.Text = _venta.Cantidad.ToString();
    }

    private async void OnGuardarClicked(object sender, EventArgs e)
    {
        _venta.Descripcion = DescripcionEntry.Text;
        _venta.Precio = decimal.Parse(PrecioEntry.Text);
        _venta.Cantidad = decimal.Parse(CantidadEntry.Text);

        await _repository.SaveVentasAsync(_venta);
        await DisplayAlert("�xito", "Venta actualizada correctamente", "OK");
        await Navigation.PopAsync();
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
