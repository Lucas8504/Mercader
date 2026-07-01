using System.Reflection;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Mercader.Domain.Entities;
using Mercader.Models;

namespace Mercader.Tests;

public class ExportExcelTests : IDisposable
{
    private readonly string _tempFile;

    public ExportExcelTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"test_balance_{Guid.NewGuid():N}.xlsx");
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile))
            File.Delete(_tempFile);
    }

    // ======================================================================
    // End-to-end: ExportarBalanceAExcelAsync
    // ======================================================================

    [Fact]
    public async Task ExportarBalance_GeneratesFile()
    {
        var data = CreateSampleData();

        await ExportExcel.ExportarBalanceAExcelAsync(data, _tempFile);

        Assert.True(File.Exists(_tempFile));
    }

    [Fact]
    public async Task ExportarBalance_FileIsValidXlsx()
    {
        var data = CreateSampleData();

        await ExportExcel.ExportarBalanceAExcelAsync(data, _tempFile);

        // Abrir el archivo generado como workbook para validar que es un .xlsx válido
        using var fs = new FileStream(_tempFile, FileMode.Open, FileAccess.Read);
        using var workbook = new XSSFWorkbook(fs);
        Assert.NotNull(workbook);
    }

    [Fact]
    public async Task ExportarBalance_HasExpectedSheets()
    {
        var data = CreateSampleData();

        await ExportExcel.ExportarBalanceAExcelAsync(data, _tempFile);

        using var fs = new FileStream(_tempFile, FileMode.Open, FileAccess.Read);
        using var workbook = new XSSFWorkbook(fs);

        var sheets = new List<string>();
        for (int i = 0; i < workbook.NumberOfSheets; i++)
            sheets.Add(workbook.GetSheetName(i));

        Assert.Contains("Ventas", sheets);
        Assert.Contains("Gastos", sheets);
        Assert.Contains("Encargos", sheets);
        Assert.Contains("Resumen Ejecutivo", sheets);
        Assert.Contains("Grafico Financiero", sheets);
    }

    [Fact]
    public async Task ExportarBalance_VentasSheet_HasData()
    {
        var data = CreateSampleData();

        await ExportExcel.ExportarBalanceAExcelAsync(data, _tempFile);

        using var fs = new FileStream(_tempFile, FileMode.Open, FileAccess.Read);
        using var workbook = new XSSFWorkbook(fs);

        var sheet = workbook.GetSheet("Ventas");
        Assert.NotNull(sheet);

        // Row 0: título, Row 4: headers, Row 5+: data
        var headerRow = sheet.GetRow(4);
        Assert.Equal("FECHA", headerRow.GetCell(0).StringCellValue);
        Assert.Equal("DESCRIPCION", headerRow.GetCell(1).StringCellValue);
        Assert.Equal("TOTAL", headerRow.GetCell(4).StringCellValue);
    }

    [Fact]
    public async Task ExportarBalance_GastosSheet_HasData()
    {
        var data = CreateSampleData();

        await ExportExcel.ExportarBalanceAExcelAsync(data, _tempFile);

        using var fs = new FileStream(_tempFile, FileMode.Open, FileAccess.Read);
        using var workbook = new XSSFWorkbook(fs);

        var sheet = workbook.GetSheet("Gastos");
        Assert.NotNull(sheet);

        var headerRow = sheet.GetRow(4);
        Assert.Equal("FECHA", headerRow.GetCell(0).StringCellValue);
    }

    [Fact]
    public async Task ExportarBalance_Resumen_HasMetrics()
    {
        var data = CreateSampleData(tipo: "con_ganancias");

        await ExportExcel.ExportarBalanceAExcelAsync(data, _tempFile);

        using var fs = new FileStream(_tempFile, FileMode.Open, FileAccess.Read);
        using var workbook = new XSSFWorkbook(fs);

        var sheet = workbook.GetSheet("Resumen Ejecutivo");
        Assert.NotNull(sheet);

        // Buscar "Ganancias Netas" en la hoja
        bool foundGanancias = false;
        for (int r = 0; r <= sheet.LastRowNum; r++)
        {
            var row = sheet.GetRow(r);
            if (row?.GetCell(0)?.StringCellValue == "Ganancias Netas")
            {
                foundGanancias = true;
                break;
            }
        }
        Assert.True(foundGanancias, "La hoja Resumen Ejecutivo debería contener 'Ganancias Netas'");
    }

    // ======================================================================
    // Edge cases
    // ======================================================================

    [Fact]
    public async Task ExportarBalance_EmptyCollections_DoesNotCrash()
    {
        var data = new BalanceExportDto
        {
            Ventas = [],
            Gastos = [],
            Encargos = [],
            TotalVentas = 0,
            TotalGastos = 0,
            TotalEncargos = 0,
            Ganancias = 0,
            Periodo = "Test"
        };

        await ExportExcel.ExportarBalanceAExcelAsync(data, _tempFile);

        Assert.True(File.Exists(_tempFile));
    }

    [Fact]
    public async Task ExportarBalance_SingleVenta_Works()
    {
        var data = CreateSampleData(tipo: "una_venta");

        await ExportExcel.ExportarBalanceAExcelAsync(data, _tempFile);

        using var fs = new FileStream(_tempFile, FileMode.Open, FileAccess.Read);
        using var workbook = new XSSFWorkbook(fs);

        var sheet = workbook.GetSheet("Ventas");
        Assert.NotNull(sheet);
    }

    // ======================================================================
    // SetCellValue via reflection (private method, key branching logic)
    // ======================================================================

    public static IEnumerable<object?[]> SetCellValue_TestData()
    {
        yield return ["hello", "hello"];              // string → string
        yield return [42, 42.0];                       // int → double
        yield return [3.14, 3.14];                     // double → double
        yield return [99.99m, 99.99];                  // decimal → double
        yield return [null, ""];                       // null → ""
        yield return ["text", "text"];                 // string
    }

    [Theory]
    [MemberData(nameof(SetCellValue_TestData))]
    public void SetCellValue_WritesCorrectType(object? input, object expectedRaw)
    {
        var method = typeof(ExportExcel).GetMethod("SetCellValue",
            BindingFlags.Static | BindingFlags.NonPublic,
            [typeof(ICell), typeof(object)]);

        Assert.NotNull(method);

        using var workbook = new XSSFWorkbook();
        var sheet = workbook.CreateSheet();
        var row = sheet.CreateRow(0);
        var cell = row.CreateCell(0);

        method.Invoke(null, [cell, input]);

        switch (expectedRaw)
        {
            case string s:
                Assert.Equal(s, cell.StringCellValue);
                break;
            case double d:
                Assert.Equal(d, cell.NumericCellValue);
                break;
        }
    }

    // ======================================================================
    // Sample data factory
    // ======================================================================

    private static BalanceExportDto CreateSampleData(string tipo = "completo")
    {
        var ventas = new List<Ventas>();
        var gastos = new List<Gasto>();

        if (tipo == "completo" || tipo == "con_ganancias")
        {
            ventas.Add(new Ventas
            {
                Fecha = new DateTime(2026, 6, 15),
                Descripcion = "Venta de prueba",
                Cantidad = 2,
                Precio = 150.00m
            });
            ventas.Add(new Ventas
            {
                Fecha = new DateTime(2026, 5, 10),
                Descripcion = "Otra venta",
                Cantidad = 1,
                Precio = 300.00m
            });

            gastos.Add(new Gasto
            {
                Fecha = new DateTime(2026, 6, 1),
                Descripcion = "Compra insumos",
                Cantidad = 1,
                Monto = 80.00m
            });
        }

        if (tipo == "con_ganancias")
        {
            // Agregar encargos
            var encargos = new List<Encargo>
            {
                new()
                {
                    Fecha = new DateTime(2026, 6, 20),
                    Nombre = "Cliente A",
                    Descripcion = "Encargo prueba",
                    Cantidad = 3,
                    Precio = 100.00m,
                    FechaEntrega = new DateTime(2026, 7, 15)
                }
            };

            return new BalanceExportDto
            {
                Ventas = ventas.AsReadOnly(),
                Gastos = gastos.AsReadOnly(),
                Encargos = encargos.AsReadOnly(),
                TotalVentas = 600.00m,
                TotalGastos = -80.00m,
                TotalEncargos = 300.00m,
                Ganancias = 520.00m,
                Periodo = "Junio 2026"
            };
        }

        if (tipo == "una_venta")
        {
            ventas = [new Ventas
            {
                Fecha = new DateTime(2026, 6, 15),
                Descripcion = "Venta única",
                Cantidad = 1,
                Precio = 100.00m
            }];
        }

        return new BalanceExportDto
        {
            Ventas = ventas.AsReadOnly(),
            Gastos = gastos.AsReadOnly(),
            Encargos = new List<Encargo>().AsReadOnly(),
            TotalVentas = ventas.Sum(v => v.Precio * v.Cantidad),
            TotalGastos = gastos.Sum(g => g.Monto * g.Cantidad),
            TotalEncargos = 0,
            Ganancias = ventas.Sum(v => v.Precio * v.Cantidad) - gastos.Sum(g => g.Monto * g.Cantidad),
            Periodo = "Test"
        };
    }
}
