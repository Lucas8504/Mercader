using OfficeOpenXml;

namespace Mercader
{
    public partial class MainPage : ContentPage
    {
        

        public MainPage()
        {
            InitializeComponent();
        }


        private void OnAgregarEncargoClicked(object sender, EventArgs e)
        {
            var encargo = new Encargo
            {
                Nombre = EncargoEntry.Text,
                Precio = decimal.Parse(PrecioEntry.Text)
            };
            // Aquí puedes agregar el encargo a una lista si es necesario
        }

        private void OnAgregarVentaClicked(object sender, EventArgs e, Balance balance)
        {
            var venta = new Ventas
            {
                Encargo = new Encargo { Nombre = EncargoEntry.Text, Precio = decimal.Parse(PrecioEntry.Text) },
                Cantidad = int.Parse(CantidadEntry.Text),
                Fecha = DateTime.Now
            };
            balance.Ventas.Add(venta);
        }

        private void OnAgregarGastoClicked(object sender, EventArgs e, Balance balance)
        {
            var gasto = new Gasto
            {
                Descripcion = DescripcionGastoEntry.Text,
                Monto = decimal.Parse(MontoGastoEntry.Text),
                Fecha = DateTime.Now
            };
            balance.Gastos.Add(gasto);
        }

        private void OnCalcularGananciasClicked(object sender, EventArgs e, Balance balance)
        {
            var ganancias = balance.CalcularGanancias();
            GananciasLabel.Text = $"Ganancias: {ganancias:C}";
        }
        private void OnExportarAExcelClicked(object sender, EventArgs e)
        {
            string rutaArchivo = Path.Combine(FileSystem.AppDataDirectory, "balance.xlsx");
            ExportarBalanceAExcel(balance, rutaArchivo);
            DisplayAlert("Exportación Completa", $"Archivo exportado a {rutaArchivo}", "OK");
        }

        public void ExportarBalanceAExcel(Balance balance, string rutaArchivo)
        {
            using (ExcelPackage package = new ExcelPackage())
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Balance");

                worksheet.Cells[1, 1].Value = "Encargo";
                worksheet.Cells[1, 2].Value = "Precio";
                worksheet.Cells[1, 3].Value = "Cantidad";
                worksheet.Cells[1, 4].Value = "Fecha";

                int row = 2;
                foreach (var venta in balance.Ventas)
                {
                    worksheet.Cells[row, 1].Value = venta.Encargo.Nombre;
                    worksheet.Cells[row, 2].Value = venta.Encargo.Precio;
                    worksheet.Cells[row, 3].Value = venta.Cantidad;
                    worksheet.Cells[row, 4].Value = venta.Fecha.ToString("dd/MM/yyyy");
                    row++;
                }

                worksheet.Cells[row + 1, 1].Value = "Descripción";
                worksheet.Cells[row + 1, 2].Value = "Monto";
                worksheet.Cells[row + 1, 3].Value = "Fecha";

                row += 2;
                foreach (var gasto in balance.Gastos)
                {
                    worksheet.Cells[row, 1].Value = gasto.Descripcion;
                    worksheet.Cells[row, 2].Value = gasto.Monto;
                    worksheet.Cells[row, 3].Value = gasto.Fecha.ToString("dd/MM/yyyy");
                    row++;
                }

                worksheet.Cells[row + 1, 1].Value = "Ganancias";
                worksheet.Cells[row + 1, 2].Value = balance.CalcularGanancias();

                FileInfo fileInfo = new FileInfo(rutaArchivo);
                package.SaveAs(fileInfo);
            }
        }

    }

}
