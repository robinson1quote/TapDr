# Mastercam .NET SDK Integration Notes

## Documented native geometry path

The supplied `NETHook3_0(1).xml` reference documents the following managed API members:

1. `Mastercam.Curves.LineGeometry(Point3D, Point3D)` creates a line entity. The line must be committed to the Mastercam geometry database.
2. `Mastercam.Database.ChainManager.ChainGeometry(Geometry[])` creates chains from the supplied geometry entities.
3. `Mastercam.Solids.SolidOperations.SolidExtrudeFromChain(Chain, SolidExtrudeParams, string)` creates a solid body from a valid profile chain.
4. `Mastercam.Support.SearchManager.CreateSolidBooleanRemove(Geometry[], string)` subtracts tool bodies from the first target body in the array.

## Extrusion parameter fields documented

`SolidExtrudeParams` exposes fields for `creationType`, `entityID`, `distMethod`, `distance`, `direction`, `local`, `reverseDir`, `bothDir`, `splitDraft`, `draft`, `thinwall`, `draftEnds`, and `flipOutward`.

## Explicit verification gates

- The XML reference copy documents the methods and fields, but the `SolidExtrudeParams` default constructor was not found in the supplied XML. Do not write adapter code assuming a constructor or initialization syntax until the installed Mastercam 2027 assembly metadata or official sample confirms it.
- Verify the assembly versions and target framework against the user's installed Mastercam 2027 environment.
- Compile the adapter against those exact assemblies.
- Run a minimal test in Mastercam that commits a closed profile, chains it, extrudes it, and confirms a native editable solid exists in the part file.
- Confirm coordinate system, construction plane, extrusion vector, units, and entity cleanup behavior in the running application.

## What this does not prove

Documentation is not compile proof or runtime proof. No Mastercam build or runtime test is claimed by this repository at this stage.