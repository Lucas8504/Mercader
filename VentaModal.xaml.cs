using System.Globalization;
using Mercader.Domain.Entities;
using Mercader.Data.Interfaces;
#if ANDROID
using Mercader.Platforms.Android;
#endif


namespace Mercader;

public partial class VentaModal : ContentPage
{
    
    private readonly IDataRepository _repository;
    public Ventas Venta { get; private set; } = null!;


    public VentaModal(IDataRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);

        InitializeComponent();
        _repository = repository;
    }


    private async void OnAgregarVentaClicked(object sender, EventArgs e)
    {
        // Validar usando ValidationService
        var descValidation = ValidationService.ValidateRequired(DescripcionV_Entry.Text, "una descripción");
        if (!descValidation.IsValid)
        {
            await DisplayAlert("Error", descValidation.ErrorMessage, "OK");
            return;
        }

        var precioResult = ValidationService.ParseDecimal(PrecioEntry.Text);
        if (!precioResult.Success)
        {
            await DisplayAlert("Error", precioResult.Error ?? "Precio inválido", "OK");
            return;
        }

        var cantidadResult = ValidationService.ParseDecimal(CantidadEntry.Text);
        if (!cantidadResult.Success)
        {
            await DisplayAlert("Error", cantidadResult.Error ?? "Cantidad inválida", "OK");
            return;
        }

        try
        {
            Venta = new Ventas
            {
                Precio = precioResult.Value ?? 0,
                Cantidad = cantidadResult.Value ?? 0,
                Descripcion = DescripcionV_Entry.Text,
                Fecha = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al crear venta: {ex.Message}", "OK");
            return;
        }
        
        await SaveVentaAsync();
    }

    private async Task SaveVentaAsync()
    {
        
        await _repository.SaveVentasAsync(Venta);

#if ANDROID
        KeyboardHelper.Close();
#endif

        await Navigation.PopModalAsync();
    }

    private async void Cancelar(object sender, EventArgs e)
    {

#if ANDROID
        KeyboardHelper.Close();
#endif

        await Navigation.PopModalAsync();

    }

}