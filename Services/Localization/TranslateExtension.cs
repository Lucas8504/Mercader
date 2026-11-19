using Microsoft.Maui.Controls.Xaml;
using Mercader.Core.Services.Localization;

namespace Mercader.Services.Localization;

/// <summary>
/// Markup extension para usar traducciones en XAML:
///     xmlns:loc="clr-namespace:Mercader.Services.Localization"
///     <Label Text="{loc:Translate Total}" />
///
/// Se resuelve en tiempo de parseo del XAML. Para que un cambio de idioma se refleje,
/// la pagina se recrea (ver Configuracion.OnIdiomaChanged).
/// </summary>
[ContentProperty(nameof(Key))]
public class TranslateExtension : IMarkupExtension<string>
{
    /// <summary>Clave del catálogo de traducciones.</summary>
    public string Key { get; set; } = string.Empty;

    public string ProvideValue(IServiceProvider serviceProvider) => Loc.Get(Key);

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
        => ProvideValue(serviceProvider);
}
