using Mercader.Domain.Entities;
using Mercader.Data.Interfaces;

namespace Mercader;

public partial class DetalleGasto : ContentPage
{
    private readonly IDataRepository _repository;
    private Gasto _gasto;

    public DetalleGasto(Gasto gasto, IDataRepository repository)
    {
        InitializeComponent();
        _gasto = gasto;
        _repository = repository;
        BindingContext = gasto;
        CalcularYMostrarTotal();
    }

    private void CalcularYMostrarTotal()
    {
        if (_gasto != null)
        {
            decimal total = _gasto.Monto * _gasto.Cantidad;
            LabelTotal.Text = $"${total:F2}";
        }
    }

    private async void OnEditarClicked(object sender, EventArgs e)
    {
        try
        {
            await Navigation.PushAsync(new EditarGastoPage(_gasto, _repository));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al abrir la pagina de edicion: {ex.Message}", "OK");
        }
    }

    private async void OnEliminarClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Confirmacion",
            $"Esta seguro de eliminar el gasto \"{_gasto.Descripcion}\" del dia {_gasto.Fecha:dd/MM/yyyy}?",
            "Si", "No");

        if (confirm)
        {
            try
            {
                await _repository.DeleteGastoAsync(_gasto);
                await DisplayAlert("Exito", "Gasto eliminado correctamente", "OK");
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al eliminar el gasto: {ex.Message}", "OK");
            }
        }
    }

    private async void OnVolverClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_gasto != null)
        {
            CalcularYMostrarTotal();
        }
    }
}
