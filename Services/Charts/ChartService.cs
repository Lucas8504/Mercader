using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microcharts;
using SkiaSharp;
using Mercader.Services.Interfaces;
using Microsoft.Maui.Storage;

namespace Mercader.Services.Charts
{
    public class ChartService : IChartService
    {
        private static bool EsModoClaro =>
            Application.Current?.UserAppTheme == AppTheme.Light ||
            Application.Current?.RequestedTheme == AppTheme.Light;

        private static SKColor ChartBg => EsModoClaro ? SKColor.Parse("#FFFFFF") : SKColor.Parse("#2a2a2a");
        private static SKColor ChartText => EsModoClaro ? SKColor.Parse("#1C1C1E") : SKColor.Parse("#E0E0E0");
        private static SKColor ChartValueLabel => EsModoClaro ? SKColor.Parse("#1C1C1E") : SKColor.Parse("#FFFFFF");
        private static SKColor ChartGridLine => EsModoClaro ? SKColor.Parse("#D1D1D6") : SKColor.Parse("#404040");

    public Chart CrearGraficoEncargos(List<(string Periodo, decimal Total)> datos)
    {
        if (datos == null || datos.Count == 0)
            return new LineChart { Entries = new List<ChartEntry>() };

        var maxTotal = datos.Max(d => d.Total);
        var minTotal = datos.Min(d => d.Total);


        var entries = datos.Select(d =>
        {
            var color = d.Total == maxTotal
                ? "#FF9016"         // pico
                : d.Total == minTotal
                    ? "#C84C0F"     // alerta
                    : "#FF4F0F";    // normal

            return new ChartEntry((float)d.Total)
            {
                Label = d.Periodo,
                ValueLabel = FormatearValorEntero(d.Total),
                Color = SKColor.Parse(color),
                TextColor = ChartText,
                ValueLabelColor = ChartValueLabel
            };
        }).ToArray();
        return CrearLineChartBase(entries);
    }

        public Chart CrearGraficoVentas(List<(string Periodo, decimal Total)> datos)
    {
        if (datos == null || datos.Count == 0)
            return new LineChart { Entries = new List<ChartEntry>() };

        var maxTotal = datos.Max(d => d.Total);
        var minTotal = datos.Min(d => d.Total);


        var entries = datos.Select(d =>
        {
            var color = d.Total == maxTotal
                ? "#06B025"         // pico
                : d.Total == minTotal
                    ? "#0F420C"     // alerta
                    : "#2e9449";    // normal

            return new ChartEntry((float)d.Total)
            {
                Label = d.Periodo,
                ValueLabel = FormatearValorEntero(d.Total),
                Color = SKColor.Parse(color),
                TextColor = ChartText,
                ValueLabelColor = ChartValueLabel
            };
        }).ToArray();
        return CrearLineChartBase(entries);
    }

    public Chart CrearGraficoGastos(List<(string Periodo, decimal Total)> datos)
    {
        if (datos == null || datos.Count == 0)
            return new LineChart { Entries = new List<ChartEntry>() };

        var maxTotal = datos.Max(d => d.Total);
        var minTotal = datos.Min(d => d.Total);

        var entries = datos.Select(d => 
        {
            var color = d.Total == maxTotal
               ? "#BE2740"         // pico
               : d.Total == minTotal
                   ? "#6E0022"     // alerta
                   : "#981F33";    // normal
            return new ChartEntry((float)d.Total)
            {
                Label = d.Periodo,
                ValueLabel = FormatearValorEntero(d.Total),
                Color = SKColor.Parse(color),
                TextColor = ChartText,
                ValueLabelColor = ChartValueLabel
            };
        }).ToArray();

        return CrearLineChartBase(entries);
    }

    public Chart CrearGraficoGanancias(
    List<(string Periodo, decimal Total)> ventas,
    List<(string Periodo, decimal Total)> gastos)
    {
        if (ventas == null || ventas.Count == 0 || gastos == null)
            return new LineChart { Entries = new List<ChartEntry>() };

        // Ambos listas se generan por AgruparPorPeriodo con el mismo
        // parámetro, por lo que tienen los mismos períodos en el mismo
        // orden. Matching por índice (en vez de por string Periodo) para
        // evitar colisiones: el formato de meses (ej. "ene") se repite
        // para varios años, lo que rompería un Dictionary/ToLookup.
        var datosCompletos = new List<(string Periodo, decimal Total)>();
        int count = Math.Min(ventas.Count, gastos.Count);
        for (int i = 0; i < count; i++)
        {
            decimal ganancia = ventas[i].Total - gastos[i].Total;
            datosCompletos.Add((ventas[i].Periodo, ganancia));
        }

        if (datosCompletos.Count == 0)
            return new LineChart { Entries = new List<ChartEntry>() };

        var maxTotal = datosCompletos.Max(d => d.Total);
        var minTotal = datosCompletos.Min(d => d.Total);

        var entries = datosCompletos.Select(d =>
        {
            var color = d.Total == maxTotal
                ? "#267CDF"
                : d.Total == minTotal
                    ? "#2249D3"
                    : "#1D69BE";

            return new ChartEntry((float)d.Total)
            {
                Label = d.Periodo,
                ValueLabel = FormatearValorEntero(d.Total),
                Color = SKColor.Parse(color),
                TextColor = ChartText,
                ValueLabelColor = ChartValueLabel
            };
        }).ToArray();

        return CrearLineChartBase(entries);
    }

private LineChart CrearLineChartBase(ChartEntry[] entries)
        {
            return new LineChart
            {
                Entries = entries.ToList(),

                // 🎯 Texto
                LabelTextSize = 26,
                ValueLabelTextSize = 32,
                ValueLabelOption = ValueLabelOption.TopOfElement,

                // 🎨 Estética — líneas curvas con área degradada
                BackgroundColor = ChartBg,
                LineSize = 4,
                PointSize = 8,
                IsAnimated = false,
                AnimationProgress = 1,
                LineMode = LineMode.Spline,
                LineAreaAlpha = 28,
                EnableYFadeOutGradient = true,

                // 📐 Orientación
                LabelOrientation = Orientation.Horizontal,
                ValueLabelOrientation = Orientation.Horizontal,

                // 📊 Ejes — grilla sutil
                ShowYAxisLines = true,
                YAxisLinesPaint = new SKPaint
                {
                    Color = ChartGridLine,
                    StrokeWidth = 1,
                    IsAntialias = true
                },

// 📦 Margen lateral del gráfico (40 = valor original)
            Margin = 40,
            };
    }

        private string FormatearValorEntero(decimal valor)
        {
            // Usar la preferencia de moneda del usuario (no la locale)
            var simbolo = Preferences.Get("moneda", "$");
            var culture = CultureInfo.InvariantCulture; // Separador decimal siempre "."

            string numero = Math.Abs(valor) switch
            {
                // "0.#" y no "0.0": evita el ".0" redundante ("$20K" en vez de "$20.0K").
                // Microcharts recorta el label a 3 caracteres si no entra en el slot del
                // punto (DrawHelper.DrawLabel), así que cada carácter cuenta.
                >= 1000000 => Math.Round(valor / 1000000, 1).ToString("0.#", culture) + "M",
                >= 1000 => Math.Round(valor / 1000, 1).ToString("0.#", culture) + "K",
                _ => Math.Round(valor).ToString("N0", culture)
            };

            // Símbolo siempre antes (formato estándar: $5.7K, $1.2M)
            return $"{simbolo}{numero}";
        }
}
}
