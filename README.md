# TapDR 5th-Axis Stock Generator

A Mastercam 2027 add-in for creating native, editable 5th Axis vise stock-prep and fixture-mounting geometry.

## Scope

- Mastercam 2027 add-in; the standalone geometry core currently targets .NET 10. The exact Mastercam adapter target framework must be checked against the installed 2027 assemblies before project wiring.
- Native editable solid geometry in the active Mastercam part file. Mesh output is not the primary workflow.
- Separate stock-preparation families for **vises** and **dovetail fixtures**. D115/D22/D05125 are the fixtures; DSP-D115/DSP-D22/DSP-D05125 are drawings for preparing the workpiece blanks those fixtures hold. The 96 mm RockLock interface is a separate hardware mounting interface, not a dovetail-stock profile.
- Revision-controlled, manufacturer-drawing-backed profiles. A profile without verified dimensions and revision metadata is blocked from production generation.

## Supported hardware family

- Vises: V75100X, original V562, V6105.
- Fixture clamps: R96-D115, R96-D22, R96-D05125.
- Mounting interface: 96 mm.

The V75100X assembly is represented as three separate items: V75100X vise, the user's specified R96-V75100 adapter, and the 96 mm mounting interface. Do not silently substitute R96-V75100X-1 or V562X.

## Stock-preparation families

### Vise blanks — DSP-V

The manufacturer DSP-V drawing specifies:
- 0.080 in nominal dovetail depth.
- Two 0.250 in minimum locating flats.
- 45 degree flanks.
- 0.010 in maximum edge break at the dovetail point.
- 75% dovetail-width guidance is a recommendation, not an absolute rejection rule.

The current validator checks the explicit flat/depth/angle/edge-break input limits. The exact drawing-defined dovetail-width datum must be mapped before applying the 75% recommendation.

A first eight-vertex parametric underside-relief cross-section generator has been added. It calculates flank undercut from depth and angle and rejects overlapping flanks. This is a geometry primitive only: it is not yet a fully drawing-conformed DSP-V profile, does not yet apply the edge-break geometry, and is not cleared for production machining. See [Vise Cross-Section Design](docs/geometry/Vise-Cross-Section-Design.md).

### Dovetail-fixture blanks — DSP-D115 / DSP-D22 / DSP-D05125

Each fixture has its own workpiece stock-prep drawing. The currently inspected stock-prep sheets identify revision A for all three profiles. These drawing revisions are separate from the D115/D22/D05125 fixture hardware drawing revisions. Confirmed shared drawing features are 45-degree flanks, 2× R0.015 maximum inside radii, and 2× 0.010-inch edge breaks. Profile-specific depths and only positively mapped gauge-pin data are recorded in the fixture catalog. Additional centerline/programming dimensions remain drawing annotations until their exact geometric association is mapped from the drawing views. D05125 gauge-pin diameter is intentionally unspecified pending that mapping. These fixture profiles remain blocked from production geometry until coordinate mapping is complete.

## Current implementation status

- [x] Project scope and hardware boundaries recorded.
- [x] Manufacturer DSP-V dimensions and current validation rules recorded.
- [x] Initial standalone geometry core: unit conversion, parameter model, hard-limit validation, and drawing provenance metadata.
- [x] First parametric vise underside-relief cross-section generator.
- [ ] Automated geometry tests: closure, winding, area, self-intersection, and dimensional cases.
- [ ] Map exact DSP-V drawing datums and apply the specified edge break.
- [x] Initial fixture stock-prep catalog with profile-specific depth, angle, radius, edge-break, and drawing metadata.
- [ ] Fixture drawing-to-coordinate mapping and geometry tests.
- [ ] Mastercam 2027 native geometry adapter.
- [ ] In-Mastercam preview and parameter UI.
- [ ] Installer and toolbar integration.

**No Mastercam runtime success is claimed yet.** The documented .NET API includes ChainManager.ChainGeometry, SolidOperations.SolidExtrudeFromChain, and solid Boolean methods. Their exact 2027 references and behavior still require a Windows compile/link and Mastercam runtime check.

## Build target

The independent geometry core targets .NET 10. The Mastercam add-in target framework remains an explicit verification gate; do not infer it from forum posts or another Mastercam release. The adapter will bind to the assemblies installed with Mastercam 2027.
