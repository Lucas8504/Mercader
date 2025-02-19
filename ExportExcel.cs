using OfficeOpenXml;


namespace Mercader
{
    public static class ExportExcel
    {
        public static void ExportarBalanceAExcel(Balance balance, string rutaArchivo)
        {
            using (ExcelPackage package = new ExcelPackage())
            {
                // Hoja de Encargos
                ExcelWorksheet worksheetEncargos = package.Workbook.Worksheets.Add("Encargos");
                worksheetEncargos.Cells[1, 1].Value = "Nombre";
                worksheetEncargos.Cells[1, 2].Value = "Descripción";
                worksheetEncargos.Cells[1, 3].Value = "Precio";
                worksheetEncargos.Cells[1, 4].Value = "Cantidad";
                worksheetEncargos.Cells[1, 5].Value = "Fecha";
                worksheetEncargos.Cells[1, 6].Value = "FechaEntrega";

                int row = 2;
                foreach (var encargo in balance.Encargos)
                {
                    worksheetEncargos.Cells[row, 1].Value = encargo.Nombre;
                    worksheetEncargos.Cells[row, 2].Value = encargo.Descripcion;
                    worksheetEncargos.Cells[row, 3].Value = encargo.Precio;
                    worksheetEncargos.Cells[row, 4].Value = encargo.Cantidad;
                    worksheetEncargos.Cells[row, 5].Value = encargo.Fecha.ToString("dd/MM/yyyy");
                    worksheetEncargos.Cells[row, 6].Value = encargo.FechaEntrega.ToString("dd/MM/yyyy");
                    row++;
                }

                // Hoja de Ventas
                ExcelWorksheet worksheetVentas = package.Workbook.Worksheets.Add("Ventas");
                worksheetVentas.Cells[1, 1].Value = "Descripción";
                worksheetVentas.Cells[1, 2].Value = "Precio";
                worksheetVentas.Cells[1, 3].Value = "Cantidad";
                worksheetVentas.Cells[1, 4].Value = "Fecha";

                row = 2;
                foreach (var venta in balance.Ventas)
                {
                    worksheetVentas.Cells[row, 1].Value = venta.Descripcion;
                    worksheetVentas.Cells[row, 2].Value = venta.Precio;
                    worksheetVentas.Cells[row, 3].Value = venta.Cantidad;
                    worksheetVentas.Cells[row, 4].Value = venta.Fecha.ToString("dd/MM/yyyy");
                    row++;
                }

                // Hoja de Gastos
                ExcelWorksheet worksheetGastos = package.Workbook.Worksheets.Add("Gastos");
                worksheetGastos.Cells[1, 1].Value = "Descripción";
                worksheetGastos.Cells[1, 2].Value = "Monto";
                worksheetGastos.Cells[1, 3].Value = "Cantidad";
                worksheetGastos.Cells[1, 4].Value = "Fecha";

                row = 2;
                foreach (var gasto in balance.Gastos)
                {
                    worksheetGastos.Cells[row, 1].Value = gasto.Descripcion;
                    worksheetGastos.Cells[row, 2].Value = gasto.Monto;
                    worksheetGastos.Cells[row, 3].Value = gasto.Cantidad;
                    worksheetGastos.Cells[row, 4].Value = gasto.Fecha.ToString("dd/MM/yyyy");
                    row++;
                }

                // Guardar el archivo
                FileInfo fileInfo = new FileInfo(rutaArchivo);
                package.SaveAs(fileInfo);
            }
        }
    }
}
