# TapDR 5th-Axis Stock Generator

A Mastercam 2027 add-in for creating native, editable 5th Axis vise stock-prep and fixture-mounting geometry.

## Scope

- Mastercam 2027 add-in; the standalone geometry core targets .NET 10. The exact Mastercam adapter target framework must be verified against the installed 2027 assemblies.
- Native editable solid geometry in the active Mastercam part file. Mesh output is not the primary workflow.
- Separate stock-preparation families for vises and dovetail fixtures. D115/D22/D05125 are fixtures; DSP-D115/DSP-D22/DSP-D05125 are drawings for preparing their workpiece blanks. The 96 mm RockLock mounting interface is separate from those dovetail stock profiles.
- The six selected hardware models remain fixed: V75100X, original V562, V6105, R96 D115, R96 D22 and R96 D05125. Every item uses the user's confirmed 96 mm mounting system.
- The V75100X assembly keeps the V75100X vise, the specified R96-V75100 adapter, and the 96 mm mounting interface as separate components. Do not substitute R96-V75100X-1 or V562X.

## Vise stock prep — DSP-V

The DSP-V Rev A drawing applies to all 5th Axis vises. It defines 0.080 in depth C, two 0.250 in minimum locating flats B, and 0.010 in maximum edge break at the dovetail point. Dimension C is called out as most critical. The drawing's 75% dovetail-width guidance is advisory, not an absolute rejection rule.

The cross-section is a **male dovetail tongue extending below the stock locating plane**, not a female pocket. The core generator now reflects that orientation. It computes horizontal toe expansion from depth and flank angle, keeps the toe within the stock width, returns a counter-clockwise eight-vertex boundary, and checks the resulting coordinates and signed area.

The core includes automated tests for the cross-section coordinates, flank calculation, invalid dimensions/angles, toe overhang, and metric units. These tests are committed to GitHub Actions; no successful CI run is claimed until its result is checked.

The profile is still **not production-authorized**. Actual edge-break geometry, full cutter/contact interpretation, drawing-defined 75% width validation, additional polygon/solid validity tests, and in-Mastercam verification remain outstanding.

## Fixture stock prep — DSP-D115 / DSP-D22 / DSP-D05125

Each fixture has its own workpiece stock-prep drawing. Drawing revisions are tracked separately from the fixture hardware drawing revisions. Only callouts whose geometric meanings have been positively mapped may feed coordinate generation. D05125 gauge-pin diameter remains unspecified until its drawing callout is mapped. Fixture profiles remain blocked from production geometry until coordinate mapping and geometry tests are complete.

## Current implementation status

- [x] Selected hardware and 96 mm interface scope recorded.
- [x] Vise parameter model and hard-limit validation.
- [x] First male-tongue vise cross-section primitive.
- [x] Initial automated geometry test suite and CI workflow.
- [x] Fixture stock-prep metadata catalog.
- [ ] Production-conformed DSP-V edge-break/contact geometry.
- [ ] Production coordinate mapping and tests for each fixture profile.
- [ ] Mastercam 2027 native geometry adapter.
- [ ] In-Mastercam parameter UI and live preview.
- [x] One-click `Build-and-Install.bat` and transactional PowerShell installer authored (not yet exercised on a Windows Mastercam 2027 seat).\n- [ ] Mastercam toolbar integration.

**No Mastercam runtime success is claimed.** The documented .NET reference includes ChainManager.ChainGeometry, SolidOperations.SolidExtrudeFromChain, and solid Boolean operations. Exact 2027 assembly compatibility and execution remain unverified.

## Build target

The independent geometry core targets .NET 10. The Mastercam adapter's target framework must be selected from the actual assemblies installed with Mastercam 2027, not inferred from another release or forum post.


## Windows build and installation

With Mastercam 2027 and the .NET 10 SDK installed at the standard location, double-click `Build-and-Install.bat`. The script runs the geometry tests first, builds the NET-Hook against the local Mastercam assemblies, stages the files, backs up an existing installation, and then swaps in the new add-in. It stops before installation if tests or compilation fail.

The script and project are source-authored but have not yet been executed on a Windows machine with Mastercam 2027 installed. Do not treat the installer as a verified binary release.
