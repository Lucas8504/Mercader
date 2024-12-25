namespace Mercader;

public partial class EncModal : ContentPage
{
    private MainPage mainPage;  // Agregar esta línea
    public EncModal(MainPage mainPage)
	{
		InitializeComponent();
        this.mainPage = mainPage;
    }

    private async void OnAgregarEncargoClicked(object sender, EventArgs e)
    {
        var encargo = new Encargo
        {
            Nombre = EncargoEntry.Text,
            Precio = decimal.Parse(PrecioEntry.Text),
            Cantidad = decimal.Parse(CantidadEntry.Text),
            Descripcion = DescripcionEntry.Text,
            Fecha = DateTime.Now,


        };
        // Usar directamente la referencia a mainPage
        mainPage.balance.Encargos.Add(encargo);
        mainPage.ActualizarEtiquetaEncargos();
        await Navigation.PopModalAsync();
    }

    private async void Cancelar(object sender, EventArgs e)
    {

        await Navigation.PopModalAsync();

    }

}