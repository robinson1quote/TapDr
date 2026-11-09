# Geometry Rules and Provenance

## Family separation

Keep these profile types separate in code, UI, validation, and metadata:

- `ViseStockPrep`: machining geometry for a part blank gripped by a 5th Axis vise.
- `FixtureStockPrep`: workpiece blank preparation for a D115, D22, or D05125 dovetail fixture.
- `RockLockMountingInterface`: the separate 96 mm quick-change interface used by hardware assemblies. This is not a workpiece dovetail profile.

No profile dimensions may be copied between these families just because both use a dovetail shape. D115, D22, and D05125 are fixture hardware models; DSP-D115, DSP-D22, and DSP-D05125 are the corresponding stock-preparation drawings for the workpieces those fixtures hold.

## Vise stock prep — manufacturer DSP-V, Rev A, inch

Verified drawing constraints:

| Parameter | Value | Meaning |
|---|---:|---|
| Nominal dovetail depth | 0.080 in | Drawing's specified prep depth |
| Minimum locating flat | 0.250 in each side | Two independent minimum flat constraints |
| Flank angle | 45 deg | Dovetail flank angle shown on the profile |
| Maximum edge break | 0.010 in | Maximum edge break at the dovetail point |
| Roughing allowance | 0.003 in minimum | Leave material for finishing cutter |

The drawing's 75% stock-width statement is a recommendation, not a hard acceptance criterion. Implement it as an advisory/warning only.

## Parameter policy

- User stock length, width, and height are explicit inputs.
- Units must be explicit and conversions must be centralized.
- Manufacturer values are stored in drawing units and converted once at the geometry boundary.
- Never silently clamp a user-entered dimension to a manufacturer's recommended value.
- A failed hard constraint blocks generation and states the failed rule.
- An advisory does not block generation.
- Keep profile ID, manufacturer, drawing number, revision, source URL, source units, and verification status with every generated result.

## Dovetail-fixture workpiece stock preparation

The official DSP-D115, DSP-D22, and DSP-D05125 stock-preparation drawings are separate from the fixture hardware drawings. The three prep sheets inspected here identify revision A. Common callouts include 45-degree flanks, 2× R0.015 maximum radii, and 2× 0.010-inch edge breaks. Profile-specific slot depth and gauge-pin diameter are stored separately in `FixtureStockPrepCatalog.cs`.

The drawings also contain several centerline/programming and inspection dimensions. Those are retained as drawing references but must not be translated into profile coordinates until their exact view association is confirmed. A drawing-derived record is not yet proof of correct B-rep geometry or safe use.