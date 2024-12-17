using static Microsoft.IO.RecyclableMemoryStreamManager;

namespace Mercader;

public partial class VentaModal : ContentPage
{
	public VentaModal()
	{
		InitializeComponent();
	}

    private async void OnAgregarVentaClicked(object sender, EventArgs e)
    {
        var venta = new Ventas
        {
            Precio = decimal.Parse(PrecioEntry.Text),
            Cantidad = decimal.Parse(CantidadEntry.Text),
            Descripcion = DescripcionV_Entry.Text,
            Fecha = DateTime.Now
        };
        _ = await Navigation.PopModalAsync();

    }

    private async void Cancelar(object sender, EventArgs e)
    {

        await Navigation.PopModalAsync();

    }

}