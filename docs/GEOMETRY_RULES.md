# Geometry Rules and Provenance

## Family separation

Keep these profile types separate in code, UI, validation, and metadata:

- `ViseStockPrep`: machining geometry for a part blank gripped by a 5th Axis vise.
- `FixtureMountingProfile`: dovetail geometry used to mount a fixture/clamp to the 96 mm system.

No profile dimensions may be copied between these families just because both use a dovetail shape.

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

## Fixture profiles

Fixture clamp profiles for R96-D115, R96-D22, and R96-D05125 require their own drawing-derived records. Do not mark them production-ready until the exact drawing and revision are attached to each profile record.