using System.Collections.Generic;
using System.Linq;
using Microcharts;
using SkiaSharp;

public class ChartService : IChartService
{

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
                    : "#ff6b35";    // normal

            return new ChartEntry((float)d.Total)
            {
                Label = d.Periodo,
                ValueLabel = d.Total.ToString("N0"),
                Color = SKColor.Parse(color),
                TextColor = SKColor.Parse("#B0B0B0"),
                ValueLabelColor = SKColor.Parse("#FFFFFF")
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
                ValueLabel = d.Total.ToString("N0"),
                Color = SKColor.Parse(color),
                TextColor = SKColor.Parse("#B0B0B0"),
                ValueLabelColor = SKColor.Parse("#FFFFFF")
            };
        }).ToArray();
        return CrearLineChartBase(entries);
    }

    public Chart CrearGraficoGastos(List<(string Periodo, decimal Total)> datos)
    {
        if (datos == null || datos.Count == 0)
            return new LineChart { Entries = new List<ChartEntry>() };

        var entries = datos.Select(d => new ChartEntry((float)d.Total)
        {
            Label = d.Periodo,
            ValueLabel = d.Total.ToString("N0"),
            Color = SKColor.Parse("#dc3545"),
            TextColor = SKColor.Parse("#E0E0E0"),
            ValueLabelColor = SKColor.Parse("#FFFFFF")
        }).ToArray();

        return CrearLineChartBase(entries);
    }

    public Chart CrearGraficoGanancias(
    List<(string Periodo, decimal Total)> ventas,
    List<(string Periodo, decimal Total)> gastos)
    {
        if (ventas == null || ventas.Count == 0 || gastos == null)
            return new LineChart { Entries = new List<ChartEntry>() };

        var datosCompletos = ventas.Select(v =>
        {
            var gastoCorrespondiente = gastos.FirstOrDefault(g => g.Periodo == v.Periodo);

            decimal gasto = gastoCorrespondiente.Total;
            decimal ganancia = v.Total - gasto;

            return (Periodo: v.Periodo, Total: ganancia);
        }).ToList();

        if (datosCompletos.Count == 0)
            return new LineChart { Entries = new List<ChartEntry>() };

        var maxTotal = datosCompletos.Max(d => d.Total);
        var minTotal = datosCompletos.Min(d => d.Total);

        var entries = datosCompletos.Select(d =>
        {
            var color = d.Total == maxTotal
                ? "#4CAF50"
                : d.Total == minTotal
                    ? "#FFC107"
                    : "#1f6bc2";

            return new ChartEntry((float)d.Total)
            {
                Label = d.Periodo,
                ValueLabel = d.Total.ToString("0"),
                Color = SKColor.Parse(color),
                TextColor = SKColors.White,
                ValueLabelColor = SKColors.White
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
            LabelTextSize = 22,
            ValueLabelTextSize = 24,

            // 🎨 Estética
            BackgroundColor = SKColor.Parse("#2a2a2a"),
            LineSize = 5,
            PointSize = 10,
            IsAnimated = true,
            LineMode = LineMode.Straight,
            AnimationDuration = TimeSpan.FromMilliseconds(800),

            // 📐 Orientación
            LabelOrientation = Orientation.Horizontal,
            ValueLabelOrientation = Orientation.Horizontal,

            // 📊 Ejes
            ShowYAxisLines = true,
            YAxisLinesPaint = new SKPaint
            {
                Color = SKColor.Parse("#404040"),
                StrokeWidth = 1,
                IsAntialias = true
            },

            // 📦 Margen
            Margin = 25,

            // 💡 Extras
            EnableYFadeOutGradient = true

        };
    }
}
