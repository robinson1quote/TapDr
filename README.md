# TapDR 5th-Axis Stock Generator

A Mastercam 2027 add-in for creating native, editable 5th Axis vise stock-prep and fixture-mounting geometry.

## Scope

- Mastercam 2027 / .NET 10 add-in.
- Native editable solid geometry in the active Mastercam part file. Mesh output is not the primary workflow.
- Separate geometry families for **vise stock preparation** and **fixture mounting dovetails**. They must never share an assumed profile.
- Revision-controlled, manufacturer-drawing-backed profiles. A profile without verified dimensions and revision metadata is blocked from production generation.

## Supported hardware family

The hardware catalogue is deliberately limited to the selected 5th Axis models:
- Vises: V75100X, original V562, V6105.
- Fixture clamps: R96-D115, R96-D22, R96-D05125.
- Mounting interface: 96 mm.

The V75100X assembly is represented as three separate items: V75100X vise, the specified R96-V75100 adapter, and the 96 mm mounting interface. Do not silently substitute R96-V75100X-1 or V562X.

## First geometry family: vise stock preparation

The manufacturer DSP-V drawing specifies:
- 0.080 in nominal dovetail depth.
- Two 0.250 in minimum locating flats.
- 45 degree flanks.
- 0.010 in maximum edge break at the dovetail point.
- 75% dovetail-width guidance is a recommendation, not an absolute rejection rule.

The core geometry engine will report the width ratio as a validation advisory. It will not silently change user dimensions to force the recommendation.

## Current implementation status

- [x] Project scope and hardware boundaries recorded.
- [x] Manufacturer DSP-V dimensions and validation rules recorded.
- [ ] Pure geometry core implementation and unit tests.
- [ ] Mastercam 2027 native geometry adapter.
- [ ] In-Mastercam preview and parameter UI.
- [ ] Revisioned fixture-profile catalog from exact manufacturer drawings.
- [ ] Installer and toolbar integration.

**No Mastercam runtime success is claimed yet.** The documented .NET API includes ChainManager.ChainGeometry, SolidOperations.SolidExtrudeFromChain, and solid Boolean methods. Their exact 2027 references and behavior still require a Windows compile/link and Mastercam runtime check.

## Build target

Mastercam 2027 uses .NET 10 for add-ins. The independent geometry core targets .NET 10; the Mastercam adapter will bind to the assemblies installed with Mastercam 2027.