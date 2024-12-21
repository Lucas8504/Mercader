
using Microsoft.Data.Sqlite;
using SQLite;
using static Microsoft.IO.RecyclableMemoryStreamManager;

namespace Mercader
{
    public partial class MainPage : ContentPage
    {
        static string filename = Path.Combine("mercader.db3");
        SQLiteConnection? conn;

        public Balance balance;



        public MainPage()
        {
            InitializeComponent();
            balance = new Balance();


            ActualizarEtiquetaVentas();
        }

        public void ActualizarEtiquetaVentas()
        {
            try
            {
                decimal ventas = balance.CalcularVentas();
                VentasLabel.Text = ventas.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al calcular ventas: {ex.Message}");
            }
        }

        private async void InAgregarEncargo(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new EncModal());

        }

        private async void InAgregarVenta(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new VentaModal(this));
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
