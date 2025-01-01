using SQLite;

namespace Mercader
{
    public partial class MainPage : ContentPage
    {
        
        public Balance balance;

        public MainPage()
        {
            InitializeComponent();
            balance = new Balance();

            ActualizarEtiquetaGanancias();
            ActualizarEtiquetaGastos();
            ActualizarEtiquetaEncargos();
            ActualizarEtiquetaVentas();


        }

        // Método público para actualizar la etiqueta

        public void ActualizarEtiquetaGanancias()
        {
            var ganancias = balance.CalcularGanancias();
            GananciasLabel.Text = $"Ganancias: {ganancias:C}";

        }

        public void ActualizarEtiquetaVentas()
        {
            try
            {

                decimal ventas = balance.CalcularVentas();
                VentasLabel.Text = ventas.ToString();
                string save = VentasLabel.Text;


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
            var ventaModal = new VentaModal(this);
            await Navigation.PushModalAsync(ventaModal);
            // Suponiendo que VentaModal tiene una propiedad Venta que contiene la nueva venta
            var nuevaVenta = ventaModal.Venta;
            //if (nuevaVenta != null)
            //{
               // await App.DataRepo.SaveVentasAsync(nuevaVenta);
                
            //}
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
