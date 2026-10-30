# WBIModuleLiftingSurface prototype

The live Mk18 has since moved to the optional [ModuleParafoil attitude controller](ModuleParafoilAttitudeController.md). That guide describes the current config and SAS-off test. The root-based configuration below records the original lifting-surface comparison.

`WBIModuleLiftingSurface` derives from stock `ModuleLiftingSurface` and remains discoverable by `ModuleParafoil`. It reuses stock curves, pressure/Mach values, submerged-force blending, shielding, attach-node blocking, and lift adjusters. It owns its physics loop and does not invoke the stock physics loop. ModuleParafoil's existing deployment coefficients, canopy handoff, control ramp, stabilizers, and cut/thermal safety are unchanged.

## Configuration

| Field | Default | Meaning |
| --- | --- | --- |
| `transformName` | empty | Orientation reference only; empty/missing uses the part root |
| `followTransformRotation` | false | Capture the reference's part-relative rotation at OnStart; true explicitly follows its live rotation |
| `orientationOffset` | 0, 0, 0 | Euler degrees relative to that reference |
| `transformDir`, `transformSign` | Z, 1 | Stock normal-axis selection and sign |
| `velocitySampleOffset` | 0, 0, 0 | Airflow sample position in part-local coordinates |
| `usePartLiftOffset` | true | Use current `part.CoLOffset` |
| `liftForceOffset` | 0, 0, 0 | Part-local lift position when `usePartLiftOffset = false` |
| `usePartDragOffset` | true | Use current `part.CoPOffset` |
| `dragForceOffset` | 0, 0, 0 | Part-local drag position when `usePartDragOffset = false` |
| `debugMode` | false | Enable `[WBIModuleLiftingSurface] -` observations |
| `debugLogInterval` | 0.25 | Seconds between lines; zero logs each physics update |

Stock `deflectionLiftCoeff`, `liftingSurfaceCurve`, curve fields, `useInternalDragModel`, `omnidirectional`, `perpendicularOnly`, and attach-node settings retain their meaning. Stock `displaceVelocity` and `velocityOffset` are superseded by `velocitySampleOffset` and produce a startup warning when configured. Pressure and Mach remain part-based, even with an offset sample.

Capture happens at OnStart, which can be a packed pose. Use a dedicated unanimated reference authored in the intended orientation, or use explicit `orientationOffset`. Do not capture the deployed orientation by assuming a packed canopy transform already has it. Explicit live mode intentionally reintroduces animation-driven orientation. Sampling and force positions remain part-local in both modes.

## Mk18 test

The live `C:\KSPDev\GameData\WildBlueIndustries\00WildBlueCore\Parts\Aero\wbiMk18Parachute\wbiMk18Parachute.cfg` uses the prototype only for its primary wing. It starts with root orientation, root sampling, stock CoL/CoP positions, coefficient 28, and prototype logging enabled at 0.25 seconds. The unused stock-wing `dragAtMinAoA`, `dragAtMaxAoA`, and `ignoresShielding` entries were removed from that module. Control/stabilizer tuning is unchanged, including control fraction 2.0, to isolate the primary-wing comparison.

Restart KSP to load the new assembly/configuration and use a newly placed Mk18 in the test craft so an old saved MODULE node cannot mask the module change. First compare the baseline with the prior root-based result. Then select a valid dedicated orientation transform with the same world rotation: moving that transform should no longer change velocity sampling. Change only one setting per test. To deliberately compare displaced sampling, set `velocitySampleOffset`; to compare matching force locations, disable the corresponding part-offset flag and set its explicit offset.

Diagnostics contain physics time, actual resolved reference name, sample/root velocity, normal, incidence/curve input, pressure, Mach, coefficient, lift/drag forces and application positions, vessel CoM, force moment, and rigidbody angular velocity. Force arrows and the ILiftProvider/editor lift query use the configured force positions. Packed, shielded, zero-coefficient, missing-rigidbody, zero-flow, and invalid-force states apply no forces and clear force diagnostics. `ModuleParafoil.diagnosticsOnly` still controls only that controller's interventions; the configured lifting module continues its own normal aerodynamic calculation.

## Verification

### Flight and steering measurements

With `ModuleParafoil.debugMode = true`, each existing `[ModuleParafoil] -` diagnostic phase also records:

- `fixedTime`: physics time in seconds.
- `vessel.verticalSpeedMps`: signed stock vertical speed; positive climbs, negative descends. `vessel.descentSpeedMps` is its downward-only magnitude.
- `vessel.horizontalSpeedMps`: surface velocity projected onto the local horizon.
- `vessel.courseDeg`: direction of horizontal travel, clockwise from north (0 north, 90 east). Undefined below 0.1 m/s horizontal speed.
- `vessel.referenceHeadingDeg`: the horizontal heading of the selected control reference's +Y/nose axis; distinct from travel direction.
- `vessel.referenceBankDeg`: signed angle about the reference nose from `nose cross local-up` to the reference right axis. Heading/bank are undefined when the nose is effectively vertical. This describes the selected control reference, not necessarily the canopy's wing frame.
- `vessel.SAS`: SAS action-group state.
- `control.preAutopilotPitchRollYaw` and its `control.preAutopilotFixedTime`: a read-only snapshot from the vessel's pre-autopilot callback, in pitch/roll/yaw order. This is input before autopilot processing, not a guarantee of raw hardware input; trim and other mods can affect the input pipeline.
- `control.effectivePitchRollYaw` and `control.trimPitchRollYaw`: current vessel control-state values and trim. Compare timestamps/phases rather than assuming every observation came from the same input-processing instant.
- `controlSurface[i].deflectionDeg`: the stock module's actual signed deflection, observed through a cached read-only reflection lookup. If that stock field is unavailable, the log reports `<unavailable>`.

No additional file or recorder is needed: measurements go to `C:\KSPDev\KSP.log` at ModuleParafoil's existing `debugLogInterval` (0.25 seconds is a useful starting point). The prototype wing's separate `debugMode` does not enable controller logs. Restart KSP after deploying the updated DLL. Preserve the log before another session replaces it. Compare `phase=AFTER ModuleParafoil` lines for controller state and correlate with wing diagnostics by `fixedTime`.

Build with the project's standard Debug command; its post-build event deploys the DLL/PDB. Run the isolated behavioral harness with:

```powershell
dotnet run --project source/Tests/LiftingSurfacePrototype/LiftingSurfacePrototype.csproj
```

The harness compiles the production module against minimal engine doubles. It checks sample/orientation independence, explicit sample response to angular velocity, world force positions, ILiftProvider dispatch, force guards and reset behavior, and internal-drag disabling. It does not simulate stock curves, Unity message dispatch, ModuleParafoil timing, or actual flight stability. Verify in KSP that only the derived physics message runs, lift/drag are not doubled, and deployment/cut safety remains correct before accepting the prototype.
