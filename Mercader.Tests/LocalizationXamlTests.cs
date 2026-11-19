using System.Text.Json;
using System.Text.RegularExpressions;

namespace Mercader.Tests;

/// <summary>
/// Guardas de la localización en el XAML y en el código.
///
/// Estas tests existen porque dos bugs reales escaping del proceso de revisión visual:
/// 1. El Shell se parseaba antes de que se cargaran los catálogos → los títulos del flyout
///    se veían como claves crudas ("NavInicio") en lugar de la traducción.
/// 2. Las markup extensions embebidas dentro de un string con texto literal
///    (Text="💵 {loc:Translate Ventas}") NO las evalúa el compilador XAML → se veían
///    literalmente en pantalla.
///
/// Ambas clases de bug son invisibles para el compilador de C#, así que se verifican
/// escaneando los archivos fuente.
/// </summary>
public class LocalizationXamlTests
{
    private static readonly string[] CatalogFiles =
    {
        "es.json", "en.json", "pt-BR.json", "zh-TW.json", "ru.json"
    };

    // Valor de atributo completo: ="...."
    private static readonly Regex AttributeValueRegex = new("=\"([^\"]*)\"", RegexOptions.Compiled);

    // Markup extension usada como valor completo del atributo (la forma que SÍ funciona)
    private static readonly Regex StandaloneTranslateRegex =
        new(@"^\{loc:Translate\s+([A-Za-z0-9_]+)\}$", RegexOptions.Compiled);

    // Cualquier uso de la markup extension de traducción
    private static readonly Regex AnyTranslateRegex =
        new(@"\{loc:Translate\s+([A-Za-z0-9_]+)\}", RegexOptions.Compiled);

    private static readonly Regex CodeKeyRegex =
        new(@"Loc\.(?:Get|Format)\(""([^""]+)""", RegexOptions.Compiled);

    private static readonly string RepoRoot = FindRepoRoot();

    // ======================================================================
    // Catálogos
    // ======================================================================

    [Fact]
    public void TodosLosCatalogos_Existen()
    {
        foreach (var file in CatalogFiles)
        {
            Assert.True(File.Exists(Path.Combine(RepoRoot, "Locales", file)),
                $"Falta el catálogo Locales/{file}");
        }
    }

    [Fact]
    public void TodosLosCatalogos_TienenLasMismasClaves()
    {
        var reference = LoadCatalog("es.json");
        var referenceKeys = reference.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();

        foreach (var file in CatalogFiles.Where(f => f != "es.json"))
        {
            var keys = LoadCatalog(file).Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();

            var faltantes = referenceKeys.Except(keys, StringComparer.Ordinal).ToList();
            var sobrantes = keys.Except(referenceKeys, StringComparer.Ordinal).ToList();

            Assert.True(faltantes.Count == 0,
                $"Locales/{file} no tiene estas claves: {string.Join(", ", faltantes)}");
            Assert.True(sobrantes.Count == 0,
                $"Locales/{file} tiene claves que es.json no tiene: {string.Join(", ", sobrantes)}");
        }
    }

    [Fact]
    public void LasClavesDeCatalogo_SonAscii()
    {
        // Una clave con acento depende del parseo de identificadores no-ASCII del XAML.
        foreach (var file in CatalogFiles)
        {
            foreach (var key in LoadCatalog(file).Keys)
            {
                Assert.True(key.All(c => c < 128),
                    $"La clave '{key}' de Locales/{file} contiene caracteres no-ASCII");
            }
        }
    }

    [Fact]
    public void NingunValorDeCatalogo_EstaVacio()
    {
        foreach (var file in CatalogFiles)
        {
            foreach (var (key, value) in LoadCatalog(file))
            {
                Assert.False(string.IsNullOrWhiteSpace(value),
                    $"Locales/{file}: la clave '{key}' tiene un valor vacío");
            }
        }
    }

    // ======================================================================
    // XAML
    // ======================================================================

