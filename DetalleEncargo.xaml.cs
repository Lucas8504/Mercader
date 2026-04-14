using Mercader.Models.Domain;

namespace Mercader;

public partial class DetalleEncargo : ContentPage
{
    private readonly DataRepository _repo;
    private Encargo _encargo;

    public DetalleEncargo(Encargo encargo, DataRepository repo)
    {
        InitializeComponent();
        _encargo = encargo;
        BindingContext = _encargo;
        CalcularTotal();
        _repo = repo;
    }

    private void CalcularTotal()
    {
        if (_encargo != null)
        {
            var total = _encargo.Precio * _encargo.Cantidad;
            LabelTotal.Text = $"${total:F2}";
        }
    }

    private async void OnConcretarVentaClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert(
            "Confirmación",
            $"¿Realmente deseas concretar la venta de {_encargo.Descripcion} para \"{_encargo.Nombre}\" pedido el día: {_encargo.Fecha:dd/MM/yyyy}?",
            "Sí",
            "No");

        if (!confirm) return;

        try
        {
            var venta = new Ventas
            {
                Descripcion = _encargo.Descripcion,
                Precio = _encargo.Precio,
                Cantidad = _encargo.Cantidad,
                Fecha = DateTime.Now
            };

            await _repo.SaveVentasAsync(venta);
            await _repo.DeleteEncargoAsync(_encargo);

            await Navigation.PopAsync(); // 👈 volver
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void OnEditarClicked(object sender, EventArgs e)
    {
        try
        {
            await Navigation.PushAsync(new EditarEncargoPage(_encargo, _repo));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al abrir la página de edición: {ex.Message}", "OK");
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ActualizarDatos();
    }

    private async void ActualizarDatos()
    {
        try
        {
            // Recargar el encargo desde la base de datos para obtener los datos más recientes
            var encargosActualizados = await _repo.GetEncargosAsync();
            if (encargosActualizados != null && encargosActualizados.Any())
            {
                // Asumimos que queremos el encargo correspondiente al ID actual
                var encargoActualizado = encargosActualizados.FirstOrDefault(e => e.Id == _encargo.Id);
                if (encargoActualizado != null)
                {
                    _encargo = encargoActualizado; // Asignación segura
                    BindingContext = _encargo;
                    CalcularTotal();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al actualizar datos: {ex.Message}");
        }
    }

    private async void OnEliminarClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert(
            "Confirmación",
            $"¿Realmente deseas eliminar el encargo de \"{_encargo.Nombre}\" hecho el día: {_encargo.Fecha:dd/MM/yyyy}?",
            "Sí",
            "No");

        if (confirm)
        {
            try
            {
                await _repo.DeleteEncargoAsync(_encargo);
                await DisplayAlert("Éxito", "Encargo eliminado correctamente", "OK");
                await Navigation.PopAsync(); // Volver a la página anterior
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al eliminar el encargo: {ex.Message}", "OK");
            }
        }
    }

    private async void OnVolverClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}