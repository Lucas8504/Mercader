using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mercader
{
     public static class ExportExcel
    {

     


        public static void ExportarBalanceAExcel(Balance balance, string rutaArchivo)
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
