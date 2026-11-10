# TapDR 5th-Axis Stock Generator

Mastercam 2027 NET-Hook add-in for generating native editable stock-prep solids for the selected 5th Axis vise and dovetail-fixture families.

## Fixed hardware scope

All six selected items use the user's confirmed 96 mm mounting system:

- Vises: V75100X, original V562, V6105.
- Dovetail fixtures: R96 D115, R96 D22, R96 D05125.
- V75100X stack: V75100X vise, user's R96-V75100 adapter, and the 96 mm mounting interface are separate components.

Do not substitute V562X or R96-V75100X-1.

Vise stock prep and fixture stock prep are separate profile families. Fixture stock preparation is not the 96 mm RockLock mounting interface.

## Vise stock prep — DSP-V

The 5th Axis DSP-V Rev A drawing applies to all 5th Axis vises. It specifies 0.080 in dovetail depth C, two 0.250 in minimum locating flats B, 45-degree flanks, and a 0.010 in maximum edge break at the dovetail point. The 75% dovetail-width note is guidance, not an absolute rule.

The core generator creates the male dovetail tongue within the entered raw-stock envelope. It calculates flank expansion from depth and angle, applies the requested toe edge breaks, and rejects profiles where a flank consumes a locating flat or where the edge break cannot fit.

The exact drawing B cutter/programming datum still needs mapping. The 75% width advisory is therefore not yet evaluated against a guessed dimension. The profile remains development geometry, not production-authorized.

## Fixture stock prep — DSP-D115 / DSP-D22 / DSP-D05125

The fixture catalog records the separate workpiece-blank profiles and the drawing callouts currently confirmed:

| Profile | Slot depth | Gauge pin |
|---|---:|---:|
| DSP-D115 | 0.080 in | 0.0625 in |
| DSP-D22 | 0.150 in | 0.1250 in |
| DSP-D05125 | 0.080 in | 0.0625 in |

The drawings also specify 45-degree flanks, 2 × R0.015 maximum inside radii, and 2 × 0.010 in edge breaks. The remaining positional/programming dimensions must be mapped to the correct coordinate views before fixture solids are enabled. Fixture profiles are cataloged but not yet generated.

## Current source status

- [x] Fixed six-model hardware scope and separate 96 mm interface.
- [x] Vise input model, inch/mm conversion, and hard-limit checks.
- [x] Parametric DSP-V male-dovetail cross-section with applied toe edge breaks.
- [x] Core regression tests for vise coordinates, edge breaks, dimensions, angles, unit conversion, and fixture catalog values.
- [x] GitHub Actions workflow for core tests (a passing run has not yet been verified).
- [x] WinForms NET-Hook entry point, vise input UI, and live section/prism preview.
- [x] Mastercam native solid extrusion adapter source.
- [x] Mastercam function table.
- [x] One-click `Build-and-Install.bat` and transactional installer source.
- [ ] Fixture coordinate models and fixture generation UI.
- [ ] Exact DSP-V B-datum mapping and complete production validation.
- [ ] Mastercam 2027 Windows compile and in-host geometry verification.
- [ ] Toolbar integration.

## Build configuration

The geometry core targets .NET 10. The Mastercam adapter targets `net10.0-windows` and references the local Mastercam installation under `C:\Program Files\Mastercam 2027`. Mastercam 2027's .NET 10 transition is documented by Mastercam staff [here](https://www.emastercam.com/forums/topic/114704-mastercam-2027-moves-to-net-10/).

## Windows build and installation

With Mastercam 2027 and the .NET 10 SDK installed at the default location, double-click `Build-and-Install.bat`. It runs the core tests, builds the NET-Hook against the local Mastercam assemblies, stages the output, backs up an existing installation, and then installs the add-in. It stops before replacing the existing installation if tests or compilation fail.

The installer and adapter are source-authored but have **not** been compiled or run on a Windows machine with Mastercam 2027 in this session. No working installer or successful Mastercam runtime test is claimed.
