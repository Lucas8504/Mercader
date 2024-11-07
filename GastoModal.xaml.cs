namespace Mercader;

public partial class GastoModal : ContentPage
{
	public GastoModal()
	{
		InitializeComponent();
	}

    private void OnAgregarGastoClicked(object sender, EventArgs e)
    {
        var gasto = new Gasto
        {
            Descripcion = DescripcionGastoEntry.Text,
            Monto = decimal.Parse(MontoGastoEntry.Text),
            Fecha = DateTime.Now
        };
       
       
    }


    private async void Cancelar(object sender, EventArgs e)
	{

	  await Navigation.PopModalAsync();

	}
}