namespace Mercader;

public partial class GastoModal : ContentPage
{
    private MainPage mainPage;  // Agregar esta línea
    public GastoModal(MainPage mainPage)
	{
		InitializeComponent();
        this.mainPage = mainPage;
    }

    private async void OnAgregarGastoClicked(object sender, EventArgs e)
    {
        try
        {
            var gasto = new Gasto
            {
                Descripcion = DescripcionGastoEntry.Text,
                Monto = decimal.Parse(MontoGastoEntry.Text),
                Fecha = DateTime.Now
            };
            // Usar directamente la referencia a mainPage
            mainPage.balance.Gastos.Add(gasto);
            mainPage.ActualizarEtiquetaGastos();
            await Navigation.PopModalAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al agregar venta: {ex.Message}", "OK");
        }

    }


    private async void Cancelar(object sender, EventArgs e)
	{

	  await Navigation.PopModalAsync();

	}
}