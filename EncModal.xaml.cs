namespace Mercader;

public partial class EncModal : ContentPage
{
	public EncModal()
	{
		InitializeComponent();
	}

    private void OnAgregarEncargoClicked(object sender, EventArgs e)
    {
        var encargo = new Encargo
        {
            Precio = decimal.Parse(PrecioEntry.Text),
            //Cantidad
            Descripcion = EncargoEntry.Text
        };
        // Aqu� puedes agregar el encargo a una lista si es necesario
    }

    private async void Cancelar(object sender, EventArgs e)
    {

        await Navigation.PopModalAsync();

    }

}