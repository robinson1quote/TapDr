namespace TapDR.Dovetail.Core;

/// <summary>
/// Parametric DSP-V cross-section primitive only. It represents the male dovetail
/// tongue milled into the underside of a rectangular raw blank. The outline
/// includes the requested toe edge breaks; exact cutter datum mapping and final
/// production release remain separate requirements.
/// </summary>
public sealed record ViseDovetailCrossSection
{
    public required double StockWidth { get; init; }
    public required double StockHeight { get; init; }
    public required double Depth { get; init; }
    public required double LeftLocatingFlat { get; init; }
    public required double RightLocatingFlat { get; init; }
    public required double FlankAngleDegrees { get; init; }
    public required double AppliedEdgeBreak { get; init; }
    public required IReadOnlyList<ProfilePoint2D> Boundary { get; init; }

    public double MouthWidth => StockWidth - LeftLocatingFlat - RightLocatingFlat;
    public double UndercutPerSide => Depth / Math.Tan(FlankAngleDegrees * Math.PI / 180.0);
    public double MaximumDovetailWidth => MouthWidth + 2 * UndercutPerSide;
    public double DovetailWidth => MaximumDovetailWidth;
    public double DovetailWidthRatio => StockWidth > 0 ? DovetailWidth / StockWidth : 0;
    public bool IsProductionReady => false;
}

public readonly record struct ProfilePoint2D(double X, double Y);

public static class ViseDovetailProfileGenerator
{
    /// <summary>
    /// Creates a closed stock section. Y=0 is the underside of the rectangular
    /// raw blank. The male dovetail extends downward to Y=-Depth; the stock top
    /// is at Y=StockHeight. FlankAngleDegrees is measured from the horizontal.
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
        var leftFlankPoint = new ProfilePoint2D(p.LeftLocatingFlat, 0);
        var leftToePoint = new ProfilePoint2D(leftToe, -p.DovetailDepth);
        var rightToePoint = new ProfilePoint2D(rightToe, -p.DovetailDepth);
        var rightFlankPoint = new ProfilePoint2D(p.StockWidth - p.RightLocatingFlat, 0);

        var points = new List<ProfilePoint2D>
        {
            new(0, 0),
            leftFlankPoint
        };

        if (p.MaximumEdgeBreak > 0)
        {
            var leftFlankLength = Distance(leftToePoint, leftFlankPoint);
            var rightFlankLength = Distance(rightToePoint, rightFlankPoint);
            var toeLength = Distance(leftToePoint, rightToePoint);
            if (p.MaximumEdgeBreak >= leftFlankLength ||
                p.MaximumEdgeBreak >= rightFlankLength ||
                2 * p.MaximumEdgeBreak >= toeLength)
            {
                throw new ArgumentException(
                    "The selected toe edge break is too large for the dovetail flank or locating face.", nameof(p));
            }

            // Chamfer both lower toe corners by the specified edge-break
            // distance measured along each adjacent profile edge.
            points.Add(PointToward(leftToePoint, leftFlankPoint, p.MaximumEdgeBreak));
            points.Add(PointToward(leftToePoint, rightToePoint, p.MaximumEdgeBreak));
            points.Add(PointToward(rightToePoint, leftToePoint, p.MaximumEdgeBreak));
            points.Add(PointToward(rightToePoint, rightFlankPoint, p.MaximumEdgeBreak));
        }
        else
        {
            points.Add(leftToePoint);
            points.Add(rightToePoint);
        }

        points.Add(rightFlankPoint);
        points.Add(new ProfilePoint2D(p.StockWidth, 0));
        points.Add(new ProfilePoint2D(p.StockWidth, p.StockHeight));
        points.Add(new ProfilePoint2D(0, p.StockHeight));

        return new ViseDovetailCrossSection
        {
            StockWidth = p.StockWidth,
            StockHeight = p.StockHeight,
            Depth = p.DovetailDepth,
            LeftLocatingFlat = p.LeftLocatingFlat,
            RightLocatingFlat = p.RightLocatingFlat,
            FlankAngleDegrees = p.FlankAngleDegrees,
            AppliedEdgeBreak = p.MaximumEdgeBreak,
            Boundary = Array.AsReadOnly(points.ToArray())
        };
    }

    private static double Distance(ProfilePoint2D a, ProfilePoint2D b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static ProfilePoint2D PointToward(
        ProfilePoint2D origin, ProfilePoint2D target, double distance)
    {
        var length = Distance(origin, target);
        if (length <= 0 || distance < 0 || distance >= length)
            throw new ArgumentOutOfRangeException(nameof(distance));

        var ratio = distance / length;
        return new ProfilePoint2D(
            origin.X + (target.X - origin.X) * ratio,
            origin.Y + (target.Y - origin.Y) * ratio);
    }
}
