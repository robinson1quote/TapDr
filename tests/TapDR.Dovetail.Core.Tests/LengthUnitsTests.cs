using TapDR.Dovetail.Core;
using Xunit;

namespace TapDR.Dovetail.Core.Tests;

public sealed class LengthUnitsTests
{
    [Fact]
    public void InchToMillimeter_UsesExact25Point4Factor()
    {
        Assert.Equal(25.4, LengthUnits.ToMillimeters(1.0, LengthUnit.Inch), 10);
        Assert.Equal(50.8, LengthUnits.Convert(2.0, LengthUnit.Inch, LengthUnit.Millimeter), 10);
    }

    [Fact]
    public void MillimeterToInch_RoundTrips()
    {
        var inches = LengthUnits.Convert(63.5, LengthUnit.Millimeter, LengthUnit.Inch);
        Assert.Equal(2.5, inches, 10);
    }

    [Fact]
    public void Conversion_RejectsInvalidSourceUnit()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            LengthUnits.Convert(1.0, (LengthUnit)99, LengthUnit.Inch));
    }

    [Fact]
    public void Conversion_RejectsInvalidDestinationUnit()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            LengthUnits.Convert(1.0, LengthUnit.Inch, (LengthUnit)99));
    }
}
