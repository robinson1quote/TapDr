using TapDR.Dovetail.Core;
using Xunit;

namespace TapDR.Dovetail.Core.Tests;

public sealed class SupportedHardwareCatalogTests
{
    [Fact]
    public void Catalog_ContainsExactlyTheSixSelectedHardwareModels()
    {
        Assert.Equal(
            new[] { "V75100X", "V562", "V6105", "R96-D115", "R96-D22", "R96-D05125" },
            SupportedHardwareCatalog.All.Select(item => item.Id));
    }

    [Fact]
    public void EverySupportedModelUsesTheConfirmedNinetySixMillimeterInterface()
    {
        Assert.All(
            SupportedHardwareCatalog.All,
            item => Assert.Equal(96.0, item.MountingInterfaceMillimeters));
    }

    [Fact]
    public void V75100XKeepsTheUserSpecifiedAdapterAsASeparateAssemblyComponent()
    {
        var model = SupportedHardwareCatalog.Get("V75100X");

        Assert.Equal(HardwareCategory.Vise, model.Category);
        Assert.Equal("DSP-V", model.StockPrepProfileId);
        Assert.Equal(
            new[] { "V75100X vise", "R96-V75100 adapter", "96 mm mounting interface" },
            model.AssemblyComponents);
        Assert.Contains("V75100X vise", model.AssemblyDescription);
        Assert.Contains("R96-V75100 adapter", model.AssemblyDescription);
        Assert.Contains("96 mm mounting interface", model.AssemblyDescription);
        Assert.DoesNotContain("R96-V75100X-1", model.AssemblyDescription);
    }

    [Fact]
    public void OriginalV562IsSupportedWithoutSubstitutingV562X()
    {
        var model = SupportedHardwareCatalog.Get("V562");

        Assert.Equal("V562", model.Id);
        Assert.Contains("original", model.DisplayName, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("V562X", model.DisplayName);
        Assert.True(model.StockPrepGenerationEnabled);
    }

    [Theory]
    [InlineData("R96-D115", "DSP-D115")]
    [InlineData("R96-D22", "DSP-D22")]
    [InlineData("R96-D05125", "DSP-D05125")]
    public void FixtureModelsRemainMappedToSeparateDisabledProfiles(
        string hardwareId, string profileId)
    {
        var model = SupportedHardwareCatalog.Get(hardwareId);

        Assert.Equal(HardwareCategory.DovetailFixture, model.Category);
        Assert.Equal(profileId, model.StockPrepProfileId);
        Assert.False(model.StockPrepGenerationEnabled);
    }

    [Fact]
    public void AllThreeVisesUseDSPVAndAllThreeFixturesUseDistinctProfiles()
    {
        var viseModels = SupportedHardwareCatalog.All
            .Where(item => item.Category == HardwareCategory.Vise)
            .ToArray();
        var fixtureModels = SupportedHardwareCatalog.All
            .Where(item => item.Category == HardwareCategory.DovetailFixture)
            .ToArray();

        Assert.Equal(3, viseModels.Length);
        Assert.All(viseModels, item => Assert.Equal("DSP-V", item.StockPrepProfileId));

        Assert.Equal(3, fixtureModels.Length);
        Assert.Equal(
            new[] { "DSP-D115", "DSP-D22", "DSP-D05125" },
            fixtureModels.Select(item => item.StockPrepProfileId));
        Assert.All(fixtureModels, item => Assert.False(item.StockPrepGenerationEnabled));
    }

    [Fact]
    public void CatalogRejectsUnlistedSubstituteModels()
    {
        Assert.Throws<KeyNotFoundException>(() => SupportedHardwareCatalog.Get("V562X"));
        Assert.Throws<KeyNotFoundException>(() => SupportedHardwareCatalog.Get("R96-V75100X-1"));
    }
}
