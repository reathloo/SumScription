using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace PayToCalc.App.Controls;

/// <summary>Papierband: Fläche, 2-px-Kante und beidseitige Lochreihe, rein vektoriell.</summary>
public sealed class TapePanel : Decorator
{
    /// <summary>Dicke der Papierkante.</summary>
    public const double EdgeThickness = 2d;

    /// <summary>Breite eines Lochs.</summary>
    public const double HoleWidth = 5d;

    /// <summary>Höhe eines Lochs.</summary>
    public const double HoleHeight = 7d;

    /// <summary>Eckenradius eines Lochs.</summary>
    public const double HoleRadius = 3d;

    /// <summary>Mittenabstand der Löcher.</summary>
    public const double HoleSpacing = 16d;

    /// <summary>Abstand der Lochreihe vom Papierrand.</summary>
    public const double HoleInset = 10d;

    /// <summary>Fläche des Papiers.</summary>
    public static readonly StyledProperty<IBrush?> PaperBrushProperty =
        AvaloniaProperty.Register<TapePanel, IBrush?>(nameof(PaperBrush));

    /// <summary>Farbe der 2-px-Kante.</summary>
    public static readonly StyledProperty<IBrush?> EdgeBrushProperty =
        AvaloniaProperty.Register<TapePanel, IBrush?>(nameof(EdgeBrush));

    /// <summary>Farbe der Lochreihe (Chassis-Farbe).</summary>
    public static readonly StyledProperty<IBrush?> HoleBrushProperty =
        AvaloniaProperty.Register<TapePanel, IBrush?>(nameof(HoleBrush));

    static TapePanel()
    {
        AffectsRender<TapePanel>(PaperBrushProperty, EdgeBrushProperty, HoleBrushProperty);
    }

    /// <summary>Fläche des Papiers.</summary>
    public IBrush? PaperBrush
    {
        get => GetValue(PaperBrushProperty);
        set => SetValue(PaperBrushProperty, value);
    }

    /// <summary>Farbe der Papierkante.</summary>
    public IBrush? EdgeBrush
    {
        get => GetValue(EdgeBrushProperty);
        set => SetValue(EdgeBrushProperty, value);
    }

    /// <summary>Farbe der Lochreihe.</summary>
    public IBrush? HoleBrush
    {
        get => GetValue(HoleBrushProperty);
        set => SetValue(HoleBrushProperty, value);
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var width = Bounds.Width;
        var height = Bounds.Height;
        if (width <= 0 || height <= 0)
        {
            return;
        }

        if (PaperBrush is { } paper)
        {
            context.FillRectangle(paper, new Rect(0, 0, width, height));
        }

        if (EdgeBrush is { } edge)
        {
            var inset = EdgeThickness / 2d;
            context.DrawRectangle(
                null,
                new Pen(edge, EdgeThickness),
                new Rect(inset, inset, width - EdgeThickness, height - EdgeThickness));
        }

        if (HoleBrush is { } hole)
        {
            DrawPerforation(context, hole, width, height);
        }
    }

    private static void DrawPerforation(DrawingContext context, IBrush brush, double width, double height)
    {
        var right = width - HoleInset - HoleWidth;
        for (var y = HoleInset - ((HoleHeight - HoleWidth) / 2d); y + HoleHeight <= height - HoleInset + HoleWidth; y += HoleSpacing)
        {
            context.DrawRectangle(
                brush,
                null,
                new RoundedRect(new Rect(HoleInset, y, HoleWidth, HoleHeight), HoleRadius));
            context.DrawRectangle(
                brush,
                null,
                new RoundedRect(new Rect(right, y, HoleWidth, HoleHeight), HoleRadius));
        }
    }
}
