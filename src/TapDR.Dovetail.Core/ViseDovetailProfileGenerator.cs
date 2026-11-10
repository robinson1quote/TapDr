namespace TapDR.Dovetail.Core;

/// <summary>
/// Parametric cross-section primitive for the DSP-V male dovetail stock-prep
/// tongue. It is not production-authorized until Mastercam solid verification.
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
    public required double UndercutPerSide { get; init; }

    public double MouthWidth => StockWidth - LeftLocatingFlat - RightLocatingFlat;
    public double ToeWidth => MouthWidth + 2 * UndercutPerSide;
    public double SignedArea => PolygonGeometry.SignedArea(Boundary);
    public bool IsProductionReady => false;
}

public readonly record struct ProfilePoint2D(double X, double Y);

public static class PolygonGeometry
{
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

    public static bool IsSimple(IReadOnlyList<ProfilePoint2D> points, double tolerance = 1e-10)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count < 3 || !double.IsFinite(tolerance) || tolerance < 0)
            return false;

        for (var i = 0; i < points.Count; i++)
        {
            var a = points[i];
            var b = points[(i + 1) % points.Count];
            if (!IsFinite(a) || !IsFinite(b) || DistanceSquared(a, b) <= tolerance * tolerance)
                return false;
        }

        for (var i = 0; i < points.Count; i++)
        {
            var a1 = points[i];
            var a2 = points[(i + 1) % points.Count];

            for (var j = i + 1; j < points.Count; j++)
            {
                // Adjacent edges share one endpoint by definition.
                if (j == i || j == (i + 1) % points.Count || i == (j + 1) % points.Count)
                    continue;

                var b1 = points[j];
                var b2 = points[(j + 1) % points.Count];
                if (SegmentsIntersect(a1, a2, b1, b2, tolerance))
                    return false;
            }
        }

        return true;
    }

    private static bool IsFinite(ProfilePoint2D point) =>
        double.IsFinite(point.X) && double.IsFinite(point.Y);

    private static double DistanceSquared(ProfilePoint2D a, ProfilePoint2D b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }

    private static bool SegmentsIntersect(
        ProfilePoint2D a, ProfilePoint2D b,
        ProfilePoint2D c, ProfilePoint2D d,
        double tolerance)
    {
        var o1 = Orientation(a, b, c);
        var o2 = Orientation(a, b, d);
        var o3 = Orientation(c, d, a);
        var o4 = Orientation(c, d, b);

        if (((o1 > tolerance && o2 < -tolerance) || (o1 < -tolerance && o2 > tolerance)) &&
            ((o3 > tolerance && o4 < -tolerance) || (o3 < -tolerance && o4 > tolerance)))
            return true;

        return Math.Abs(o1) <= tolerance && OnSegment(a, b, c, tolerance)
            || Math.Abs(o2) <= tolerance && OnSegment(a, b, d, tolerance)
            || Math.Abs(o3) <= tolerance && OnSegment(c, d, a, tolerance)
            || Math.Abs(o4) <= tolerance && OnSegment(c, d, b, tolerance);
    }

    private static double Orientation(ProfilePoint2D a, ProfilePoint2D b, ProfilePoint2D c) =>
        (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);

    private static bool OnSegment(ProfilePoint2D a, ProfilePoint2D b, ProfilePoint2D p, double tolerance) =>
        p.X >= Math.Min(a.X, b.X) - tolerance &&
        p.X <= Math.Max(a.X, b.X) + tolerance &&
        p.Y >= Math.Min(a.Y, b.Y) - tolerance &&
        p.Y <= Math.Max(a.Y, b.Y) + tolerance;
}

public static class ViseDovetailProfileGenerator
{
    /// <summary>
    /// Builds the DSP-V male dovetail tongue below the stock locating plane.
    /// Flank angle is measured from the locating plane. The edge break is a
    /// symmetric chamfer setback at each toe corner, limited by the drawing's
    /// maximum edge-break allowance.
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
        var sin = Math.Sin(radians);
        var tan = Math.Tan(radians);
        var undercut = p.DovetailDepth / tan;
        if (!double.IsFinite(undercut) || undercut <= 0 || !double.IsFinite(sin) || sin <= 0)
            throw new ArgumentException("The resulting dovetail undercut is not finite and positive.", nameof(p));

        if (undercut > p.LeftLocatingFlat || undercut > p.RightLocatingFlat)
            throw new ArgumentException(
                "The dovetail toe extends beyond a locating flat. Increase the flat length, reduce depth, or use a steeper flank angle.", nameof(p));

        var mouthLeft = p.LeftLocatingFlat;
        var mouthRight = p.StockWidth - p.RightLocatingFlat;
        var toeLeft = mouthLeft - undercut;
        var toeRight = mouthRight + undercut;
        var flankLength = p.DovetailDepth / sin;
        var edgeBreak = p.MaximumEdgeBreak;

        if (edgeBreak > 0 && (edgeBreak >= flankLength || edgeBreak * 2 >= toeRight - toeLeft))
            throw new ArgumentException(
                "The requested edge break is too large for the dovetail toe geometry.", nameof(p));

        var points = new List<ProfilePoint2D>(edgeBreak > 0 ? 12 : 8)
        {
            new(0, 0),
            new(mouthLeft, 0)
        };

        if (edgeBreak > 0)
        {
            // Set back from the sharp toe corner along the inclined flank and
            // along the bottom toe face by the same edge-break distance.
            var flankDx = edgeBreak * Math.Cos(radians);
            var flankDy = edgeBreak * sin;
            points.Add(new ProfilePoint2D(toeLeft + flankDx, -p.DovetailDepth + flankDy));
            points.Add(new ProfilePoint2D(toeLeft + edgeBreak, -p.DovetailDepth));
            points.Add(new ProfilePoint2D(toeRight - edgeBreak, -p.DovetailDepth));
            points.Add(new ProfilePoint2D(toeRight - flankDx, -p.DovetailDepth + flankDy));
        }
        else
        {
            points.Add(new ProfilePoint2D(toeLeft, -p.DovetailDepth));
            points.Add(new ProfilePoint2D(toeRight, -p.DovetailDepth));
        }

        points.Add(new ProfilePoint2D(mouthRight, 0));
        points.Add(new ProfilePoint2D(p.StockWidth, 0));
        points.Add(new ProfilePoint2D(p.StockWidth, p.StockHeight));
        points.Add(new ProfilePoint2D(0, p.StockHeight));

        if (!PolygonGeometry.IsSimple(points))
            throw new ArgumentException("The generated profile is self-intersecting or degenerate.", nameof(p));

        var area = PolygonGeometry.SignedArea(points);
        if (!double.IsFinite(area) || area <= 0)
            throw new ArgumentException("The generated profile has invalid boundary winding or zero area.", nameof(p));

        return new ViseDovetailCrossSection
        {
            StockWidth = p.StockWidth,
            StockHeight = p.StockHeight,
            Depth = p.DovetailDepth,
            LeftLocatingFlat = p.LeftLocatingFlat,
            RightLocatingFlat = p.RightLocatingFlat,
            FlankAngleDegrees = p.FlankAngleDegrees,
            AppliedEdgeBreak = edgeBreak,
            Boundary = points,
            UndercutPerSide = undercut
        };
    }
}
