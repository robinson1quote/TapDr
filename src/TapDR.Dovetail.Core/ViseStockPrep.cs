namespace TapDR.Dovetail.Core;

public sealed record ViseStockPrepParameters
{
    public required double StockLength { get; init; }
    public required double StockWidth { get; init; }
    public required double StockHeight { get; init; }
    public LengthUnit Unit { get; init; } = LengthUnit.Inch;
    public double DovetailDepth { get; init; } = 0.080;
    public double LeftLocatingFlat { get; init; } = 0.250;
    public double RightLocatingFlat { get; init; } = 0.250;
    public double FlankAngleDegrees { get; init; } = 45.0;
    public double MaximumEdgeBreak { get; init; } = 0.010;

    public static ViseStockPrepParameters CreateDefaults(
        double length, double width, double height, LengthUnit unit) =>
        new()
        {
            StockLength = length,
            StockWidth = width,
            StockHeight = height,
            Unit = unit,
            DovetailDepth = LengthUnits.Convert(0.080, LengthUnit.Inch, unit),
            LeftLocatingFlat = LengthUnits.Convert(0.250, LengthUnit.Inch, unit),
            RightLocatingFlat = LengthUnits.Convert(0.250, LengthUnit.Inch, unit),
            MaximumEdgeBreak = LengthUnits.Convert(0.010, LengthUnit.Inch, unit)
        };
}

public enum ValidationSeverity
{
    Error,
    Warning
}

public sealed record ValidationIssue(
    string Code, ValidationSeverity Severity, string Message);

public sealed record ViseStockPrepValidation(
    IReadOnlyList<ValidationIssue> Issues)
{
    public bool CanGenerate => Issues.All(i => i.Severity != ValidationSeverity.Error);
    public bool HasWarnings => Issues.Any(i => i.Severity == ValidationSeverity.Warning);
}

public static class ViseStockPrepValidator
{
    private const double MinimumFlatInches = 0.250;
    private const double NominalDepthInches = 0.080;
    private const double MaximumEdgeBreakInches = 0.010;

    public static ViseStockPrepValidation Validate(ViseStockPrepParameters p)
    {
        ArgumentNullException.ThrowIfNull(p);
        var issues = new List<ValidationIssue>();

        if (!Enum.IsDefined(p.Unit))
            Error("INPUT-UNIT", "The selected length unit is not supported.");

        CheckPositive(p.StockLength, nameof(p.StockLength));
        CheckPositive(p.StockWidth, nameof(p.StockWidth));
        CheckPositive(p.StockHeight, nameof(p.StockHeight));
        CheckPositive(p.DovetailDepth, nameof(p.DovetailDepth));
        CheckPositive(p.LeftLocatingFlat, nameof(p.LeftLocatingFlat));
        CheckPositive(p.RightLocatingFlat, nameof(p.RightLocatingFlat));

        if (!double.IsFinite(p.FlankAngleDegrees) ||
            p.FlankAngleDegrees <= 0 || p.FlankAngleDegrees >= 90)
            Error("VISE-ANGLE", "Flank angle must be finite, greater than 0 and less than 90 degrees.");

        if (!double.IsFinite(p.MaximumEdgeBreak) ||
            p.MaximumEdgeBreak < 0)
            Error("VISE-EDGE-BREAK", "Edge break must be a finite, non-negative value.");

        if (!Enum.IsDefined(p.Unit))
            return new ViseStockPrepValidation(issues);

        var minFlat = LengthUnits.Convert(MinimumFlatInches, LengthUnit.Inch, p.Unit);
        var nominalDepth = LengthUnits.Convert(NominalDepthInches, LengthUnit.Inch, p.Unit);
        var maxEdge = LengthUnits.Convert(MaximumEdgeBreakInches, LengthUnit.Inch, p.Unit);

        if (p.LeftLocatingFlat < minFlat)
            Error("VISE-FLAT-L", $"Left locating flat must be at least {minFlat:G6} {p.Unit}.");
        if (p.RightLocatingFlat < minFlat)
            Error("VISE-FLAT-R", $"Right locating flat must be at least {minFlat:G6} {p.Unit}.");
        if (p.DovetailDepth >= p.StockHeight)
            Error("VISE-DEPTH-STOCK", "Dovetail depth must be less than stock height so the stock retains a top web.");
        if (double.IsFinite(p.MaximumEdgeBreak) && p.MaximumEdgeBreak > maxEdge)
            Error("VISE-EDGE-BREAK-LIMIT", $"Edge break cannot exceed {maxEdge:G6} {p.Unit}.");
        if (p.DovetailDepth > nominalDepth)
            Warning("VISE-DEPTH-NONSTANDARD", $"Entered depth exceeds the DSP-V nominal {nominalDepth:G6} {p.Unit}; verify the setup before machining.");

        if (p.StockWidth <= p.LeftLocatingFlat + p.RightLocatingFlat)
            Error("VISE-FLATS-WIDTH", "Stock width must exceed the combined locating-flat widths.");

        return new ViseStockPrepValidation(issues);

        void CheckPositive(double value, string name)
        {
            if (!double.IsFinite(value) || value <= 0)
                Error("INPUT-POSITIVE", $"{name} must be a finite value greater than zero.");
        }
        void Error(string code, string message) =>
            issues.Add(new ValidationIssue(code, ValidationSeverity.Error, message));
        void Warning(string code, string message) =>
            issues.Add(new ValidationIssue(code, ValidationSeverity.Warning, message));
    }
}
