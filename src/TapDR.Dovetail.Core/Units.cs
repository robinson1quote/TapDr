namespace TapDR.Dovetail.Core;

public enum LengthUnit
{
    Inch,
    Millimeter
}

public static class LengthUnits
{
    public const double MillimetersPerInch = 25.4;

    public static double ToMillimeters(double value, LengthUnit unit)
    {
        ValidateUnit(unit, nameof(unit));
        return unit == LengthUnit.Inch ? value * MillimetersPerInch : value;
    }

    public static double FromMillimeters(double value, LengthUnit unit)
    {
        ValidateUnit(unit, nameof(unit));
        return unit == LengthUnit.Inch ? value / MillimetersPerInch : value;
    }

    public static double Convert(double value, LengthUnit from, LengthUnit to) =>
        FromMillimeters(ToMillimeters(value, from), to);

    private static void ValidateUnit(LengthUnit unit, string parameterName)
    {
        if (!Enum.IsDefined(unit))
            throw new ArgumentOutOfRangeException(parameterName, unit, "Unsupported length unit.");
    }
}
