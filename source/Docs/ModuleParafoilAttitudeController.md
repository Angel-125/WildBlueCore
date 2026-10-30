# ModuleParafoil attitude-controller test

The optional controller in `ModuleParafoil` supplies EVA-style assistance using limited torque through the vessel's control-reference part (root part as fallback). It never assigns rigidbody velocity, rotation, or angular velocity. Existing lift modules still determine lift, drag, and the flight path. This prototype needs KSP flight validation; the numerical tests exercise reference frames and feedback, not the complete aerodynamic vessel.

The first assisted flight exposed a handoff problem: rotation exceeded 1,600 degrees/second before assistance started. Torque initially opposed that spin, but subsequently saturated. That version introduced wing lift before handoff, measured the chute rigidbody's rotation, and applied vessel-sized torque through that lightweight part. The revision holds primary/control lift at zero through inflation, uses the mass-weighted vessel angular velocity converted from KSP's reference frame to world space, and applies torque through the control-reference part instead of the chute. The joint response still needs in-game validation.

The next flight showed that holding primary lift at zero created a recovery deadlock: error remained 80-175 degrees, trim reached its separate limit, and descent stayed above 100 m/s. The current revision therefore ramps toward a 5% recovery-lift floor after handoff even at large attitude errors. Attitude error and angular speed below 30 degrees/degrees per second allow lift to increase to 100%. Moderate excursions hold authority above that floor; error above 60 degrees or rate above 90 degrees/second unloads toward 5%, not zero. If SAS is on, torque assistance is suspended and primary lift uses a normal time ramp instead. Existing passive-stabilizer timing is retained.

Trim now integrates the **inner rotation-rate error**, with gain 2 and acceleration limit 20. It no longer integrates the outer attitude error while the desired rotation rate is capped; that approach produced a limit cycle in the added disturbance regression test. Integration is accepted when within torque authority or when it unwinds an already saturated command. The total assistance torque remains limited to 100. No claim of in-game recovery follows from the numerical tests alone.

## Live Mk18 configuration

Only the live `C:\KSPDev\GameData\WildBlueIndustries\00WildBlueCore\Parts\Aero\wbiMk18Parachute\wbiMk18Parachute.cfg` was changed. The user's current primary-wing coefficient of 48 and both passive stabilizers are retained. The stock `ModuleControlSurface` node was removed so its forces do not compete with the attitude controller. Immediately before removal its experimental settings were range 2 degrees and area 62, coefficient 1.25, actuator speed 25, transform ControlSurface. Steering for this test is entirely through the new torque controller; the visible ControlSurface transform is no longer actuated by a stock module.

The controller is enabled only for this part configuration; `enableAttitudeController` defaults false in code. Its normal prerequisites are full deployment/handoff, a rigidbody, unpacked flight, exposure to airflow, dynamic pressure at least 0.01 kPa, and an airborne vessel. Cut, landing, splashdown, diagnosticsOnly, and controller deactivation stop assistance and clear its accumulated correction. Only the first eligible configured parafoil in the vessel's part/module order supplies assistance, avoiding duplicate torque from multiple chutes.

## Controls and first test

1. Restart KSP to load the DLL and cfg. Use a newly placed Mk18 so saved craft MODULE nodes do not retain the removed stock control surface.
2. **Turn SAS off.** With the supplied configuration SAS on suspends this controller (`attitude.status=SAS-active`); it does not switch SAS off for the player.
3. Deploy and leave controls released while assistance settles the attitude and introduces wing lift. Torque ramps over 0.5 seconds; lift initially ramps toward 5% recovery support and then increases toward full authority when settled. Observe `attitude.liftAuthority` rather than assuming full lift begins immediately after deployment.
4. Apply short yaw inputs left and right, releasing between them. Roll input also commands coordinated turns; yaw plus roll are summed and clamped. Releasing a turn levels the target bank and retains the commanded heading.
5. Try short pitch inputs. Positive input increases commanded nose elevation, negative decreases it. The default neutral elevation is -5 degrees and the pitch range is +/-15 degrees.

The target heading initially follows horizontal surface velocity, with the selected forward axis/north as low-speed fallbacks. Turn commands request bank up to 35 degrees; the controller advances heading at a coordinated rate based on gravity, speed, and bank (`g tan(bank) / speed`), capped at 12 degrees/second. At low speed bank is reduced to match the heading-rate cap. A 15 m/s speed floor prevents singular low-speed commands. Release returns bank to zero, rather than preserving a rolling command.

