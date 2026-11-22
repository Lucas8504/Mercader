using System.Reflection;
using System.Text.Json;
using Mercader.Core.Services.Localization;

namespace Mercader.Services.Localization;

/// <summary>
/// Carga los catálogos de traducciones desde los recursos embebidos (Locales/*.json)
/// y los registra en <see cref="Loc"/>.
/// </summary>
public static class Localizacion
{
    private const string PrefijoRecurso = "Mercader.Locales.";

    /// <summary>Idiomas soportados, en el orden en que se muestran al usuario.</summary>
    public static readonly (string Code, string Nombre)[] Idiomas =
    {
        ("es", "Español"),
        ("en", "English"),
        ("pt-BR", "Português (Brasil)"),
        ("zh-TW", "繁體中文"),
        ("ru", "Русский"),
    };

    private static bool _cargado;

    /// <summary>
    /// Carga los catálogos embebidos. Idempotente: solo hace trabajo la primera vez.
    /// </summary>
    public static void CargarCatalogos()
    {
        if (_cargado)
            return;

        var ensamblado = Assembly.GetExecutingAssembly();

        foreach (var (code, _) in Idiomas)
        {
            var nombreRecurso = $"{PrefijoRecurso}{code}.json";
            using var stream = ensamblado.GetManifestResourceStream(nombreRecurso);
            if (stream == null)
                continue;

            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();

            var catalogo = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (catalogo != null)
                Loc.RegisterCatalog(code, catalogo);
        }

        _cargado = true;
    }

    /// <summary>
    /// Carga los catálogos y aplica el idioma persistido (o español por defecto).
    /// También sincroniza el símbolo de moneda elegido por el usuario: Core no
    /// accede a Preferences, así que la app se la pasa a <see cref="Loc"/>.
    /// </summary>
    public static void Inicializar()
    {
        CargarCatalogos();

        var idioma = Preferences.Get("idioma", Loc.DefaultLanguage);
        Loc.SetLanguage(idioma);

        Loc.SimboloMoneda = Preferences.Get("moneda", "$");
    }
}
