using System.Globalization;
using Mercader.Core.Services.Localization;
using Mercader.Domain.Entities;
using Mercader.Services.Calculators;

namespace Mercader.Tests;

/// <summary>
/// Tests del aislamiento entre la etiqueta de período (texto de UI, traducida) y la
/// clave de período (identificador de lógica, estable).
///
/// Contexto: <c>BalanceCalculatorService</c> filtraba y agrupaba comparando el
/// período contra textos en español ("Días", "Semanas", "Meses", "Años"). Como la UI
/// ahora entrega la etiqueta traducida ("Days", "Months", "Месяцы"), el switch caía
/// al caso por defecto y el balance se calculaba sobre el rango equivocado. Estos
/// tests fijan que la lógica depende de la clave, no del idioma visible.
///
/// Nota: <c>Loc</c> es estático y muta la cultura del hilo, por eso la clase restaura
/// <see cref="CultureInfo.CurrentCulture"/> al terminar. Comparte la colección
/// "Localization" con <see cref="LocTests"/> para no ejecutarse en paralelo.
/// </summary>
[Collection("Localization")]
public class BalanceCalculatorPeriodoTests : IDisposable
{
    private const string IdiomaTest = "zz-test";

    private readonly BalanceCalculatorService _calculator = new();
    private readonly CultureInfo _previousCulture = CultureInfo.CurrentCulture;
    private readonly CultureInfo _previousUiCulture = CultureInfo.CurrentUICulture;

    public BalanceCalculatorPeriodoTests()
    {
        // Catálogo propio para no pisar los reales: sólo las 4 etiquetas de período.
        Loc.RegisterCatalog(IdiomaTest, new Dictionary<string, string>
        {
            ["Dias"] = "Days",
            ["Semanas"] = "Weeks",
            ["Meses"] = "Months",
            ["Anios"] = "Years"
        });
        Loc.SetLanguage(IdiomaTest);
    }

    public void Dispose()
    {
        Loc.SetLanguage(Loc.DefaultLanguage);
        CultureInfo.CurrentCulture = _previousCulture;
        CultureInfo.CurrentUICulture = _previousUiCulture;
    }

    private static List<Ventas> VentasDePrueba() => new()
    {
        new Ventas { Fecha = DateTime.Today.AddDays(-10), Precio = 100, Cantidad = 1 },
        new Ventas { Fecha = DateTime.Today.AddDays(-800), Precio = 50, Cantidad = 1 }
    };

    // ======================================================================
    // Claves canónicas: la lógica no depende del idioma
    // ======================================================================

    [Fact]
    public void AgruparVentasPorPeriodo_ClaveCanonicaDias_GeneraUnCuboPorDia()
    {
        var resultado = _calculator.AgruparVentasPorPeriodo(VentasDePrueba(), "Dias");

        Assert.Equal(133, resultado.Count);
    }

    [Fact]
    public void AgruparVentasPorPeriodo_ClaveCanonicaAnios_GeneraUnCuboPorAnio()
    {
        var resultado = _calculator.AgruparVentasPorPeriodo(VentasDePrueba(), "Anios");

        Assert.Equal(10, resultado.Count);
    }

    [Fact]
    public void AgruparVentasPorPeriodo_ClaveCanonicaSemanas_EtiquetaEnFormatoDiaMes()
    {
        var resultado = _calculator.AgruparVentasPorPeriodo(VentasDePrueba(), "Semanas");

        Assert.Equal(130, resultado.Count);
        // Las semanas se etiquetan dd/MM; los meses usan el nombre abreviado ("MMM").
        Assert.Contains('/', resultado[0].Periodo);
    }

    [Fact]
    public void AgruparVentasPorPeriodo_ClaveCanonicaMeses_EtiquetaEsNombreDeMes()
    {
        var resultado = _calculator.AgruparVentasPorPeriodo(VentasDePrueba(), "Meses");

        Assert.Equal(130, resultado.Count);
        Assert.DoesNotContain('/', resultado[0].Periodo);
    }

    // ======================================================================
    // Etiqueta traducida: la UI entrega el texto visible, no la clave
    // ======================================================================

    [Fact]
    public void AgruparVentasPorPeriodo_EtiquetaTraducida_Dias_GeneraUnCuboPorDia()
    {
        var resultado = _calculator.AgruparVentasPorPeriodo(VentasDePrueba(), "Days");

        Assert.Equal(133, resultado.Count);
    }

    [Fact]
    public void AgruparVentasPorPeriodo_EtiquetaTraducida_Anios_GeneraUnCuboPorAnio()
    {
        var resultado = _calculator.AgruparVentasPorPeriodo(VentasDePrueba(), "Years");

        Assert.Equal(10, resultado.Count);
    }

    // ======================================================================
    // Etiqueta legacy en español con tilde: sigue funcionando
    // ======================================================================

    [Fact]
    public void AgruparVentasPorPeriodo_EtiquetaLegacyConTilde_Dias_GeneraUnCuboPorDia()
    {
        var resultado = _calculator.AgruparVentasPorPeriodo(VentasDePrueba(), "Días");

        Assert.Equal(133, resultado.Count);
    }

    [Fact]
    public void AgruparVentasPorPeriodo_EtiquetaLegacyConTilde_Anios_GeneraUnCuboPorAnio()
    {
        var resultado = _calculator.AgruparVentasPorPeriodo(VentasDePrueba(), "Años");

        Assert.Equal(10, resultado.Count);
    }
}
