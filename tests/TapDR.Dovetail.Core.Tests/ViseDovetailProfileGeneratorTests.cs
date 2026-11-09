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
    public void Generate_ReturnsClosedEightVertexBoundary()
    {
        var profile = ViseDovetailProfileGenerator.Generate(ValidParameters());

        Assert.Equal(8, profile.Boundary.Count);
        Assert.Equal(new ProfilePoint2D(0, 0), profile.Boundary[0]);
        Assert.Equal(new ProfilePoint2D(0, 1.0), profile.Boundary[^1]);
        Assert.Equal(0.080, profile.UndercutPerSide, 10);
        Assert.False(profile.IsProductionReady);
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
    public void Generate_RejectsOverlappingFlanks()
    {
        // Mouth is 0.15 in; each 45-degree flank consumes 0.08 in.
        var ex = Assert.Throws<ArgumentException>(() =>
            ViseDovetailProfileGenerator.Generate(ValidParameters(width: 0.65)));

        Assert.Contains("flanks overlap", ex.Message, StringComparison.OrdinalIgnoreCase);
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
}
