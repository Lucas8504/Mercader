


using SQLite;

namespace Mercader
{
    public partial class MainPage : ContentPage
    {
        static string filename = Path.Combine("");
        static SQLiteConnection conn;

        public Balance balance;

        public MainPage()
        {
            InitializeComponent();
            conn = new SQLiteConnection(filename);
            balance = new Balance(); // Inicializar la variable balance

        }


        private async void InAgregarEncargo(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new EncModal());

        }

        private async void InAgregarVenta(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new VentaModal());
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
