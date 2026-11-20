using Mercader.Core.Services.Localization;

namespace Mercader.Services.Calculators;

/// <summary>
/// Claves de período del selector de la pantalla principal.
///
/// Son identificadores de lógica, NO etiquetas de UI: la UI muestra la traducción
/// (<see cref="Loc.Get"/>) pero el balance siempre compara contra estas claves.
/// Así, cambiar el idioma no altera el rango filtrado ni el agrupamiento.
///
/// Sin esta separación, el selector entrega "Months" / "Месяцы" y el cálculo cae
/// en el caso por defecto, mostrando un balance calculado sobre el rango errado.
/// </summary>
public static class Periodos
{
    public const string Dias = "Dias";
    public const string Semanas = "Semanas";
    public const string Meses = "Meses";
    public const string Anios = "Anios";

    /// <summary>
    /// Normaliza cualquier etiqueta a la clave canónica de lógica. Acepta la clave
    /// canónica, el texto legacy en español con tilde ("Días", "Años") y la
    /// traducción del idioma activo. Una etiqueta desconocida cae en
    /// <see cref="Meses"/>, que es el rango por defecto histórico.
    /// </summary>
    public static string Normalizar(string etiqueta)
    {
        if (string.IsNullOrWhiteSpace(etiqueta))
            return Meses;

        switch (etiqueta)
        {
            case Dias:
            case Semanas:
            case Meses:
            case Anios:
                return etiqueta;
        }

        // Legacy: el selector usaba los textos en español con tilde.
        if (etiqueta == "Días")
            return Dias;
        if (etiqueta == "Años")
            return Anios;

        // Traducción del idioma activo (lo que la UI muestra y entrega).
        if (etiqueta == Loc.Get("Dias"))
            return Dias;
        if (etiqueta == Loc.Get("Semanas"))
            return Semanas;
        if (etiqueta == Loc.Get("Meses"))
            return Meses;
        if (etiqueta == Loc.Get("Anios"))
            return Anios;

        return Meses;
    }
}
