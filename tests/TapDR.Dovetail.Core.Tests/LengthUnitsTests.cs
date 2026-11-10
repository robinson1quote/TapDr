using TapDR.Dovetail.Core;
using Xunit;

namespace TapDR.Dovetail.Core.Tests;

public sealed class LengthUnitsTests
{
    [Theory]
    [InlineData(1.0, 25.4)]
    [InlineData(0.5, 12.7)]
    [InlineData(2.0, 50.8)]
    [InlineData(0.125, 3.175)]
    public void Convert_InchesToMillimeters_UsesExactInternationalConversion(
        double inches, double expectedMillimeters)
    {
        var actual = LengthUnits.Convert(inches, LengthUnit.Inch, LengthUnit.Millimeter);
        Assert.Equal(expectedMillimeters, actual, 10);
    }

    [Theory]
    [InlineData(25.4, 1.0)]
    [InlineData(12.7, 0.5)]
    [InlineData(50.8, 2.0)]
    [InlineData(3.175, 0.125)]
    public void Convert_MillimetersToInches_UsesExactInternationalConversion(
        double millimeters, double expectedInches)
    {
        var actual = LengthUnits.Convert(millimeters, LengthUnit.Millimeter, LengthUnit.Inch);
        Assert.Equal(expectedInches, actual, 10);
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(0.080)]
    [InlineData(1.0)]
    [InlineData(123.456)]
    public void Convert_RoundTripsWithoutChangingLength(double value)
    {
        var millimeters = LengthUnits.Convert(value, LengthUnit.Inch, LengthUnit.Millimeter);
        var roundTrip = LengthUnits.Convert(millimeters, LengthUnit.Millimeter, LengthUnit.Inch);
        Assert.Equal(value, roundTrip, 12);
    }

    [Fact]
    public void Convert_RejectsInvalidSourceUnit()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            LengthUnits.Convert(1.0, (LengthUnit)99, LengthUnit.Inch));
    }

    [Fact]
    public void Convert_RejectsInvalidTargetUnit()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            LengthUnits.Convert(1.0, LengthUnit.Inch, (LengthUnit)99));
    }
}
