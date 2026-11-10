namespace TapDR.Dovetail.Core;

/// <summary>
/// Parametric cross-section primitive only. The profile is a projecting male
/// dovetail tongue below the stock's underside, matching the orientation in
/// the 5th Axis DSP-V drawing. Drawing datum mapping and edge-break geometry
/// remain incomplete, so this primitive is not production-ready.
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
    public double MaximumDovetailWidth => MouthWidth + 2 * UndercutPerSide;
    public bool IsProductionReady => false;
}

public readonly record struct ProfilePoint2D(double X, double Y);

public static class ViseDovetailProfileGenerator
{
    /// <summary>
    /// Creates a closed 2D stock section with a male dovetail tongue projecting
    /// below Y=0. FlankAngleDegrees is measured from the horizontal underside.
    /// Left/right locating flats are the undisturbed underside lands at each
    /// stock edge. The exact B/C datum relationship still requires full
    /// drawing mapping before production release.
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

        // A dovetail flank must not consume the whole locating flat and break
        // through the stock side. Reject instead of silently clipping it.
        if (undercut >= p.LeftLocatingFlat || undercut >= p.RightLocatingFlat)
            throw new ArgumentException(
                "The dovetail flank consumes a locating flat. Reduce depth or use a steeper flank angle.", nameof(p));

        var leftToe = p.LeftLocatingFlat - undercut;
        var rightToe = p.StockWidth - p.RightLocatingFlat + undercut;

        // Counter-clockwise closed perimeter (implicit closing edge last->first).
        var points = new[]
        {
            new ProfilePoint2D(0, 0),
            new ProfilePoint2D(p.LeftLocatingFlat, 0),
            new ProfilePoint2D(leftToe, -p.DovetailDepth),
            new ProfilePoint2D(rightToe, -p.DovetailDepth),
            new ProfilePoint2D(p.StockWidth - p.RightLocatingFlat, 0),
            new ProfilePoint2D(p.StockWidth, 0),
            new ProfilePoint2D(p.StockWidth, p.StockHeight),
            new ProfilePoint2D(0, p.StockHeight)
        };

        if (leftToe >= rightToe)
            throw new ArgumentException(
                "The dovetail locating surface has no usable width.", nameof(p));

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
