# Vise cross-section geometry — DSP-V drawing interpretation

## Drawing source and datum model

The common 5th Axis DSP-V Rev A drawing labels:
- A: overall stock width.
- B: two minimum 0.250 in locating-flat lengths, one at each stock edge.
- C: 0.080 in dovetail depth.
- Maximum edge break: 0.010 in at the dovetail point.

It states that the DSP-V preparation works with all 5th Axis vises. It also says C is the most critical dimension and that A/B vary with stock size. The 75% dovetail-width note is a recommendation, not an absolute rule.

## Cross-section

The profile is a **male dovetail tongue** projecting below the stock's locating plane. It is not a trapezoidal pocket cut upward into the blank.

Use the locating plane as Y=0 and positive Y into the stock. The dovetail toe extends in negative Y by C.

Let:
- W = stock width.
- fL / fR = left/right locating-flat lengths.
- d = dovetail depth C.
- theta = flank angle measured from the locating plane.
- u = horizontal toe expansion on each side.

The initial parametric relationship is:

\[
u = \frac{d}{\tan(\theta)}
\]

The two upper flank endpoints are:

\[
x_{mouth,L}=f_L,\quad x_{mouth,R}=W-f_R
\]

The lower toe endpoints are:

\[
x_{toe,L}=f_L-u,\quad x_{toe,R}=W-f_R+u
\]

The generator requires each toe endpoint to remain within the stock's outside width. This prevents the dovetail from overhanging beyond the locating flats. The eight points are ordered counter-clockwise, starting at the lower-left stock corner and walking across the underside, up the right side, and back across the top.

For a symmetric 2.000 in wide blank using 0.250 in locating flats, 0.080 in depth, and 45-degree flanks, the generated cross-section has:
- mouth width = 1.500 in.
- toe expansion = 0.080 in per side.
- toe width = 1.660 in.
- toe depth = 0.080 in below the locating plane.

These are geometry calculations from those inputs, not additional manufacturer-specified dimensions.

## Implementation status

The core now generates this male-tongue orientation and checks finite coordinates, flank fit, and positive polygon area. Automated tests assert the point coordinates, depth/angle calculation, metric-unit behavior, invalid dimensions, and toe-overhang rejection.

## Still blocked from production

- Apply the specified 0.010 in maximum edge break as actual geometry at the dovetail point.
- Verify the detailed contact/relief interpretation against all profile views and the actual cutter geometry before releasing a machining profile.
- Check the drawing's 75% width recommendation using the drawing-defined dovetail width.
- Add polygon simplicity/solid validity tests and native Mastercam solid tests.
- Verify the resulting B-rep in Mastercam 2027.

The generated outline remains a development geometry primitive; it is not production-authorized.
