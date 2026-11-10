using TapDR.Dovetail.Core;
using Xunit;

namespace TapDR.Dovetail.Core.Tests;

public sealed class ViseDovetailProfileGeneratorTests
{
    private static ViseStockPrepParameters ValidParameters(
        double width = 2.0,
        double height = 1.0,
        double depth = 0.080,
        double angle = 45.0) =>
        new()
        {
            StockLength = 4.0,
            StockWidth = width,
            StockHeight = height,
            Unit = LengthUnit.Inch,
            DovetailDepth = depth,
            LeftLocatingFlat = 0.250,
            RightLocatingFlat = 0.250,
            FlankAngleDegrees = angle,
            MaximumEdgeBreak = 0.010
        };

    [Fact]
    public void Generate_ReturnsEightPointProfileInsideRawStockEnvelope()
    {
        var profile = ViseDovetailProfileGenerator.Generate(ValidParameters());

        Assert.Equal(8, profile.Boundary.Count);
        Assert.Equal(new ProfilePoint2D(0, 0.080), profile.Boundary[2] with { });
        Assert.Equal(new ProfilePoint2D(2.0, 1.0), profile.Boundary[6]);
        Assert.Equal(0.080, profile.UndercutPerSide, 10);
        Assert.Equal(0.160, profile.MaximumDovetailWidth - profile.MouthWidth, 10);
        Assert.False(profile.IsProductionReady);

        // The polygon must wind counter-clockwise and remain within the
        // entered rectangular stock envelope (no stock added below Z=0).
        Assert.All(profile.Boundary, p =>
        {
            Assert.InRange(p.X, 0.0, 2.0);
            Assert.InRange(p.Y, 0.0, 1.0);
        });
        Assert.True(SignedArea(profile.Boundary) > 0);
    }

    [Fact]
    public void Generate_UsesDepthAndAngleToCalculateUndercut()
    {
        var profile = ViseDovetailProfileGenerator.Generate(
            ValidParameters(depth: 0.100, angle: 30.0));

        var expected = 0.100 / Math.Tan(Math.PI / 6.0);
        Assert.Equal(expected, profile.UndercutPerSide, 10);
    }

    [Fact]
    public void Generate_RejectsFlankThatConsumesLocatingFlat()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            ViseDovetailProfileGenerator.Generate(
                ValidParameters(depth: 0.300, angle: 30.0)));

        Assert.Contains("consumes a locating flat", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Generate_RejectsNonFiniteStockDimensions()
    {
        var p = ValidParameters() with { StockWidth = double.NaN };

        Assert.Throws<ArgumentException>(() =>
            ViseDovetailProfileGenerator.Generate(p));
    }

    [Fact]
    public void Generate_RejectsInvalidFlankAngle()
    {
        var p = ValidParameters(angle: double.PositiveInfinity);

        Assert.Throws<ArgumentException>(() =>
            ViseDovetailProfileGenerator.Generate(p));
    }

    [Fact]
    public void Generate_RejectsDepthGreaterThanStockHeight()
    {
        var p = ValidParameters(depth: 1.1);

        Assert.Throws<ArgumentException>(() =>
            ViseDovetailProfileGenerator.Generate(p));
    }

    private static double SignedArea(IReadOnlyList<ProfilePoint2D> points)
    {
        var area = 0.0;
        for (var i = 0; i < points.Count; i++)
        {
            var next = (i + 1) % points.Count;
            area += points[i].X * points[next].Y
                  - points[next].X * points[i].Y;
        }
        return area / 2.0;
    }
}
