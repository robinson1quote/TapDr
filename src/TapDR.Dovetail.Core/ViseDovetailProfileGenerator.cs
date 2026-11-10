namespace TapDR.Dovetail.Core;

/// <summary>
/// A parametric cross-section primitive only. It is deliberately not marked
/// production-ready until drawing datums, edge-break geometry, and solid tests
/// are complete.
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

    /// <summary>Width of the groove at the stock underside.</summary>
    public double MouthWidth => StockWidth - LeftLocatingFlat - RightLocatingFlat;

    /// <summary>Width between the two outer dovetail points at full depth.</summary>
    public double DovetailWidth => MouthWidth + 2 * UndercutPerSide;

    public double DovetailWidthRatio => DovetailWidth / StockWidth;
    public bool IsProductionReady => false;
}

public readonly record struct ProfilePoint2D(double X, double Y);

public static class ViseDovetailProfileGenerator
{
    /// <summary>
    /// Creates the DSP-V underside relief boundary. FlankAngleDegrees is measured
    /// from the horizontal underside. The groove widens toward its deeper floor.
    /// All coordinates use the input unit.
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
        var mouthLeft = p.LeftLocatingFlat;
        var mouthRight = p.StockWidth - p.RightLocatingFlat;

        // The dovetail opens outward with depth: left point moves left and
        // right point moves right. The locating flats must remain positive.
        if (undercut >= p.LeftLocatingFlat || undercut >= p.RightLocatingFlat)
            throw new ArgumentException(
                "The dovetail undercut consumes a locating flat. Increase the locating-flat length or reduce depth.", nameof(p));

        var points = new[]
        {
            new ProfilePoint2D(0, 0),
            new ProfilePoint2D(mouthLeft, 0),
            new ProfilePoint2D(mouthLeft - undercut, p.DovetailDepth),
            new ProfilePoint2D(mouthRight + undercut, p.DovetailDepth),
            new ProfilePoint2D(mouthRight, 0),
            new ProfilePoint2D(p.StockWidth, 0),
            new ProfilePoint2D(p.StockWidth, p.StockHeight),
            new ProfilePoint2D(0, p.StockHeight)
        };

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
