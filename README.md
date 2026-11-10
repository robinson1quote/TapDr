# TapDR 5th-Axis Stock Generator

Mastercam 2027 NET-Hook add-in for creating native editable stock-prep solids for the selected 5th Axis vise and dovetail-fixture families.

## Fixed hardware scope

All six selected items use the user's confirmed 96 mm mounting system:

- Vises: V75100X, original V562, V6105.
- Dovetail fixtures: R96 D115, R96 D22, R96 D05125.
- V75100X stack: V75100X vise, user's R96-V75100 adapter, and the 96 mm mounting interface are separate components.

Do not substitute V562X or R96-V75100X-1.

Vise stock prep and fixture stock prep are separate profile families. Fixture stock preparation is not the 96 mm RockLock mounting interface.

## Vise stock prep — DSP-V

The 5th Axis DSP-V Rev A drawing applies to all 5th Axis vises. It specifies 0.080 in dovetail depth C, two 0.250 in minimum outside locating flats B, 45-degree flanks, and a 0.010 in maximum edge break at the dovetail point. The 75% dovetail-width note is guidance, not an absolute rule.

The core creates the male dovetail tongue inside the entered raw-stock envelope. It calculates the flank expansion from depth and angle, applies both toe edge breaks, and computes the actual locating-face width:

`D = stock width - left flat - right flat + 2 × flank expansion`

The 75% advisory is checked against D/stock width. It warns only; it does not reject or silently resize the profile.

Mastercam stock coordinates are Z-up: blank length runs along X, width along Y, and height along Z. The native adapter source constructs the section in the YZ plane and extrudes along X. The preview is a visual section/prism preview, not a rendering of a Mastercam B-rep.

The DSP-V drawing's separate instruction to program the dovetail cutter from the part centerline is a cutter-path note. This add-in creates stock geometry; it does not create a cutter toolpath.

The current section generator and native-solid adapter are development source, not production-authorized. Full contact/relief checks and in-Mastercam geometry verification remain open.

## Fixture stock prep — DSP-D115 / DSP-D22 / DSP-D05125

The fixture catalog records the separate workpiece-blank profiles and confirmed drawing callouts:

| Profile | Slot depth | Gauge pin |
|---|---:|---:|
| DSP-D115 | 0.080 in | 0.0625 in |
| DSP-D22 | 0.150 in | 0.1250 in |
| DSP-D05125 | 0.080 in | 0.0625 in |

The drawings also specify 45-degree flanks, 2 × R0.015 maximum inside radii, and 2 × 0.010 in edge breaks. Positional/programming dimensions still need exact mapping to their drawing views before fixture geometry can be enabled. The fixture catalog remains separate from the 96 mm mounting interface.

## Current source status

- [x] Fixed six-model hardware scope and separate 96 mm interface.
- [x] Vise input model, inch/mm conversion, hard-limit validation.
- [x] Parametric DSP-V male-dovetail cross-section with actual toe edge-break geometry.
- [x] DSP-V 75% width advisory based on generated locating-face width.
- [x] Core regression tests for section coordinates, edge breaks, dimensions, angles, units, ratio warning, and fixture catalog values.
- [x] GitHub Actions workflow for core tests (passing result not yet verified).
- [x] WinForms NET-Hook entry point, all-six-model selector, vise input UI, and live prism preview (fixture generation disabled pending coordinate mapping).
- [x] Mastercam solid extrusion adapter source; section in YZ, extrusion along X, Z-up stock height, centering on machine axes.
- [x] Mastercam NET-Hook function table.
- [x] One-click `Build-and-Install.bat` and transactional PowerShell installer source.
- [ ] Fixture coordinate models and fixture generation UI.
- [ ] Final DSP-V contact/relief review and in-Mastercam B-rep verification.
- [ ] Mastercam 2027 Windows compile and host test.
- [ ] Toolbar integration.

## Build configuration

The geometry core targets .NET 10. The Mastercam adapter targets `net10.0-windows` and references the local Mastercam installation under `C:\Program Files\Mastercam 2027`. Mastercam's 2027 .NET 10 move is documented by Mastercam staff [here](https://www.emastercam.com/forums/topic/114704-mastercam-2027-moves-to-net-10/).

## Windows build and installation

With Mastercam 2027 and the .NET 10 SDK installed at the default location, double-click `Build-and-Install.bat`. It runs the core tests, builds the NET-Hook against the local Mastercam assemblies, stages the output, backs up an existing installation, and installs the add-in. It stops before replacing the existing installation if tests or compilation fail.

The installer and adapter source have **not** yet been compiled or run on a Windows machine with Mastercam 2027 in this session. No verified installer or successful Mastercam runtime test is claimed.
