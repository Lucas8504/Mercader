using SQLite;
using Microsoft.Maui.Controls;
using System.Globalization;

namespace Mercader
{
    public partial class MainPage : ContentPage
    {
        public Balance balance;

        public MainPage()
        {
            InitializeComponent();
            balance = new Balance();

            
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Console.WriteLine("OnAppearing ejecutado");

            // Cargar datos de la base de datos cada vez que la página aparece
            CargarDatosAsync().ConfigureAwait(false);
        }

        private async Task CargarDatosAsync()
        {
            try
            {
                var encargos = await App.DataRepo.GetEncargosAsync();
                var gastos = await App.DataRepo.GetGastosAsync();
                var ventas = await App.DataRepo.GetVentasAsync();

                balance.Encargos = encargos;
                balance.Gastos = gastos;
                balance.Ventas = ventas;

                ActualizarEtiquetaEncargos();
                ActualizarEtiquetaGastos();
                ActualizarEtiquetaVentas();
                ActualizarEtiquetaGanancias();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar datos: {ex.Message}");
                await DisplayAlert("Error", $"Error al cargar datos: {ex.Message}", "OK");
            }
        }

        // Método público para actualizar la etiqueta de ganancias
        public void ActualizarEtiquetaGanancias()
        {
            try
            {
                var ganancias = balance.CalcularGanancias();
                Console.WriteLine($"Ganancias calculadas: {ganancias}");
                GananciasLabel.Text = $"Ganancias: {ganancias:C}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al actualizar etiqueta de ganancias: {ex.Message}");
            }
        }

        // Método público para actualizar la etiqueta de ventas
        public void ActualizarEtiquetaVentas()
        {
            try
            {
                decimal ventas = balance.CalcularVentas();
                Console.WriteLine($"Ventas calculadas: {ventas}");
                VentasLabel.Text = $"Ventas: {ventas:C}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al calcular ventas: {ex.Message}");
            }
        }

        // Método público para actualizar la etiqueta de gastos
        public void ActualizarEtiquetaGastos()
        {
            try
            {
                decimal gastos = balance.CalcularGastos();
                Console.WriteLine($"Gastos calculados: {gastos}");
                GastosLabel.Text = $"Gastos: {gastos:C}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al calcular gastos: {ex.Message}");
            }
        }

        // Método público para actualizar la etiqueta de encargos
        public void ActualizarEtiquetaEncargos()
        {
            try
            {
                decimal encargo = balance.CalcularEncargos();
                Console.WriteLine($"Encargos calculados: {encargo}");
                EncargosLabel.Text = $"Encargos: {encargo:C}";
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
            }
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
            try
            {
                var ganancias = balance.CalcularGanancias();
                GananciasLabel.Text = $"Ganancias: {ganancias:C}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al calcular ganancias: {ex.Message}");
            }
        }

        private void OnExportarAExcelClicked(object sender, EventArgs e)
        {
            try
            {
                string rutaArchivo = Path.Combine(FileSystem.AppDataDirectory, "balance.xlsx");
                ExportExcel.ExportarBalanceAExcel(balance, rutaArchivo);
                DisplayAlert("Exportación Completa", $"Archivo exportado a {rutaArchivo}", "OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al exportar a Excel: {ex.Message}");
            }
        }

        private async void OnVerEncargosClicked(object sender, EventArgs e)
        {
            var navigationParameter = new Dictionary<string, object>
                            {
                                { "MainPage", this }
                            };
            await Shell.Current.GoToAsync(nameof(Encargos), navigationParameter);
        }
    }
}
