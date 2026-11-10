using TapDR.Dovetail.Core;
using Xunit;

namespace TapDR.Dovetail.Core.Tests;

public sealed class ViseDovetailProfileGeneratorTests
{
    private static ViseStockPrepParameters ValidParameters(
        double width = 2.0,
        double height = 1.0,
        double depth = 0.080,
        double angle = 45.0,
        double edgeBreak = 0.010) =>
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
            MaximumEdgeBreak = edgeBreak
        };

    [Fact]
    public void Generate_ReturnsMaleTongueAndChamferedToeBelowLocatingPlane()
    {
        var profile = ViseDovetailProfileGenerator.Generate(ValidParameters());

        Assert.Equal(10, profile.Boundary.Count);
        Assert.Equal(new ProfilePoint2D(0, 0), profile.Boundary[0]);
        Assert.Equal(0.250, profile.Boundary[1].X, 10);
        Assert.Equal(0.0, profile.Boundary[1].Y, 10);
        Assert.Equal(0.1770710678118655, profile.Boundary[2].X, 10);
        Assert.Equal(-0.0729289321881345, profile.Boundary[2].Y, 10);
        Assert.Equal(0.180, profile.Boundary[3].X, 10);
        Assert.Equal(-0.080, profile.Boundary[3].Y, 10);
        Assert.Equal(1.820, profile.Boundary[4].X, 10);
        Assert.Equal(-0.080, profile.Boundary[4].Y, 10);
        Assert.Equal(1.8229289321881345, profile.Boundary[5].X, 10);
        Assert.Equal(-0.0729289321881345, profile.Boundary[5].Y, 10);
        Assert.Equal(1.750, profile.Boundary[6].X, 10);
        Assert.Equal(0.0, profile.Boundary[6].Y, 10);
        Assert.Equal(new ProfilePoint2D(2.0, 0), profile.Boundary[7]);
        Assert.Equal(new ProfilePoint2D(2.0, 1.0), profile.Boundary[8]);
        Assert.Equal(new ProfilePoint2D(0, 1.0), profile.Boundary[9]);
        Assert.Equal(0.080, profile.UndercutPerSide, 10);
        Assert.Equal(1.660, profile.ToeWidth, 10);
        Assert.Equal(0.010, profile.AppliedEdgeBreak, 10);
        Assert.True(profile.SignedArea > 0);
        Assert.True(PolygonGeometry.IsSimple(profile.Boundary));
        Assert.False(profile.IsProductionReady);
    }

    [Fact]
    public void Generate_OmitsChamferVerticesWhenEdgeBreakIsZero()
    {
        var profile = ViseDovetailProfileGenerator.Generate(ValidParameters(edgeBreak: 0));

        Assert.Equal(8, profile.Boundary.Count);
        Assert.Equal(0.170, profile.Boundary[2].X, 10);
        Assert.Equal(-0.080, profile.Boundary[2].Y, 10);
        Assert.Equal(1.830, profile.Boundary[3].X, 10);
        Assert.Equal(-0.080, profile.Boundary[3].Y, 10);
        Assert.Equal(0.0, profile.AppliedEdgeBreak, 10);
    }

    [Fact]
    public void Generate_UsesDepthAndAngleToCalculateToeExpansion()
    {
        var profile = ViseDovetailProfileGenerator.Generate(
            ValidParameters(depth: 0.100, angle: 30.0));

        var expected = 0.100 / Math.Tan(Math.PI / 6.0);
        Assert.Equal(expected, profile.UndercutPerSide, 10);
        Assert.Equal(1.5 + 2 * expected, profile.ToeWidth, 10);
    }

    [Fact]
    public void Generate_RejectsToeThatExtendsBeyondLocatingFlats()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            ViseDovetailProfileGenerator.Generate(ValidParameters(depth: 0.300)));

        Assert.Contains("extends beyond a locating flat", ex.Message, StringComparison.OrdinalIgnoreCase);
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

    [Fact]
    public void Generate_PreservesMetricUnitsAndMetricEdgeBreak()
    {
        var p = new ViseStockPrepParameters
        {
            StockLength = 101.6,
            StockWidth = 50.8,
            StockHeight = 25.4,
            Unit = LengthUnit.Millimeter,
            DovetailDepth = 2.032,
            LeftLocatingFlat = 6.35,
            RightLocatingFlat = 6.35,
            FlankAngleDegrees = 45.0,
            MaximumEdgeBreak = 0.254
        };

        var profile = ViseDovetailProfileGenerator.Generate(p);

        Assert.Equal(2.032, profile.UndercutPerSide, 10);
        Assert.Equal(4.064, profile.ToeWidth - profile.MouthWidth, 10);
        Assert.Equal(-2.032, profile.Boundary[3].Y, 10);
        Assert.Equal(0.254, profile.AppliedEdgeBreak, 10);
        Assert.True(PolygonGeometry.IsSimple(profile.Boundary));
    }

    [Fact]
    public void IsSimple_AcceptsCounterClockwiseRectangle()
    {
        ProfilePoint2D[] rectangle = [new(0, 0), new(2, 0), new(2, 1), new(0, 1)];

        Assert.True(PolygonGeometry.IsSimple(rectangle));
        Assert.Equal(2.0, PolygonGeometry.SignedArea(rectangle), 10);
    }

    [Fact]
    public void IsSimple_RejectsSelfIntersectingBowTie()
    {
        ProfilePoint2D[] bowTie = [new(0, 0), new(2, 2), new(0, 2), new(2, 0)];

        Assert.False(PolygonGeometry.IsSimple(bowTie));
    }

    [Fact]
    public void IsSimple_RejectsZeroLengthEdges()
    {
        ProfilePoint2D[] invalid = [new(0, 0), new(2, 0), new(2, 0), new(0, 1)];

        Assert.False(PolygonGeometry.IsSimple(invalid));
    }
}
