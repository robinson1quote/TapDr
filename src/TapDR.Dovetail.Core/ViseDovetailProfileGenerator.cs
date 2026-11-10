namespace TapDR.Dovetail.Core;

/// <summary>
/// Parametric DSP-V cross-section primitive only. It represents a male dovetail
/// tongue milled from the underside of a rectangular raw blank: the lowest
/// locating face is at Y=0, the two outer underside flats are at Y=Depth, and
/// the top remains at Y=StockHeight. It is not production-released until exact
/// drawing datum mapping, edge-break geometry, and solid tests are complete.
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
    /// Creates a closed stock section. FlankAngleDegrees is measured from the
    /// horizontal underside. The 0.250-inch minimum end flats, nominal depth,
    /// and 45-degree flanks come from DSP-V. Exact dimension-B cutter datum
    /// mapping is still a separate production-release requirement.
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

        var undercut = p.DovetailDepth /
                       Math.Tan(p.FlankAngleDegrees * Math.PI / 180.0);

        if (undercut >= p.LeftLocatingFlat || undercut >= p.RightLocatingFlat)
            throw new ArgumentException(
                "The dovetail flank consumes a locating flat. Reduce depth or use a steeper flank angle.", nameof(p));

        var leftToe = p.LeftLocatingFlat - undercut;
        var rightToe = p.StockWidth - p.RightLocatingFlat + undercut;

        // CCW boundary of the material remaining after the underside cuts.
        // This keeps the complete profile inside the raw rectangular stock
        // envelope: X=[0, StockWidth], Y=[0, StockHeight].
        var points = new[]
        {
            new ProfilePoint2D(0, p.DovetailDepth),
            new ProfilePoint2D(p.LeftLocatingFlat, p.DovetailDepth),
            new ProfilePoint2D(leftToe, 0),
            new ProfilePoint2D(rightToe, 0),
            new ProfilePoint2D(p.StockWidth - p.RightLocatingFlat, p.DovetailDepth),
            new ProfilePoint2D(p.StockWidth, p.DovetailDepth),
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
            Boundary = Array.AsReadOnly(points),
            UndercutPerSide = undercut
        };
    }
}
