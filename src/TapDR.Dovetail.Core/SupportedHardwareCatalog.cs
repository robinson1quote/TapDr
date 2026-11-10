namespace TapDR.Dovetail.Core;

public enum HardwareCategory
{
    Vise,
    DovetailFixture
}

public sealed record HardwareDefinition
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required HardwareCategory Category { get; init; }
    public required double MountingInterfaceMillimeters { get; init; }
    public required string StockPrepProfileId { get; init; }
    public required IReadOnlyList<string> AssemblyComponents { get; init; }
    public required bool StockPrepGenerationEnabled { get; init; }

    public string AssemblyDescription => string.Join(" + ", AssemblyComponents);
    public override string ToString() => DisplayName;
}

/// <summary>
/// The exact six hardware models selected by the user. All use the 96 mm
/// mounting interface. Fixture blank-prep profiles stay separately gated
/// until their manufacturer drawing dimensions are mapped to coordinates.
/// </summary>
public static class SupportedHardwareCatalog
{
    public const double MountingInterfaceMillimeters = 96.0;

    private static readonly HardwareDefinition[] Items =
    [
        new()
        {
            Id = "V75100X",
            DisplayName = "V75100X — R96-V75100 adapter / 96 mm",
            Category = HardwareCategory.Vise,
            MountingInterfaceMillimeters = MountingInterfaceMillimeters,
            StockPrepProfileId = "DSP-V",
            AssemblyComponents = Array.AsReadOnly(new[]
            {
                "V75100X vise",
                "R96-V75100 adapter",
                "96 mm mounting interface"
            }),
            StockPrepGenerationEnabled = true
        },
        new()
        {
            Id = "V562",
            DisplayName = "V562 original — 96 mm",
            Category = HardwareCategory.Vise,
            MountingInterfaceMillimeters = MountingInterfaceMillimeters,
            StockPrepProfileId = "DSP-V",
            AssemblyComponents = Array.AsReadOnly(new[]
            {
                "V562 original vise",
                "96 mm mounting interface"
            }),
            StockPrepGenerationEnabled = true
        },
        new()
        {
            Id = "V6105",
            DisplayName = "V6105 — 96 mm",
            Category = HardwareCategory.Vise,
            MountingInterfaceMillimeters = MountingInterfaceMillimeters,
            StockPrepProfileId = "DSP-V",
            AssemblyComponents = Array.AsReadOnly(new[]
            {
                "V6105 vise",
                "96 mm mounting interface"
            }),
            StockPrepGenerationEnabled = true
        },
        new()
        {
            Id = "R96-D115",
            DisplayName = "R96 D115 — fixture / 96 mm",
            Category = HardwareCategory.DovetailFixture,
            MountingInterfaceMillimeters = MountingInterfaceMillimeters,
            StockPrepProfileId = "DSP-D115",
            AssemblyComponents = Array.AsReadOnly(new[]
            {
                "R96 D115 fixture",
                "96 mm mounting interface"
            }),
            StockPrepGenerationEnabled = false
        },
        new()
        {
            Id = "R96-D22",
            DisplayName = "R96 D22 — fixture / 96 mm",
            Category = HardwareCategory.DovetailFixture,
            MountingInterfaceMillimeters = MountingInterfaceMillimeters,
            StockPrepProfileId = "DSP-D22",
            AssemblyComponents = Array.AsReadOnly(new[]
            {
                "R96 D22 fixture",
                "96 mm mounting interface"
            }),
            StockPrepGenerationEnabled = false
        },
        new()
        {
            Id = "R96-D05125",
            DisplayName = "R96 D05125 — fixture / 96 mm",
            Category = HardwareCategory.DovetailFixture,
            MountingInterfaceMillimeters = MountingInterfaceMillimeters,
            StockPrepProfileId = "DSP-D05125",
            AssemblyComponents = Array.AsReadOnly(new[]
            {
                "R96 D05125 fixture",
                "96 mm mounting interface"
            }),
            StockPrepGenerationEnabled = false
        }
    ];

    public static IReadOnlyList<HardwareDefinition> All => Array.AsReadOnly(Items);

    public static HardwareDefinition Get(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return Items.SingleOrDefault(item =>
            string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase))
            ?? throw new KeyNotFoundException($"Unsupported 5th Axis hardware model '{id}'.");
    }
}
