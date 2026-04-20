using System.Globalization;
using Mercader.Domain.Entities;
#if ANDROID
using Mercader.Platforms.Android;
#endif


namespace Mercader;

public partial class VentaModal : ContentPage
{
    
    private readonly DataRepository _repo;
    public Ventas Venta { get; private set; } = null!; // Null forgiving operator


    public VentaModal( DataRepository repo)
    {
        
        ArgumentNullException.ThrowIfNull(repo);

        InitializeComponent();
        
        _repo = repo;


    }


    private async void OnAgregarVentaClicked(object sender, EventArgs e)
    {
        if (!ValidateV_Entries())
            return;


        try
        {
            Venta = new Ventas
            {
                Precio = decimal.Parse(PrecioEntry!.Text!, CultureInfo.InvariantCulture),
                Cantidad = decimal.Parse(CantidadEntry!.Text!, CultureInfo.InvariantCulture),
                Descripcion = DescripcionV_Entry!.Text!,
                Fecha = DateTime.Now
            };
            
        }
        catch (FormatException)
        {
            await DisplayAlert("Error", "Por favor, ingrese valores num�ricos v�lidos", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al agregar venta: {ex.Message}", "OK");
        }
        await SaveVentaAsync();
    }

    private async Task SaveVentaAsync()
    {
        
        await _repo.SaveVentasAsync(Venta);

#if ANDROID
        KeyboardHelper.Close();
#endif

        await Navigation.PopModalAsync();
    }

    private bool ValidateV_Entries()
    {
        
        if (string.IsNullOrWhiteSpace(DescripcionV_Entry.Text))
        {
            DisplayAlert("Error", "Por favor, ingrese una descripci�n", "OK");
            return false;
        }
        if (string.IsNullOrWhiteSpace(PrecioEntry.Text))
        {
            DisplayAlert("Error", "Por favor, ingrese un precio", "OK");
            return false;
        }
        if (string.IsNullOrWhiteSpace(CantidadEntry.Text))
        {
            DisplayAlert("Error", "Por favor, ingrese una cantidad", "OK");
            return false;
        }

        return true;
    }



    private async void Cancelar(object sender, EventArgs e)
    {

#if ANDROID
        KeyboardHelper.Close();
#endif

        await Navigation.PopModalAsync();

    }

}