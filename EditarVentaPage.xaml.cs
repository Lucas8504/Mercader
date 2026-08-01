using System.Collections.ObjectModel;
using Mercader.Domain.Entities;
using Mercader.Data.Interfaces;

namespace Mercader;

public partial class EditarVentaPage : ContentPage
{
    private readonly IDataRepository _repository;
    private readonly Ventas _venta;
    private readonly ObservableCollection<ArticuloVenta> _articulos = new();

    public EditarVentaPage(Ventas venta, IDataRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _venta = venta ?? throw new ArgumentNullException(nameof(venta));
        InitializeComponent();
        BindableLayout.SetItemsSource(ArticulosStack, _articulos);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        DescripcionEntry.Text = _venta.Descripcion;

        // Cargar artículos existentes
        var articulos = await _repository.GetArticulosVentaAsync(_venta.Id);
        foreach (var articulo in articulos)
            _articulos.Add(articulo);

        ActualizarTotal();
    }

    private async void OnGuardarClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(DescripcionEntry.Text))
        {
            await DisplayAlert("Error", "La descripción es requerida", "OK");
            return;
        }

        if (_articulos.Count == 0)
        {
            await DisplayAlert("Error", "Debe agregar al menos un artículo", "OK");
            return;
        }

        var articuloInvalido = _articulos.FirstOrDefault(a => string.IsNullOrWhiteSpace(a.Descripcion));
        if (articuloInvalido is not null)
        {
            await DisplayAlert("Error", "Cada artículo debe tener una descripción", "OK");
            return;
        }

        // Actualizar venta
        _venta.Descripcion = DescripcionEntry.Text;
        _venta.Precio = _articulos.Sum(a => a.Total);
        _venta.Cantidad = 1;

        await _repository.SaveVentasAsync(_venta);

        // Eliminar artículos viejos y guardar los nuevos
        var articulosViejos = await _repository.GetArticulosVentaAsync(_venta.Id);
        foreach (var viejo in articulosViejos)
            await _repository.DeleteArticuloVentaAsync(viejo);

        foreach (var articulo in _articulos)
        {
            articulo.Id = 0; // Reset ID para que sea un insert
            articulo.VentaId = _venta.Id;
            await _repository.SaveArticuloVentaAsync(articulo);
        }

        await DisplayAlert("Éxito", "Venta actualizada correctamente", "OK");
        await Navigation.PopAsync();
    }

    private async void OnCancelarClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert(
            "Confirmar",
            "¿Estás seguro de que deseas cancelar? Se perderán los cambios no guardados.",
            "Sí",
            "No");

        if (confirm)
            await Navigation.PopAsync();
    }

    // ===== MULTI-ARTÍCULO =====

    private void OnAgregarArticuloClicked(object sender, EventArgs e)
    {
        var articulo = new ArticuloVenta { Orden = _articulos.Count + 1 };
        _articulos.Add(articulo);
        ActualizarTotal();
    }

    private void OnSubirArticulo(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is ArticuloVenta articulo)
        {
            var idx = _articulos.IndexOf(articulo);
            if (idx <= 0) return;
            _articulos.Move(idx, idx - 1);
            ReordenarArticulos();
            ActualizarTotal();
        }
    }

    private void OnBajarArticulo(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is ArticuloVenta articulo)
        {
            var idx = _articulos.IndexOf(articulo);
            if (idx < 0 || idx >= _articulos.Count - 1) return;
            _articulos.Move(idx, idx + 1);
            ReordenarArticulos();
            ActualizarTotal();
        }
    }

    private void OnEliminarArticulo(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is ArticuloVenta articulo)
        {
            _articulos.Remove(articulo);
            ReordenarArticulos();
            ActualizarTotal();
        }
    }

    private void ReordenarArticulos()
    {
        for (int i = 0; i < _articulos.Count; i++)
            _articulos[i].Orden = i + 1;
    }

    private void ActualizarTotal()
    {
        var total = _articulos.Sum(a => a.Total);
        TotalLabel.Text = $"${total:N0}";
    }

    private void OnArticuloFieldChanged(object? sender, TextChangedEventArgs e)
    {
        ActualizarTotal();
    }
}
