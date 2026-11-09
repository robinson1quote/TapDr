namespace TapDR.Dovetail.Core;

/// <summary>
/// A parametric cross-section primitive only. It is deliberately not marked
/// production-ready until the full drawing datum/edge-break interpretation and
/// native Mastercam solid verification are complete.
/// </summary>
public sealed record ViseDovetailCrossSection
{
    public required double StockWidth { get; init; }
    public required double StockHeight { get; init; }
    public required double Depth { get; init; }
    public required double LeftLocatingFlat { get; init; }
    public required double RightLocatingFlat { get; init; }
    public required double FlankAngleDegrees { get; init; }
    public required IReadOnlyList<ProfilePoint2D> Boundary { get; init; }
    public required double UndercutPerSide { get; init; }

    public double MouthWidth => StockWidth - LeftLocatingFlat - RightLocatingFlat;
    public double ToeWidth => MouthWidth + 2 * UndercutPerSide;
    public double SignedArea => PolygonGeometry.SignedArea(Boundary);
    public bool IsProductionReady => false;
}

public readonly record struct ProfilePoint2D(double X, double Y);

public static class PolygonGeometry
{
    /// <summary>Signed area for a polygon whose vertices are supplied in boundary order.</summary>
    public static double SignedArea(IReadOnlyList<ProfilePoint2D> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count < 3)
            return 0;

        double twiceArea = 0;
        for (var i = 0; i < points.Count; i++)
        {
            var current = points[i];
            var next = points[(i + 1) % points.Count];
            twiceArea += current.X * next.Y - next.X * current.Y;
        }

        return twiceArea / 2.0;
    }
}

public static class ViseDovetailProfileGenerator
{
    /// <summary>
    /// Builds the common DSP-V stock-prep outline from the manufacturer's drawing:
    /// two bottom locating flats at the stock edges and a central male dovetail
    /// tongue extending below the locating plane. Flank angle is measured from
    /// that locating plane. The horizontal toe offset is depth / tan(angle).
    /// </summary>
    public static ViseDovetailCrossSection Generate(ViseStockPrepParameters p)
    {
        ArgumentNullException.ThrowIfNull(p);

        var validation = ViseStockPrepValidator.Validate(p);
        if (!validation.CanGenerate)
            throw new ArgumentException(
                string.Join(" ", validation.Issues
                    .Where(i => i.Severity == ValidationSeverity.Error)
                    .Select(i => i.Message)), nameof(p));

        var radians = p.FlankAngleDegrees * Math.PI / 180.0;
        var undercut = p.DovetailDepth / Math.Tan(radians);
        if (!double.IsFinite(undercut) || undercut <= 0)
            throw new ArgumentException("The resulting dovetail undercut is not finite and positive.", nameof(p));

        // The lower, wider toe must remain inside the stock's outside width.
        // This preserves both locating flats and prevents overhanging geometry.
        if (undercut > p.LeftLocatingFlat || undercut > p.RightLocatingFlat)
            throw new ArgumentException(
                "The dovetail toe extends beyond a locating flat. Increase the flat length, reduce depth, or use a steeper flank angle.", nameof(p));

        var mouthLeft = p.LeftLocatingFlat;
        var mouthRight = p.StockWidth - p.RightLocatingFlat;
        var toeLeft = mouthLeft - undercut;
        var toeRight = mouthRight + undercut;

        // Boundary is counter-clockwise. Y=0 is the locating plane; the male
        // dovetail tongue projects toward negative Y by the drawing's depth C.
        var points = new[]
        {
            new ProfilePoint2D(0, 0),
            new ProfilePoint2D(mouthLeft, 0),
            new ProfilePoint2D(toeLeft, -p.DovetailDepth),
            new ProfilePoint2D(toeRight, -p.DovetailDepth),
            new ProfilePoint2D(mouthRight, 0),
            new ProfilePoint2D(p.StockWidth, 0),
            new ProfilePoint2D(p.StockWidth, p.StockHeight),
            new ProfilePoint2D(0, p.StockHeight)
        };

        if (points.Any(point => !double.IsFinite(point.X) || !double.IsFinite(point.Y)))
            throw new ArgumentException("The generated profile contains a non-finite coordinate.", nameof(p));

        var area = PolygonGeometry.SignedArea(points);
        if (!double.IsFinite(area) || area <= 0)
            throw new ArgumentException("The generated profile is degenerate or has invalid boundary winding.", nameof(p));

        return new ViseDovetailCrossSection
        {
            StockWidth = p.StockWidth,
            StockHeight = p.StockHeight,
            Depth = p.DovetailDepth,
            LeftLocatingFlat = p.LeftLocatingFlat,
            RightLocatingFlat = p.RightLocatingFlat,
            FlankAngleDegrees = p.FlankAngleDegrees,
            Boundary = points,
            UndercutPerSide = undercut
        };
    }
}
