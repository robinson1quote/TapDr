namespace TapDR.Dovetail.Core;

public enum ProfileVerificationStatus
{
    Unverified,
    DrawingVerified,
    RuntimeValidated
}

public sealed record ManufacturerProfileMetadata
{
    public required string ProfileId { get; init; }
    public required string Manufacturer { get; init; }
    public required string DrawingNumber { get; init; }
    public required string Revision { get; init; }
    public required string SourceUrl { get; init; }
    public required LengthUnit DrawingUnit { get; init; }
    public ProfileVerificationStatus VerificationStatus { get; init; }

    public bool IsProductionEligible =>
        VerificationStatus == ProfileVerificationStatus.RuntimeValidated;
}