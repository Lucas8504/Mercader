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
            var encargoModal = new EncModal(this);
            await Navigation.PushModalAsync(encargoModal);
            var nuevoEncargo = encargoModal.Encargo;
            if (nuevoEncargo != null)
            {
                await App.DataRepo.SaveEncargoAsync(nuevoEncargo);
                balance.Encargos.Add(nuevoEncargo);
                ActualizarEtiquetaEncargos();
            };

        }

        private async void InAgregarVenta(object sender, EventArgs e)
        {
            var ventaModal = new VentaModal(this);
            await Navigation.PushModalAsync(ventaModal);
            var nuevaVenta = ventaModal.Venta;
            if (nuevaVenta != null)
            {
                await App.DataRepo.SaveVentasAsync(nuevaVenta);
                balance.Ventas.Add(nuevaVenta);
                ActualizarEtiquetaVentas();
            }
        }


        private async void InAgregarGasto(object sender, EventArgs e)
        {
 var gastoModal = new GastoModal(this);
    await Navigation.PushModalAsync(gastoModal);
    var nuevoGasto = gastoModal.Gasto;
    if (nuevoGasto != null)
    {
        await App.DataRepo.SaveGastoAsync(nuevoGasto);
        balance.Gastos.Add(nuevoGasto);
        ActualizarEtiquetaGastos();
    }
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
