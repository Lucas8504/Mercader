using System.Globalization;
using Mercader.Core.Services.Localization;

namespace Mercader.Tests;

/// <summary>
/// Tests del servicio de localización <see cref="Loc"/>.
/// Verifican S1 (idiomas soportados), S3 (cultura por idioma) y S6 (fallback seguro).
///
/// Nota: <c>Loc</c> es estático y muta la cultura del hilo, por eso la clase
/// restaura <see cref="CultureInfo.CurrentCulture"/> al terminar cada test.
/// Comparte la colección "Localization" con <c>BalanceCalculatorPeriodoTests</c>
/// para que las dos clases que mutan el estado estático no se ejecuten en paralelo.
/// </summary>
[Collection("Localization")]
public class LocTests : IDisposable
{
    private const string Es = "es";
    private const string En = "en";
    private const string PtBr = "pt-BR";
    private const string ZhTw = "zh-TW";
    private const string Ru = "ru";

    private readonly CultureInfo _previousCulture = CultureInfo.CurrentCulture;
    private readonly CultureInfo _previousUiCulture = CultureInfo.CurrentUICulture;

    public LocTests()
    {
        // Catálogos de prueba con claves propias para no pisar las reales.
        Loc.RegisterCatalog(Es, new Dictionary<string, string>
        {
            ["Test.Saludo"] = "Hola",
            ["Test.SoloEspanol"] = "Solo español",
            ["Test.Saldo"] = "Tenés {0} pesos",
            ["Test.Malo"] = "Valor {0} y {1}"
        });
        Loc.RegisterCatalog(En, new Dictionary<string, string>
        {
            ["Test.Saludo"] = "Hello"
        });
        Loc.RegisterCatalog(PtBr, new Dictionary<string, string>());
        Loc.RegisterCatalog(ZhTw, new Dictionary<string, string>());
        Loc.RegisterCatalog(Ru, new Dictionary<string, string>());

        Loc.SetLanguage(Es);
    }

    public void Dispose()
    {
        Loc.SetLanguage(Loc.DefaultLanguage);
        CultureInfo.CurrentCulture = _previousCulture;
        CultureInfo.CurrentUICulture = _previousUiCulture;
    }

    // ======================================================================
    // Get / fallback seguro (S6)
    // ======================================================================

    [Fact]
    public void Get_UnknownKey_ReturnsTheKeyItself()
    {
        Loc.SetLanguage(Es);

        Assert.Equal("Test.NoExiste", Loc.Get("Test.NoExiste"));
    }

    [Fact]
    public void Get_KeyMissingInActiveLanguage_FallsBackToSpanish()
    {
        Loc.SetLanguage(En);

        Assert.Equal("Solo español", Loc.Get("Test.SoloEspanol"));
    }

    [Fact]
    public void Get_KeyPresentInActiveLanguage_UsesActiveLanguage()
    {
        Loc.SetLanguage(En);

        Assert.Equal("Hello", Loc.Get("Test.Saludo"));
    }

    [Fact]
    public void Get_EmptyTranslationInActiveLanguage_FallsBackToSpanish()
    {
        Loc.RegisterCatalog(En, new Dictionary<string, string> { ["Test.Saludo"] = string.Empty });
        Loc.SetLanguage(En);

        Assert.Equal("Hola", Loc.Get("Test.Saludo"));
    }

    [Fact]
    public void Get_EmptyKey_ReturnsEmptyString()
    {
        Assert.Equal(string.Empty, Loc.Get(string.Empty));
    }

    [Fact]
    public void Get_NullKey_ReturnsEmptyString()
    {
        Assert.Equal(string.Empty, Loc.Get(null!));
    }

    // ======================================================================
    // SetLanguage (S1, S3)
    // ======================================================================

    [Fact]
    public void SetLanguage_UnknownLanguage_FallsBackToDefault()
    {
        Loc.SetLanguage("xx-YY");

        Assert.Equal(Loc.DefaultLanguage, Loc.CurrentLanguage);
    }

    [Fact]
    public void SetLanguage_EmptyLanguage_FallsBackToDefault()
    {
        Loc.SetLanguage("   ");

        Assert.Equal(Loc.DefaultLanguage, Loc.CurrentLanguage);
    }

    [Fact]
    public void SetLanguage_KnownLanguage_ChangesCurrentLanguage()
    {
        Loc.SetLanguage(En);

        Assert.Equal(En, Loc.CurrentLanguage);
    }

