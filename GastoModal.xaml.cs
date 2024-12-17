namespace Mercader;

public partial class GastoModal : ContentPage
{
	public GastoModal()
	{
		InitializeComponent();
	}

    private async void OnAgregarGastoClicked(object sender, EventArgs e)
    {
        var gasto = new Gasto
        {
            Descripcion = DescripcionGastoEntry.Text,
            Monto = decimal.Parse(MontoGastoEntry.Text),
            Fecha = DateTime.Now
        };
        await Navigation.PopModalAsync();


    }


    private async void Cancelar(object sender, EventArgs e)
	{

	  await Navigation.PopModalAsync();

	}
}