## Reference frame and tuning

By default, `attitudeUseVesselReference = true`: the frame is the vessel's selected control reference, independent of the chute's mounting rotation. Standard KSP axes are `attitudeForwardAxis = 0, 1, 0` (nose) and `attitudeUpAxis = 0, 0, -1` (dorsal). Custom control references can change their meaning. Setting `attitudeUseVesselReference = false` interprets the same configured axes in the chute part's frame instead. Parallel or otherwise invalid configured axes disable assistance with a diagnostic status.

| Setting | Live value | Purpose |
| --- | --- | --- |
| enableAttitudeController | true | Enable the optional controller |
| attitudeControllerWithSAS | false | Suspend when SAS is on, avoiding competing controllers |
| attitudeGlidePitch | -5 | Neutral elevation above the local horizon, degrees |
| attitudePitchRange | 15 | Additional pitch command range, degrees |
| attitudeMaxBank | 35 | Bank limit, degrees (also constrained internally to 60) |
| attitudeMaxTurnRate | 12 | Heading-rate cap, degrees/second |
| attitudeMaxCorrectionRate | 35 | Attitude error correction-rate cap, degrees/second, before turn feed-forward |
| attitudeResponse | 2 | Attitude error to desired angular-rate gain |
| attitudeDamping | 4 | Angular-rate error to angular-acceleration gain |
| attitudeIntegralGain | 2 | Integrated angular-rate-error acceleration gain |
| attitudeIntegralLimit | 20 | Integrated acceleration magnitude cap, radians/second squared |
| attitudeMaxTorque | 100 | Total world torque magnitude cap, kN m |
| attitudeRampTime | 0.5 | Seconds to ramp torque after activation or resuming from SAS |
| attitudeLiftRampTime | 4 | Seconds of settled assistance to introduce primary wing lift |
| attitudeRecoveryLift | 0.05 | Recovery lift floor after handoff, fraction of each wing's configured coefficient |

Torque uses vessel moment-of-inertia components in the vessel reference frame, transformed back into world space. The controller uses the shortest quaternion attitude error, caps the requested correction rate, damps measured rotation, and adds bounded rate-error trim for persistent aerodynamic moments. Anti-windup blocks integration further into saturation while allowing it to unwind. Pre-autopilot pitch/roll/yaw commands are observed without mutating flight inputs, even when debug logging is off; stale samples are discarded. SAS corrections are not used as steering commands.

This is attitude/turn assistance, not automatic descent-speed or landing control. The neutral glide pitch and lift/drag tuning will still need flight testing to obtain a suitable landing speed.

## Diagnostics and verification

Existing `[ModuleParafoil] -` observations include `attitude.status`, requested pitch/bank/heading rate, error angle, actual assistance torque, world heading target, integrated acceleration, and reference-frame choice. The revision adds `attitude.liftAuthority`, `attitude.worldAngularVelocity`, `attitude.vesselMOI`, and `attitude.torquePart`. Correlate `phase=AFTER ModuleParafoil` with the existing SAS/input/deflection, course, bank, and descent-speed measurements. A `controlling` status with persistently large error and torque near 100 suggests the torque cap or aerodynamic trim needs investigation; it does not establish which without flight data.

Current diagnostics also record world target/measured quaternions, the error vector, torque and integral limits, and recovery-lift fraction, so a field test can verify the actual frame and active tuning.

Build with the standard Debug msbuild command; the project post-build event deploys DLL/PDB. The isolated harness is `dotnet run --project source/Tests/LiftingSurfacePrototype/LiftingSurfacePrototype.csproj`. It checks torque limits, damping direction, quaternion sign/wrap, arbitrary configured axes including the KSP reference, inertia-frame conversion, and closed-loop recovery/turn release. The disturbance regression starts with 170 degrees of error, initial spin, and inertia near the logged 3.3-5 range. It applies a persistent opposing moment that grows with lift and reverses at 30 seconds, at 0.02 and 0.08 second timesteps. It checks recovery time, convergence, full lift, hard trim/torque limits, and saturated-trim unwinding. In-game deployment, controller gating, multi-part joint response, and landing/cut behavior remain required flight checks.
