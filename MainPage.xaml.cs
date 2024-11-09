using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mercader
{
    public partial class MainPage : ContentPage
    {
        public Balance balance;

        public MainPage()
        {
            InitializeComponent();
            balance = new Balance(); // Inicializar la variable balance

        }


       

        private async void InAgregarEncargo(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new EncModal());


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

        

        private async void InAgregarGasto(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new GastoModal());


        }
        private void OnCalcularGananciasClicked(object sender, EventArgs e)
        {
            var ganancias = balance.CalcularGanancias();
            GananciasLabel.Text = $"Ganancias: {ganancias:C}";
        }


        private void OnExportarAExcelClicked(object sender, EventArgs e)
        {
            string rutaArchivo = Path.Combine(FileSystem.AppDataDirectory, "balance.xlsx");
            ExportExcel.ExportarBalanceAExcel(balance, rutaArchivo);
            DisplayAlert("Exportación Completa", $"Archivo exportado a {rutaArchivo}", "OK");
        }

       
    }

}
