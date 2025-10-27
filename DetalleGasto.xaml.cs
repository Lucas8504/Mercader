namespace Mercader;

/// <summary>
/// Página de detalle que muestra la información completa de un gasto específico.
/// Permite visualizar, editar y eliminar el gasto seleccionado.
/// </summary>
public partial class DetalleGasto : ContentPage
{
    /// <summary>
    /// Instancia del gasto que se está visualizando en esta página.
    /// </summary>
    private Gasto _gasto;

    /// <summary>
    /// Constructor de la página DetalleGasto.
    /// </summary>
    /// <param name="gasto">El objeto Gasto que se desea visualizar y gestionar.</param>
    public DetalleGasto(Gasto gasto)
    {
        InitializeComponent();
        _gasto = gasto;

        // Establece el contexto de enlace para el binding de datos en la interfaz
        BindingContext = gasto;

        // Calcula y muestra el total inicial
        CalcularYMostrarTotal();
    }

    /// <summary>
    /// Calcula el total del gasto (Monto × Cantidad) y actualiza el label correspondiente en la UI.
    /// </summary>
    private void CalcularYMostrarTotal()
    {
        if (_gasto != null)
        {
            // Calcula el total multiplicando el monto unitario por la cantidad
            decimal total = _gasto.Monto * _gasto.Cantidad;

            // Formatea el total con 2 decimales y símbolo de peso
            LabelTotal.Text = $"${total:F2}";
        }
    }

    /// <summary>
    /// Manejador del evento Click del botón Editar.
    /// Navega a la página de edición del gasto actual.
    /// </summary>
    /// <param name="sender">El objeto que generó el evento.</param>
    /// <param name="e">Argumentos del evento.</param>
    private async void OnEditarClicked(object sender, EventArgs e)
    {
        try
        {
            // Navega a la página de edición pasando el gasto actual
            await Navigation.PushAsync(new EditarGastoPage(_gasto));
        }
        catch (Exception ex)
        {
            // Muestra un mensaje de error si falla la navegación
            await DisplayAlert("Error", $"Error al abrir la página de edición: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Manejador del evento Click del botón Eliminar.
    /// Solicita confirmación y elimina el gasto de la base de datos.
    /// </summary>
    /// <param name="sender">El objeto que generó el evento.</param>
    /// <param name="e">Argumentos del evento.</param>
    private async void OnEliminarClicked(object sender, EventArgs e)
    {
        // Solicita confirmación al usuario antes de eliminar
        bool confirm = await DisplayAlert("Confirmación",
            $"¿Estás seguro de eliminar el gasto \"{_gasto.Descripcion}\" del día {_gasto.Fecha:dd/MM/yyyy}?",
            "Sí", "No");

        if (confirm)
        {
            try
            {
                // Elimina el gasto del repositorio de datos
                await App.DataRepo.DeleteGastoAsync(_gasto);

                // Muestra mensaje de éxito
                await DisplayAlert("Éxito", "Gasto eliminado correctamente", "OK");

                // Regresa a la página anterior después de eliminar
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                // Muestra mensaje de error si falla la eliminación
                await DisplayAlert("Error", $"Error al eliminar el gasto: {ex.Message}", "OK");
            }
        }
    }

    /// <summary>
    /// Manejador del evento Click del botón Volver.
    /// Regresa a la página anterior en la pila de navegación.
    /// </summary>
    /// <param name="sender">El objeto que generó el evento.</param>
    /// <param name="e">Argumentos del evento.</param>
    private async void OnVolverClicked(object sender, EventArgs e)
    {
        // Navega hacia atrás en la pila de navegación
        await Navigation.PopAsync();
    }

    /// <summary>
    /// Método del ciclo de vida que se ejecuta cuando la página aparece en pantalla.
    /// Actualiza los datos mostrados en caso de que el gasto haya sido modificado.
    /// </summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Recalcula y actualiza el total por si el gasto fue modificado
        if (_gasto != null)
        {
            CalcularYMostrarTotal();
        }
    }
}