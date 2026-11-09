namespace TapDR.Dovetail.Core;

/// <summary>
/// Drawing-backed preparation data for a workpiece blank held by a 5th Axis dovetail fixture.
/// This is not the 96 mm RockLock fixture-mounting interface.
/// </summary>
public sealed record FixtureStockPrepDefinition
{
    public required ManufacturerProfileMetadata Metadata { get; init; }
    public required string FixtureModel { get; init; }
    public required double SlotDepthInches { get; init; }
    public double FlankAngleDegrees { get; init; } = 45.0;
    public double? GaugePinDiameterInches { get; init; }
    public double? MaximumInternalRadiusInches { get; init; }
    public double? MaximumEdgeBreakInches { get; init; }

    /// <summary>
    /// Exact dimension callouts retained from the drawing but not yet mapped to
    /// coordinates by the profile engine. Do not use these as geometric inputs.
    /// </summary>
    public IReadOnlyList<string> UnmappedDrawingCallouts { get; init; } = Array.Empty<string>();

    public bool CanGenerateProductionGeometry => false;
}

public static class FixtureStockPrepCatalog
{
    private static readonly FixtureStockPrepDefinition[] Profiles =
    [
        new()
        {
            Metadata = Metadata("DSP-D115", "D115", "DSP-D115", "A", "https://5thaxis.com/wp-content/uploads/2021/05/Dovetail-Stock-Prep-Inch-Dovetails.pdf"),
            FixtureModel = "D115",
            SlotDepthInches = 0.080,
            GaugePinDiameterInches = 0.0625,
            MaximumInternalRadiusInches = 0.015,
            MaximumEdgeBreakInches = 0.010,
            UnmappedDrawingCallouts = [".713", ".357", ".2510 ± .0005", ".500 ± .020", ".250 programming dimension", ".126 +.000 / -.050 programming dimension"]
        },
        new()
        {
            Metadata = Metadata("DSP-D22", "D22", "DSP-D22", "A", "https://5thaxis.com/wp-content/uploads/2021/05/Dovetail-Stock-Prep-Inch-Dovetails.pdf"),
            FixtureModel = "D22",
            SlotDepthInches = 0.150,
            GaugePinDiameterInches = 0.1250,
            MaximumInternalRadiusInches = 0.015,
            MaximumEdgeBreakInches = 0.010,
            UnmappedDrawingCallouts = ["1.427", ".714", ".2510 ± .0005", ".625 ± .020", ".500 programming dimension", ".386 +.000 / -.050 programming dimension"]
        },
        new()
        {
            Metadata = Metadata("DSP-D05125", "D05125", "DSP-D05125", "A", "https://5thaxis.com/wp-content/uploads/2021/04/D05125-Dovetail-Stock-Preparation-2022.pdf"),
            FixtureModel = "D05125",
            SlotDepthInches = 0.080,
            GaugePinDiameterInches = null,
            MaximumInternalRadiusInches = 0.015,
            MaximumEdgeBreakInches = 0.010,
            UnmappedDrawingCallouts = [".413", ".207", ".1260 ± .0005", ".150 ± .020", ".100 programming dimension", ".066 +.000 / -.050 programming dimension", ".003 +.000 / -.050 programming dimension"]
        }
    ];

    public static IReadOnlyList<FixtureStockPrepDefinition> All => Profiles;

    public static FixtureStockPrepDefinition GetByFixtureModel(string fixtureModel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fixtureModel);
        return Profiles.SingleOrDefault(p =>
            string.Equals(p.FixtureModel, fixtureModel, StringComparison.OrdinalIgnoreCase))
            ?? throw new KeyNotFoundException($"No stock-prep profile is cataloged for fixture '{fixtureModel}'.");
    }

    private static ManufacturerProfileMetadata Metadata(
        string profileId, string fixtureModel, string drawingNumber, string revision, string sourceUrl) =>
        new()
        {
            ProfileId = profileId,
            Manufacturer = "5th Axis",
            DrawingNumber = drawingNumber,
            Revision = revision,
            SourceUrl = sourceUrl,
            DrawingUnit = LengthUnit.Inch,
            VerificationStatus = ProfileVerificationStatus.DrawingVerified
        };
}