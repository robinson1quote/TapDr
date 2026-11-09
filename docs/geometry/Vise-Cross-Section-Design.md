# Vise cross-section geometry — design note

## What the first geometry commit does

`ViseDovetailProfileGenerator.Generate` builds an eight-vertex closed 2D boundary for a rectangular stock blank with a dovetail relief in its underside. It accepts the stock width/height, left/right locating flats, depth and flank angle from the validated vise parameter model.

For flank angle \(\theta\) measured from the horizontal underside, the horizontal undercut per side is:

\[
u = \frac{d}{\tan(\theta)}
\]

The mouth width is:

\[
w_m = W - f_L - f_R
\]

The upper relief width is:

\[
w_t = w_m + 2u
\]

The generator rejects a profile if the two flanks overlap. It does not silently shrink the dovetail or alter the locating flats.

## Important limitations

- This is a parametric geometry primitive, not a fully drawing-conformed DSP-V profile. The DSP-V drawing's exact dimension-C relationship and point/edge-break treatment still need mapping.
- The current boundary has sharp vertices. The DSP-V edge-break limit is validated as an input but the edge-break geometry is not yet applied.
- No production-generation flag is enabled by this file. The geometry still needs automated polygon closure/orientation/self-intersection tests, then native Mastercam B-rep creation and in-app verification.
- The DSP-V 75% recommendation must be checked against the drawing-defined dovetail width once its datum is mapped; do not substitute an inferred ratio without confirming that definition.

## Basic geometry checks required before production use

1. Exactly eight finite boundary points.
2. First and last edge close by construction.
3. Positive stock width and height.
4. Left and right locating flats meet the drawing minimum.
5. Flank angle strictly between 0 and 90 degrees.
6. Both undercut flanks fit without crossing.
7. Polygon has nonzero area and no self-intersection.
8. Edge-break geometry is present and bounded by the drawing maximum.
9. Profile dimensions and unit are retained with drawing provenance.

The current code addresses the explicit input checks and flank-overlap check. The remaining checks are not claimed complete.
