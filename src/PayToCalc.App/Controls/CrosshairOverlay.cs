using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace PayToCalc.App.Controls;

/// <summary>Vier Fadenkreuze an den Ecken der Live-Zone, rein dekorativ.</summary>
public sealed class CrosshairOverlay : Control
{
    /// <summary>Länge der Arme.</summary>
    public const double ArmLength = 12d;

    /// <summary>Farbe der Fadenkreuze.</summary>
    public static readonly StyledProperty<IBrush?> GuideBrushProperty =
        AvaloniaProperty.Register<CrosshairOverlay, IBrush?>(nameof(GuideBrush));

    static CrosshairOverlay()
    {
        AffectsRender<CrosshairOverlay>(GuideBrushProperty);
    }

    /// <summary>Farbe der Fadenkreuze.</summary>
    public IBrush? GuideBrush
    {
        get => GetValue(GuideBrushProperty);
        set => SetValue(GuideBrushProperty, value);
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (GuideBrush is not { } brush)
        {
            return;
        }

        var width = Bounds.Width;
        var height = Bounds.Height;
        if (width < ArmLength || height < ArmLength)
        {
            return;
        }

        var pen = new Pen(brush, 1);
        const double half = 0.5d;
        var left = half;
        var right = width - half;
        var top = half;
        var bottom = height - half;

        context.DrawLine(pen, new Point(left, top), new Point(left + ArmLength, top));
        context.DrawLine(pen, new Point(left, top), new Point(left, top + ArmLength));
        context.DrawLine(pen, new Point(right, top), new Point(right - ArmLength, top));
        context.DrawLine(pen, new Point(right, top), new Point(right, top + ArmLength));
        context.DrawLine(pen, new Point(left, bottom), new Point(left + ArmLength, bottom));
        context.DrawLine(pen, new Point(left, bottom), new Point(left, bottom - ArmLength));
        context.DrawLine(pen, new Point(right, bottom), new Point(right - ArmLength, bottom));
        context.DrawLine(pen, new Point(right, bottom), new Point(right, bottom - ArmLength));
    }
}
