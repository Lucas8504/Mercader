using SQLite;

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

            ActualizarEtiquetaGastos();
            ActualizarEtiquetaEncargos();
            ActualizarEtiquetaVentas();
        }

        // Método público para actualizar la etiqueta
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
     
        public void ActualizarEtiquetaGastos()
        {
            try
            {
                decimal gastos = balance.CalcularGastos();
                GastosLabel.Text = gastos.ToString();
                Console.WriteLine($"Total de gastos calculado: {gastos}"); // Para debug
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al calcular gastos: {ex.Message}");
            }
        }
        public void ActualizarEtiquetaEncargos()
        {
            try
            {
                decimal encargo = balance.CalcularEncargos();
                EncargosLabel.Text = encargo.ToString();
                Console.WriteLine($"Total de encargos calculado: {encargo}"); // Para debug
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al calcular encargos: {ex.Message}");
            }
        }

        private async void InAgregarEncargo(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new EncModal(this));

        }

        private async void InAgregarVenta(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new VentaModal(this));
        }


        private async void InAgregarGasto(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new GastoModal(this));

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
