# Vise cross-section geometry — DSP-V drawing interpretation

## Manufacturer drawing inputs

The common 5th Axis DSP-V Rev A drawing shows:
- Overall stock width A.
- Two 0.250 in minimum locating-flat lengths B, one at each outside edge.
- 0.080 in dovetail depth C.
- 45-degree dovetail cut surfaces.
- 0.010 in maximum edge break at the dovetail point.

The drawing states it applies to all 5th Axis vises. It identifies C as the most critical dimension and says A/B change with stock size. Its 75% dovetail-width guidance is explicitly a recommendation, not an absolute rule. The B callout is the horizontal minimum flat length from each outside stock edge to the start of its dovetail flank. The separate drawing note tells the programmer to locate the cutter from the part centerline using B; that CNC cutter-location instruction is not part of this solid generator.

## Coordinate convention and parametric section

In Mastercam machine coordinates the profile width maps to Y, stock height maps to vertical Z, and stock length extrudes along X. This keeps the blank in the normal Z-up machine orientation.

The generator represents the stock inside its raw rectangular envelope:
- X = 0 to stock width W.
- Y = 0 at the lowest central dovetail locating surface.
- The two outside underside flats lie at Y = depth d.
- The stock top lies at Y = stock height H.
- The complete section stays inside X=[0,W], Y=[0,H]; the generator does not add stock outside the entered blank.

Inputs:
- W: stock width.
- H: raw stock height.
- fL / fR: left/right outside locating-flat lengths.
- d: dovetail depth C.
- theta: flank angle measured from the horizontal underside.
- b: actual toe edge break, limited to 0.010 in.
- u: horizontal expansion of the toe on each side.

The flank relationship is:

`u = d / tan(theta)`

The full dovetail locating-face width is `D = W - fL - fR + 2u`. The DSP-V 75% advisory is evaluated as `D / W`; it generates a warning only and never rejects or silently alters the part.

The unchamfered flank starts are `xL=fL` and `xR=W-fR`; the toe endpoints are `xToeL=fL-u` and `xToeR=W-fR+u`.

For nonzero b, each of the two lower toe corners is chamfered by distance b measured along both adjacent profile edges. The toe bevel must fit within both flank segments and the locating face. The default 0.010 in edge break is therefore part of the generated section, not merely a displayed parameter. Setting b to zero intentionally leaves sharp toe corners.

For a 2.000 in wide blank with 0.250 in locating flats, 0.080 in depth, 45-degree flanks, and 0.010 in edge break, the generator creates a ten-point closed boundary. Before the two corner chamfers, the toe width is 1.660 in. These calculated widths are consequences of the parametric model, not separately called-out manufacturer dimensions.

## Geometry protections and tests authored

- Reject unsupported units, non-finite or non-positive dimensions, depth equal to or greater than stock height, invalid angles, excessive edge breaks, consumed locating flats, and edge breaks too large for the available toe geometry.
- Generate a counter-clockwise, read-only boundary.
- Regression tests assert the ten-point edge-broken profile, the sharp-edge zero-break option, depth/angle relationship, stock envelope, orientation, and rejection cases.

The test source is committed for exact profile points, actual dovetail width and the 75% advisory. No passing test run is claimed until the GitHub workflow reports success.

## Remaining production-release gates

- Keep the DSP-V cutter-centerline instruction separate from stock-solid construction; the generator does not create a cutter path.
- Verify the exact edge-break interpretation and all contact surfaces against the manufacturer's drawing and cutter geometry.
- Add polygon self-intersection and native-solid validity tests.
- Build and verify the resulting B-rep inside Mastercam 2027.

The current cross-section remains development geometry and is not production-authorized.
