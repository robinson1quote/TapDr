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
    public void Generate_AppliesToeEdgeBreakAndProjectsTongueBelowRawStock()
    {
        var profile = ViseDovetailProfileGenerator.Generate(ValidParameters());
        var points = profile.Boundary;

        Assert.Equal(10, points.Count);
        AssertPoint(points[0], 0.000, 0.000);
        AssertPoint(points[1], 0.250, 0.000);
        AssertPoint(points[2], 0.1770710678, -0.0729289322);
        AssertPoint(points[3], 0.180, -0.080);
        AssertPoint(points[4], 1.820, -0.080);
        AssertPoint(points[5], 1.8229289322, -0.0729289322);
        AssertPoint(points[6], 1.750, 0.000);
        AssertPoint(points[7], 2.000, 0.000);
        AssertPoint(points[8], 2.000, 1.000);
        AssertPoint(points[9], 0.000, 1.000);

        Assert.Equal(0.080, profile.UndercutPerSide, 10);
        Assert.Equal(1.660, profile.DovetailWidth, 10);
        Assert.Equal(0.830, profile.DovetailWidthRatio, 10);
        Assert.Equal(0.160, profile.MaximumDovetailWidth - profile.MouthWidth, 10);
        Assert.Equal(0.010, profile.AppliedEdgeBreak, 10);
        Assert.False(profile.IsProductionReady);

        Assert.All(points, p =>
        {
            Assert.InRange(p.X, 0.0, 2.0);
            Assert.InRange(p.Y, -0.080, 1.0);
        });
        Assert.True(SignedArea(points) > 0);
    }

    [Fact]
    public void Generate_CanOmitToeEdgeBreakWhenSetToZero()
    {
        var profile = ViseDovetailProfileGenerator.Generate(ValidParameters(edgeBreak: 0));

        Assert.Equal(8, profile.Boundary.Count);
        Assert.Equal(0.0, profile.AppliedEdgeBreak);
        AssertPoint(profile.Boundary[2], 0.170, -0.080);
        AssertPoint(profile.Boundary[3], 1.830, -0.080);
    }

    [Fact]
    public void Validator_WarnsWhenActualDovetailWidthIsBelowSeventyFivePercent()
    {
        var validation = ViseStockPrepValidator.Validate(ValidParameters(width: 1.2));

        Assert.True(validation.CanGenerate);
        Assert.True(validation.HasWarnings);
        Assert.Contains(validation.Issues, issue => issue.Code == "VISE-WIDTH-RATIO");
    }

    [Fact]
    public void Validator_DoesNotWarnAtExactDepthToleranceBoundary()
    {
        var validation = ViseStockPrepValidator.Validate(ValidParameters(depth: 0.085));

        Assert.True(validation.CanGenerate);
        Assert.DoesNotContain(validation.Issues, issue => issue.Code == "VISE-DEPTH-NONSTANDARD");
    }

    [Fact]
    public void Validator_DoesNotWarnAtExactFlankAngleToleranceBoundary()
    {
        var validation = ViseStockPrepValidator.Validate(ValidParameters(angle: 45.5));

        Assert.True(validation.CanGenerate);
        Assert.DoesNotContain(validation.Issues, issue => issue.Code == "VISE-ANGLE-NONSTANDARD");
    }

    [Fact]
    public void Validator_DefaultDrawingDimensionsHaveNoWarnings()
    {
        var validation = ViseStockPrepValidator.Validate(ValidParameters());

        Assert.True(validation.CanGenerate);
        Assert.False(validation.HasWarnings);
    }

    [Fact]
    public void Validator_WarnsWhenDepthIsOutsideDrawingTolerance()
    {
        var validation = ViseStockPrepValidator.Validate(ValidParameters(depth: 0.070));

        Assert.True(validation.CanGenerate);
        Assert.Contains(validation.Issues, issue => issue.Code == "VISE-DEPTH-NONSTANDARD");
    }

    [Fact]
    public void Validator_WarnsWhenFlankAngleIsOutsideDrawingTolerance()
    {
        var validation = ViseStockPrepValidator.Validate(ValidParameters(angle: 44.0));

        Assert.True(validation.CanGenerate);
        Assert.Contains(validation.Issues, issue => issue.Code == "VISE-ANGLE-NONSTANDARD");
    }

    [Fact]
    public void Validator_WarnsWhenToeEdgeBreakIsOmitted()
    {
        var validation = ViseStockPrepValidator.Validate(ValidParameters(edgeBreak: 0));

        Assert.True(validation.CanGenerate);
        Assert.Contains(validation.Issues, issue => issue.Code == "VISE-EDGE-BREAK-OMITTED");
    }

    [Fact]
    public void Generate_UsesDepthAndAngleToCalculateUndercut()
    {
        var profile = ViseDovetailProfileGenerator.Generate(
            ValidParameters(depth: 0.100, angle: 30.0, edgeBreak: 0));

        var expected = 0.100 / Math.Tan(Math.PI / 6.0);
        Assert.Equal(expected, profile.UndercutPerSide, 10);
    }

    [Fact]
    public void Generate_RejectsFlankThatConsumesLocatingFlat()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            ViseDovetailProfileGenerator.Generate(
                ValidParameters(depth: 0.300, angle: 30.0)));

        Assert.Contains("smaller than both locating-flat lengths", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Generate_RejectsEdgeBreakThatCannotFitTheToeFlank()
    {
        var p = ValidParameters(depth: 0.001, edgeBreak: 0.010);

        var ex = Assert.Throws<ArgumentException>(() =>
            ViseDovetailProfileGenerator.Generate(p));

        Assert.Contains("edge break is too large", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Generate_RejectsNonFiniteStockDimensions()
    {
        var p = ValidParameters() with { StockWidth = double.NaN };
        Assert.Throws<ArgumentException>(() => ViseDovetailProfileGenerator.Generate(p));
    }

    [Fact]
    public void Generate_RejectsInvalidFlankAngle()
    {
        var p = ValidParameters(angle: double.PositiveInfinity);
        Assert.Throws<ArgumentException>(() => ViseDovetailProfileGenerator.Generate(p));
    }

    [Fact]
    public void Generate_RejectsDepthEqualToOrGreaterThanStockHeight()
    {
        var equal = ValidParameters(depth: 1.0);
        var greater = ValidParameters(depth: 1.1);

        Assert.Throws<ArgumentException>(() => ViseDovetailProfileGenerator.Generate(equal));
        Assert.Throws<ArgumentException>(() => ViseDovetailProfileGenerator.Generate(greater));
    }

    [Fact]
    public void Generate_RejectsInvalidUnit()
    {
        var p = ValidParameters() with { Unit = (LengthUnit)99 };
        Assert.Throws<ArgumentException>(() => ViseDovetailProfileGenerator.Generate(p));
    }

    [Fact]
    public void Generate_RejectsEdgeBreakAboveManufacturerMaximum()
    {
        var p = ValidParameters() with { MaximumEdgeBreak = 0.0101 };
        Assert.Throws<ArgumentException>(() => ViseDovetailProfileGenerator.Generate(p));
    }

    private static void AssertPoint(ProfilePoint2D actual, double x, double y)
    {
        Assert.Equal(x, actual.X, 10);
        Assert.Equal(y, actual.Y, 10);
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
