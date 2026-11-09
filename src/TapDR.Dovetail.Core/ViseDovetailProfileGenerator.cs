namespace TapDR.Dovetail.Core;

/// <summary>A 2D cross-section of a rectangular vise blank with a dovetail relief cut into its underside.</summary>
public sealed record ViseDovetailCrossSection
{
    public required double StockWidth { get; init; }
    public required double StockHeight { get; init; }
    public required double Depth { get; init; }
    public required double LeftLocatingFlat { get; init; }
    public required double RightLocatingFlat { get; init; }
    public required double FlankAngleDegrees { get; init; }

    /// <summary>Closed boundary points in counter-clockwise order, in the input unit.</summary>
    public required IReadOnlyList<ProfilePoint2D> Boundary { get; init; }

    public double MouthWidth => StockWidth - LeftLocatingFlat - RightLocatingFlat;
    public double UndercutPerSide { get; init; }
    public double MaximumDovetailWidth => MouthWidth + 2 * UndercutPerSide;
}

public readonly record struct ProfilePoint2D(double X, double Y);

public static class ViseDovetailProfileGenerator
{
    /// <summary>
    /// Creates a bottom-edge dovetail relief. FlankAngleDegrees is measured from
    /// the horizontal bottom face. The profile width is explicitly derived from
    /// the two locating flats and stock width; no undocumented DSP-V width datum
    /// is guessed. This is a geometric model, not authorization to machine it.
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

        var points = new[]
        {
            new ProfilePoint2D(0, 0),
            new ProfilePoint2D(mouthLeft, 0),
            new ProfilePoint2D(mouthLeft + undercut, p.DovetailDepth),
            new ProfilePoint2D(mouthRight - undercut, p.DovetailDepth),
            new ProfilePoint2D(mouthRight, 0),
            new ProfilePoint2D(p.StockWidth, 0),
            new ProfilePoint2D(p.StockWidth, p.StockHeight),
            new ProfilePoint2D(0, p.StockHeight)
        };

        if (points[2].X >= points[3].X)
            throw new ArgumentException(
                "The dovetail flanks overlap. Increase stock width or locating-flat span, or reduce depth/angle.", nameof(p));

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