    [Fact]
    public void NingunaMarkupExtensionEstaEmbebidaEnUnString()
    {
        // XamlC no compila markup extensions custom embebidas en un string con texto literal:
        // el valor queda tal cual en pantalla. Solo funciona como valor completo del atributo.
        var offenders = new List<string>();

        foreach (var file in XamlFiles())
        {
            var content = File.ReadAllText(file);

            foreach (Match match in AttributeValueRegex.Matches(content))
            {
                var value = match.Groups[1].Value;

                if (!value.Contains("{loc:", StringComparison.Ordinal))
                    continue;

                if (StandaloneTranslateRegex.IsMatch(value))
                    continue;

                offenders.Add($"{Relative(file)}: =\"{value}\"");
            }
        }

        Assert.True(offenders.Count == 0,
            "Markup extensions de traducción embebidas en un string (no se evalúan en runtime):\n" +
            string.Join("\n", offenders));
    }

    [Fact]
    public void TodasLasClavesUsadasEnXaml_ExistenEnTodosLosCatalogos()
    {
        var offenders = new List<string>();

        foreach (var file in XamlFiles())
        {
            var content = File.ReadAllText(file);

            foreach (Match match in AnyTranslateRegex.Matches(content))
            {
                var key = match.Groups[1].Value;

                foreach (var catalog in CatalogFiles)
                {
                    if (!LoadCatalog(catalog).ContainsKey(key))
                        offenders.Add($"{Relative(file)}: clave '{key}' ausente en Locales/{catalog}");
                }
            }
        }

        Assert.True(offenders.Count == 0, string.Join("\n", offenders));
    }

    [Fact]
    public void LasPaginasTraducidas_DeclaranElNamespaceLoc()
    {
        // Si falta xmlns:loc, la markup extension no compila; pero si el namespace se declara
        // y no se usa, es código muerto. Este test obliga a que cada uso tenga su namespace.
        var offenders = new List<string>();

        foreach (var file in XamlFiles())
        {
            var content = File.ReadAllText(file);
            var usesTranslate = AnyTranslateRegex.IsMatch(content);
            var declaresNamespace = content.Contains("xmlns:loc=", StringComparison.Ordinal);

            if (usesTranslate && !declaresNamespace)
                offenders.Add($"{Relative(file)}: usa {{loc:Translate}} sin xmlns:loc");
        }

        Assert.True(offenders.Count == 0, string.Join("\n", offenders));
    }

    // ======================================================================
    // Código C#
    // ======================================================================

    [Fact]
    public void TodasLasClavesUsadasEnCodigo_ExistenEnTodosLosCatalogos()
    {
        var offenders = new List<string>();

        foreach (var file in CsFiles())
        {
            var content = File.ReadAllText(file);

            foreach (Match match in CodeKeyRegex.Matches(content))
            {
                var key = match.Groups[1].Value;

                foreach (var catalog in CatalogFiles)
                {
                    if (!LoadCatalog(catalog).ContainsKey(key))
                        offenders.Add($"{Relative(file)}: clave '{key}' ausente en Locales/{catalog}");
                }
            }
        }

        Assert.True(offenders.Count == 0, string.Join("\n", offenders));
    }

    // ======================================================================
    // Helpers
    // ======================================================================

    private static Dictionary<string, string> LoadCatalog(string fileName)
    {
        var path = Path.Combine(RepoRoot, "Locales", fileName);
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
               ?? new Dictionary<string, string>();
    }

    private static IEnumerable<string> XamlFiles() =>
        SourceFiles("*.xaml");

    private static IEnumerable<string> CsFiles() =>
        SourceFiles("*.cs");

    private static IEnumerable<string> SourceFiles(string pattern)
    {
        return Directory.EnumerateFiles(RepoRoot, pattern, SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                        && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !path.Contains($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}")
                        // El proyecto de tests usa claves sintéticas (Test.*) contra catálogos
                        // de prueba: no debe contrastarse contra los catálogos de la app.
                        && !path.Contains($"{Path.DirectorySeparatorChar}Mercader.Tests{Path.DirectorySeparatorChar}"));
    }

    private static string Relative(string path) =>
        Path.GetRelativePath(RepoRoot, path).Replace('\\', '/');

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Mercader.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "No se encontró Mercader.sln subiendo desde " + AppContext.BaseDirectory);
    }
}
