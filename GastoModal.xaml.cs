using System.Globalization;
using Mercader.Domain.Entities;
using Mercader.Data.Interfaces;
#if ANDROID
using Mercader.Platforms.Android;
#endif

namespace Mercader;

public partial class GastoModal : ContentPage
{
    private readonly IDataRepository _repository;
    public Gasto Gasto { get; private set; } = null!;

    public GastoModal(IDataRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);

        InitializeComponent();
        _repository = repository;
    }

    private async void OnAgregarGastoClicked(object sender, EventArgs e)
    {
        if (!ValidateG_Entries())
            return;

        try
        {
            Gasto = new Gasto
            {
                Descripcion = DescripcionGastoEntry.Text,
                Cantidad = decimal.Parse(CantidadG_Entry!.Text!, CultureInfo.InvariantCulture),
                Monto = decimal.Parse(MontoGastoEntry!.Text!, CultureInfo.InvariantCulture),
                Fecha = DateTime.Now
            };
        }
        catch (FormatException)
        {
            await DisplayAlert("Error", "Por favor, ingrese valores num�ricos v�lidos", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al agregar gasto: {ex.Message}", "OK");
        }
        await SaveGastoAsync();
    }

    private async Task SaveGastoAsync()
    {
        await _repository.SaveGastoAsync(Gasto);
#if ANDROID
        KeyboardHelper.Close();
#endif
        await Navigation.PopModalAsync();
    }

    private bool ValidateG_Entries()
    {
        if (string.IsNullOrWhiteSpace(DescripcionGastoEntry.Text))
        {
            DisplayAlert("Error", "Por favor, ingrese una descripci�n", "OK");
            return false;
        }
        if (string.IsNullOrWhiteSpace(MontoGastoEntry.Text))
        {
            DisplayAlert("Error", "Por favor, ingrese un Monto", "OK");
            return false;
        }
        if (string.IsNullOrWhiteSpace(CantidadG_Entry.Text))
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