    [Theory]
    [InlineData(Es, "es-ES")]
    [InlineData(En, "en-US")]
    [InlineData(PtBr, "pt-BR")]
    [InlineData(ZhTw, "zh-TW")]
    [InlineData(Ru, "ru-RU")]
    public void SetLanguage_SupportedLanguage_UsesExpectedCulture(string language, string expectedCulture)
    {
        Loc.SetLanguage(language);

        Assert.Equal(expectedCulture, Loc.Culture.Name);
    }

    [Fact]
    public void SetLanguage_KnownLanguage_UpdatesCurrentCulture()
    {
        Loc.SetLanguage(En);

        Assert.Equal("en-US", CultureInfo.CurrentCulture.Name);
    }

    [Fact]
    public void SetLanguage_KnownLanguage_UpdatesCurrentUiCulture()
    {
        Loc.SetLanguage(En);

        Assert.Equal("en-US", CultureInfo.CurrentUICulture.Name);
    }

    [Fact]
    public void SetLanguage_SameLanguage_ReturnsFalse()
    {
        Loc.SetLanguage(En);

        Assert.False(Loc.SetLanguage(En));
    }

    [Fact]
    public void SetLanguage_DifferentLanguage_ReturnsTrue()
    {
        Loc.SetLanguage(Es);

        Assert.True(Loc.SetLanguage(En));
    }

    [Fact]
    public void SetLanguage_DifferentLanguage_RaisesLanguageChanged()
    {
        Loc.SetLanguage(Es);
        var raised = 0;
        void Handler(object? sender, EventArgs e) => raised++;

        Loc.LanguageChanged += Handler;
        try
        {
            Loc.SetLanguage(En);
        }
        finally
        {
            Loc.LanguageChanged -= Handler;
        }

        Assert.Equal(1, raised);
    }

    [Fact]
    public void SetLanguage_SameLanguage_DoesNotRaiseLanguageChanged()
    {
        Loc.SetLanguage(En);
        var raised = 0;
        void Handler(object? sender, EventArgs e) => raised++;

        Loc.LanguageChanged += Handler;
        try
        {
            Loc.SetLanguage(En);
        }
        finally
        {
            Loc.LanguageChanged -= Handler;
        }

        Assert.Equal(0, raised);
    }

    // ======================================================================
    // RegisterCatalog
    // ======================================================================

    [Fact]
    public void RegisterCatalog_AvailableLanguages_PutsDefaultFirst()
    {
        Assert.Equal(Loc.DefaultLanguage, Loc.AvailableLanguages[0]);
    }

    [Fact]
    public void RegisterCatalog_AvailableLanguages_ContainsAllSupportedLanguages()
    {
        foreach (var language in new[] { Es, En, PtBr, ZhTw, Ru })
        {
            Assert.Contains(language, Loc.AvailableLanguages);
        }
    }

    [Fact]
    public void RegisterCatalog_ReRegisteringSameLanguage_ReplacesCatalog()
    {
        Loc.RegisterCatalog(En, new Dictionary<string, string> { ["Test.Saludo"] = "Hi" });
        Loc.SetLanguage(En);

        Assert.Equal("Hi", Loc.Get("Test.Saludo"));
    }

    // ======================================================================
    // Format
    // ======================================================================

    [Fact]
    public void Format_WithArguments_SubstitutesPlaceholders()
    {
        Loc.SetLanguage(Es);

        Assert.Equal("Tenés 5 pesos", Loc.Format("Test.Saldo", 5));
    }

    [Fact]
    public void Format_MissingArgument_ReturnsTemplateUnchanged()
    {
        Loc.SetLanguage(Es);

        Assert.Equal("Valor {0} y {1}", Loc.Format("Test.Malo", 1));
    }

    [Fact]
    public void FormatCurrency_FollowsActiveCulture()
    {
        Loc.SetLanguage(En);
        Assert.Contains("1,234.50", Loc.FormatCurrency(1234.5m));

        Loc.SetLanguage(Es);
        Assert.Contains("1.234,50", Loc.FormatCurrency(1234.5m));
    }

    [Fact]
    public void FormatDate_FollowsActiveCulture()
    {
        var date = new DateTime(2026, 11, 18);

        Loc.SetLanguage(En);
        Assert.Equal("11/18/2026", Loc.FormatDate(date));

        Loc.SetLanguage(Es);
        Assert.Equal("18/11/2026", Loc.FormatDate(date));
    }
}
