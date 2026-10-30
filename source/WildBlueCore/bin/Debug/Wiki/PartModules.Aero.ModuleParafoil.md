            
Acts as a controller for ModuleLiftingSurface and ModuleControlSurface As the parachute deploys, it gains lift and flight control- if the above part modules are found.
        
## Fields

### debugMode
Enables FixedUpdate diagnostics in KSP.log.
### debugLogInterval
Minimum time, in seconds, between groups of FixedUpdate diagnostic messages. Set to 0 to log every physics update.
### diagnosticsOnly
When enabled, ModuleParafoil only emits diagnostics and leaves all parachute, drag, rotation, lift, and control behavior to the base module.
### semiDeployedDeflectionLiftCoeff
Lift coefficient when the chute is semi-deployed (not fully open)
### semiDeployedCtlSfcDeflectionLiftCoeff
Control surface lift coefficient when the chute is semi-deployed (not fully open)
### enableControlInSemiDeploy
Flag indicating whether or not the parafoil can be steered when in the semi-deployed state.
### controlAuthorityRampTime
Time, in seconds, to ramp the control surface and passive stabilizers from zero to their configured authority after the parafoil takes control.
### enableAttitudeController
Enable torque-limited glide stabilization and coordinated steering after full deployment.
### attitudeControllerWithSAS
Allow assistance alongside SAS; false avoids competing attitude controllers.
### attitudeUseVesselReference
Use the vessel control reference rather than the chute mounting transform.
### attitudeForwardAxis
Local forward direction in the selected attitude reference.
### attitudeUpAxis
Local dorsal/up direction in the selected attitude reference (KSP uses -Z).
### attitudeGlidePitch
Neutral nose elevation above the horizon, in degrees.
### attitudePitchRange
Maximum additional nose elevation commanded by pitch input, in degrees.
### attitudeMaxBank
Maximum bank commanded by yaw/roll input, in degrees.
### attitudeMaxTurnRate
Upper bound on coordinated heading rate, in degrees per second.
### attitudeMaxCorrectionRate
Upper bound on attitude-correction rate, in degrees per second.
### attitudeResponse
Attitude error to desired rotation-rate gain, per second.
### attitudeDamping
Rotation-rate error to angular acceleration gain, per second.
### attitudeIntegralGain
Integrate angular-rate error to estimate persistent aerodynamic moments.
### attitudeIntegralLimit
Maximum integrated acceleration contribution, in radians per second squared.
### attitudeMaxTorque
Total assistance torque limit in KSP torque units (kN m).
### attitudeRampTime
Seconds to ramp assistance after handoff or resuming from SAS.
### attitudeLiftRampTime
Seconds to introduce primary lift once assistance has settled the vessel.
### attitudeRecoveryLift
Minimum deployed lift fraction during recovery, introduced by the normal lift ramp.
## Methods


### OnStart(PartModule.StartState)
Overrides base OnStart to provide custom functionality.
> #### Parameters
> **state:** 


### FixedUpdate
Lets ModuleParachute control deployment, then hands fully deployed flight to the parafoil lifting and control surfaces.

### OnDestroy
Remove the observational callback before stock parachute cleanup.

