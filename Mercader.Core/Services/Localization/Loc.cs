using System.Globalization;

namespace Mercader.Core.Services.Localization;

/// <summary>
/// Servicio centralizado de localización. Sin dependencias de plataforma para poder
/// usarse desde Mercader.Core (librería net8.0) y desde la app MAUI.
///
/// Catálogos: diccionarios clave -> traducción, uno por idioma.
/// Fallback: idioma activo -> español (default) -> propia clave.
/// </summary>
public static class Loc
{
    public const string DefaultLanguage = "es";

    private static readonly Dictionary<string, Dictionary<string, string>> Catalogs =
        new(StringComparer.OrdinalIgnoreCase);

    private static string _language = DefaultLanguage;

    /// <summary>Cultura activa. Determina formato de fechas, meses y moneda.</summary>
    public static CultureInfo Culture { get; private set; } = new("es-ES");

    /// <summary>Código del idioma activo (es, en, pt-BR, zh-TW, ru).</summary>
    public static string CurrentLanguage => _language;

    /// <summary>Idiomas disponibles, en el orden en que se muestran al usuario.</summary>
    public static IReadOnlyList<string> AvailableLanguages { get; private set; } = new[] { DefaultLanguage };

    /// <summary>Se dispara cuando cambia el idioma, para que la UI pueda refrescarse.</summary>
    public static event EventHandler? LanguageChanged;

    /// <summary>
    /// Registra un catálogo de traducciones. Sobrescribe si el idioma ya estaba cargado.
    /// </summary>
    public static void RegisterCatalog(string language, Dictionary<string, string> catalog)
    {
        ArgumentNullException.ThrowIfNull(language);
        ArgumentNullException.ThrowIfNull(catalog);

        Catalogs[language] = catalog;
        AvailableLanguages = Catalogs.Keys
            .OrderBy(code => code == DefaultLanguage ? 0 : 1)
            .ThenBy(code => code, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Cambia el idioma activo y la cultura. Si el idioma no está registrado,
    /// cae al español. Devuelve true si el cambio fue efectivo.
    /// </summary>
    public static bool SetLanguage(string language)
    {
        if (string.IsNullOrWhiteSpace(language))
            language = DefaultLanguage;

        var code = Catalogs.ContainsKey(language) ? language : DefaultLanguage;
        var culture = cultureFor(code);

        var changed = !string.Equals(code, _language, StringComparison.OrdinalIgnoreCase)
                      || !Equals(Culture, culture);

        _language = code;
        Culture = culture;

        // CultureInfo.CurrentCulture / CurrentUICulture afectan el formato de
        // fechas y números de todo el runtime (no solo de nuestro servicio).
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        if (changed)
            LanguageChanged?.Invoke(null, EventArgs.Empty);

        return changed;
    }

    /// <summary>
    /// Traduce una clave. Prioridad: idioma activo -> español -> la propia clave.
    /// Nunca devuelve null ni lanza.
    /// </summary>
    public static string Get(string key)
    {
        if (string.IsNullOrEmpty(key))
            return string.Empty;

        if (Catalogs.TryGetValue(_language, out var activo)
            && activo.TryGetValue(key, out var valorActivo)
            && !string.IsNullOrEmpty(valorActivo))
        {
            return valorActivo;
        }

        if (Catalogs.TryGetValue(DefaultLanguage, out var porDefecto)
            && porDefecto.TryGetValue(key, out var valorDefecto)
            && !string.IsNullOrEmpty(valorDefecto))
        {
            return valorDefecto;
        }

        return key;
    }

    /// <summary>Traduce una clave con formato compuesto (string.Format).</summary>
    public static string Format(string key, params object[] args)
    {
        var template = Get(key);
        try
        {
            return string.Format(Culture, template, args);
        }
        catch (FormatException)
        {
            return template;
        }
    }

    /// <summary>Moneda formateada según la cultura activa.</summary>
    public static string FormatCurrency(decimal value) => value.ToString("C", Culture);

    /// <summary>Fecha formateada según la cultura activa.</summary>
    public static string FormatDate(DateTime date) => date.ToString("d", Culture);

    private static CultureInfo cultureFor(string code) => code switch
    {
        "en" => new CultureInfo("en-US"),
        "pt-BR" => new CultureInfo("pt-BR"),
        "zh-TW" => new CultureInfo("zh-TW"),
        "ru" => new CultureInfo("ru-RU"),
        _ => new CultureInfo("es-ES"),
    };
}
