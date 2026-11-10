# Vise cross-section geometry — DSP-V drawing interpretation

## Manufacturer drawing inputs

The common 5th Axis DSP-V Rev A drawing shows:
- Overall stock width A.
- Two 0.250 in minimum locating-flat lengths B, one at each outside edge.
- 0.080 in dovetail depth C.
- 45-degree dovetail cut surfaces.
- 0.010 in maximum edge break at the dovetail point.

The drawing states it applies to all 5th Axis vises. It identifies C as the most critical dimension and says A/B change with stock size. Its 75% dovetail-width guidance is explicitly a recommendation, not an absolute rule.

## Coordinate convention and first parametric model

The generator represents the stock inside its raw rectangular envelope:
- X = 0 to stock width W.
- Y = 0 at the lowest central dovetail locating surface.
- The two outside underside locating flats lie at Y = depth d.
- The stock top lies at Y = stock height H.
- The tongue therefore remains inside the entered raw-stock height; the generator does not add material below the blank.

Inputs:
- W: stock width.
- H: raw stock height.
- fL / fR: left/right outside locating-flat lengths.
- d: dovetail depth C.
- theta: flank angle measured from the horizontal underside.
- u: horizontal expansion of the toe on each side.

The initial geometric relationship is:

`u = d / tan(theta)`

The outside flank start locations are:

`xL = fL`

`xR = W - fR`

The lower toe endpoints are:

`xToeL = fL - u`

`xToeR = W - fR + u`

The eight-point boundary is:

`(0,d), (fL,d), (fL-u,0), (W-fR+u,0), (W-fR,d), (W,d), (W,H), (0,H)`

The generator rejects the profile if either flank consumes its locating flat, if the toe has no usable width, or if upstream input validation fails.

For a 2.000 in wide blank with 0.250 in locating flats, 0.080 in depth, and 45-degree flanks, this model calculates:
- outside flank-start span: 1.500 in.
- toe expansion: 0.080 in per side.
- toe width: 1.660 in.

Those calculated widths are consequences of the parametric model, not separately called-out manufacturer dimensions.

## Remaining release gates

- Map the drawing's B cutter/programming datum exactly; do not assume a generated span is a substitute for that toolpath datum.
- Apply the 0.010 in maximum edge break as actual profile geometry.
- Confirm corner relief and contact surfaces against the manufacturer's details and actual cutter geometry.
- Validate the 75% recommendation against the correctly mapped dovetail-width definition.
- Add polygon self-intersection and solid validity tests.
- Build and verify the native B-rep inside Mastercam 2027.

The current cross-section is development geometry only. It is not production-authorized.
