using Microcharts;
using SkiaSharp;

public class ChartService : IChartService
{
    public Chart CrearGraficoVentas(List<(string Periodo, decimal Total)> datos)
    {
        var max = datos.Max(d => d.Total);
        var min = datos.Min(d => d.Total);


        var entries = datos.Select(d =>
        {
            var color = d.Total == max
                ? "#06B025"
                : d.Total == min
                    ? "#0F420C"
                    : "#2e9449";

            return new ChartEntry((float)d.Total)
            {
                Label = d.Periodo,
                ValueLabel = d.Total.ToString("N0"),
                Color = SKColor.Parse(color),
                TextColor = SKColor.Parse("#B0B0B0"),
                ValueLabelColor = SKColor.Parse("#FFFFFF")
            };
        });
        return new LineChart
        {
            Entries = entries.ToList(),
            BackgroundColor = SKColor.Parse("#2a2a2a"),
            LineSize = 5,
            PointSize = 10,
            LabelTextSize = 22,
            ValueLabelTextSize = 24,
            Margin = 25,
            IsAnimated = true
        };
    }

    public Chart CrearGraficoGastos(List<(string Periodo, decimal Total)> datos)
    {
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
        var datos = ventas.Select(v =>
        {
            var gasto = gastos.FirstOrDefault(g => g.Periodo == v.Periodo);
            return new
            {
                v.Periodo,
                Ganancia = v.Total - gasto.Total
            };
        }).ToList();

        var max = datos.Max(d => d.Ganancia);
        var min = datos.Min(d => d.Ganancia);

        var entries = datos.Select(d =>
        {
            var color =
                d.Ganancia == max ? "#4CAF50" :
                d.Ganancia == min ? "#FFC107" :
                                    "#1f6bc2";

            return new ChartEntry((float)d.Ganancia)
            {
                Label = d.Periodo,
                ValueLabel = d.Ganancia.ToString("N0"),
                Color = d.Ganancia >= 0
                    ? SKColor.Parse(color)
                    : SKColor.Parse("#dc3545"),
                TextColor = SKColor.Parse("#E0E0E0"),
                ValueLabelColor = SKColor.Parse("#FFFFFF")
            };
        }).ToArray();

        return CrearLineChartBase(entries);
    }

    private LineChart CrearLineChartBase(ChartEntry[] entries)
    {
        return new LineChart
        {
            Entries = entries,
            LabelTextSize = 24,
            ValueLabelTextSize = 26,
            BackgroundColor = SKColor.Parse("#2a2a2a"),
            LineSize = 4,
            PointSize = 8,
            IsAnimated = true,
            AnimationDuration = TimeSpan.FromMilliseconds(600),
            LabelOrientation = Orientation.Horizontal,
            ValueLabelOrientation = Orientation.Horizontal,
            Margin = 20,
            ShowYAxisLines = true,
            YAxisLinesPaint = new SKPaint
            {
                Color = SKColor.Parse("#3C3C3C"),
                StrokeWidth = 1
            }
        };
    }
}
