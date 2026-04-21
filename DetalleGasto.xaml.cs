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

        // Establece el contexto de enlace para el binding de datos en la interfaz
        BindingContext = gasto;

        // Calcula y muestra el total inicial
        CalcularYMostrarTotal();
    }

    /// <summary>
    /// Calcula el total del gasto (Monto � Cantidad) y actualiza el label correspondiente en la UI.
    /// </summary>
    private void CalcularYMostrarTotal()
    {
        if (_gasto != null)
        {
            // Calcula el total multiplicando el monto unitario por la cantidad
            decimal total = _gasto.Monto * _gasto.Cantidad;

            // Formatea el total con 2 decimales y s�mbolo de peso
            LabelTotal.Text = $"${total:F2}";
        }
    }

    /// <summary>
    /// Manejador del evento Click del bot�n Editar.
    /// Navega a la p�gina de edici�n del gasto actual.
    /// </summary>
    /// <param name="sender">El objeto que gener� el evento.</param>
    /// <param name="e">Argumentos del evento.</param>
    private async void OnEditarClicked(object sender, EventArgs e)
    {
        try
        {
            // Navega a la p�gina de edici�n pasando el gasto actual
            await Navigation.PushAsync(new EditarGastoPage(_gasto, _repository));
        }
        catch (Exception ex)
        {
            // Muestra un mensaje de error si falla la navegaci�n
            await DisplayAlert("Error", $"Error al abrir la p�gina de edici�n: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Manejador del evento Click del bot�n Eliminar.
    /// Solicita confirmaci�n y elimina el gasto de la base de datos.
    /// </summary>
    /// <param name="sender">El objeto que gener� el evento.</param>
    /// <param name="e">Argumentos del evento.</param>
    private async void OnEliminarClicked(object sender, EventArgs e)
    {
        // Solicita confirmaci�n al usuario antes de eliminar
        bool confirm = await DisplayAlert("Confirmaci�n",
            $"�Est�s seguro de eliminar el gasto \"{_gasto.Descripcion}\" del d�a {_gasto.Fecha:dd/MM/yyyy}?",
            "S�", "No");

        if (confirm)
        {
            try
            {
                // Elimina el gasto del repositorio de datos
                await _repository.DeleteGastoAsync(_gasto);

                // Muestra mensaje de �xito
                await DisplayAlert("�xito", "Gasto eliminado correctamente", "OK");

                // Regresa a la p�gina anterior despu�s de eliminar
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                // Muestra mensaje de error si falla la eliminaci�n
                await DisplayAlert("Error", $"Error al eliminar el gasto: {ex.Message}", "OK");
            }
        }
    }

    /// <summary>
    /// Manejador del evento Click del bot�n Volver.
    /// Regresa a la p�gina anterior en la pila de navegaci�n.
    /// </summary>
    /// <param name="sender">El objeto que gener� el evento.</param>
    /// <param name="e">Argumentos del evento.</param>
    private async void OnVolverClicked(object sender, EventArgs e)
    {
        // Navega hacia atr�s en la pila de navegaci�n
        await Navigation.PopAsync();
    }

    /// <summary>
    /// M�todo del ciclo de vida que se ejecuta cuando la p�gina aparece en pantalla.
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