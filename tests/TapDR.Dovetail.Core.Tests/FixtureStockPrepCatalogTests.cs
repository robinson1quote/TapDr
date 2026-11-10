using TapDR.Dovetail.Core;
using Xunit;

namespace TapDR.Dovetail.Core.Tests;

public sealed class FixtureStockPrepCatalogTests
{
    [Theory]
    [InlineData("D115", 0.080, 0.2510, 0.0625)]
    [InlineData("D22", 0.150, 0.2510, 0.1250)]
    [InlineData("D05125", 0.080, 0.1260, 0.0625)]
    public void Catalog_ContainsDrawingBackedFixtureDimensions(
        string model, double expectedDepth, double expectedSlotWidth, double expectedGaugePin)
    {
        var profile = FixtureStockPrepCatalog.GetByFixtureModel(model);

        Assert.Equal(expectedDepth, profile.SlotDepthInches, 10);
        Assert.Equal(expectedSlotWidth, profile.SlotWidthInches, 10);
        Assert.Equal(0.0005, profile.SlotWidthToleranceInches, 10);
        Assert.DoesNotContain(
            profile.UnmappedDrawingCallouts,
            callout => callout.Contains(expectedSlotWidth.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal));
        Assert.Equal(expectedGaugePin, profile.GaugePinDiameterInches!.Value, 10);
        Assert.Equal(45.0, profile.FlankAngleDegrees, 10);
        Assert.Equal(0.015, profile.MaximumInternalRadiusInches!.Value, 10);
        Assert.Equal(0.010, profile.MaximumEdgeBreakInches!.Value, 10);
        Assert.Equal("A", profile.Metadata.Revision);
        Assert.False(profile.CanGenerateProductionGeometry);
    }

    [Fact]
    public void EveryFixtureHasPositiveMappedSlotDimensionsAndRemainsGenerationBlocked()
    {
        Assert.All(FixtureStockPrepCatalog.All, profile =>
        {
            Assert.True(profile.SlotDepthInches > 0);
            Assert.True(profile.SlotWidthInches > 0);
            Assert.True(profile.SlotWidthToleranceInches > 0);
            Assert.True(profile.SlotWidthToleranceInches < profile.SlotWidthInches);
            Assert.False(profile.CanGenerateProductionGeometry);
        });
    }

    [Fact]
    public void Catalog_ContainsOnlyTheThreeSelectedFixtureModels()
    {
        Assert.Equal(
            new[] { "D115", "D22", "D05125" },
            FixtureStockPrepCatalog.All.Select(p => p.FixtureModel).ToArray());
    }

    [Fact]
    public void Catalog_LookupIsCaseInsensitive()
    {
        var upper = FixtureStockPrepCatalog.GetByFixtureModel("D22");
        var lower = FixtureStockPrepCatalog.GetByFixtureModel("d22");

        Assert.Same(upper, lower);
    }

    [Fact]
    public void Catalog_RejectsUnknownFixtureModel()
    {
        Assert.Throws<KeyNotFoundException>(() =>
            FixtureStockPrepCatalog.GetByFixtureModel("V562"));
    }
}
