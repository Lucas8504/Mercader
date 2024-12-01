global using System;
global using Microsoft.Maui;
global using Microsoft.Maui.Controls;
global using Microsoft.Maui.Controls.Hosting;
global using Microsoft.Maui.Hosting;
global using Microsoft.Maui.Graphics;
namespace Mercader;

public partial class VentaModal : ContentPage
{
	public VentaModal()
	{
		InitializeComponent();
	}

    private void OnAgregarVentaClicked(object sender, EventArgs e)
    {
        var venta = new Ventas
        {
            Precio = decimal.Parse(PrecioEntry.Text),
            Cantidad = decimal.Parse(CantidadEntry.Text),
            Descripcion = DescripcionV_Entry.Text,
            Fecha = DateTime.Now
        };


    }

    private async void Cancelar(object sender, EventArgs e)
    {

        await Navigation.PopModalAsync();

    }

}