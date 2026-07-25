            
Acts as a controller for ModuleLiftingSurface and ModuleControlSurface As the parachute deploys, it gains lift and flight control- if the above part modules are found.
        
## Fields

### retractedDeflectionLiftCoeff
Lift coefficient when the chute is fully retracted
### semiDeployedDeflectionLiftCoeff
Lift coefficient when the chute is semi-deployed (not fully open)
### deployedDeflectionLiftCoeff
Lift coefficient when the chute is deployed (fully open)
### retractedCtlSfcDeflectionLiftCoeff
Control surface's lift coefficient when the chute is fully retracted
### semiDeployedCtlSfcDeflectionLiftCoeff
Control surface lift coefficient when the chute is semi-deployed (not fully open)
### deployedCtlSfcDeflectionLiftCoeff
Control surface lift coefficient when the chute is deployed (fully open)
### enableControlInSemiDeploy
Flag indicating whether or not the parafoil can be steered when in the semi-deployed state.
### debugMode
Enables FixedUpdate diagnostics in KSP.log.
### debugLogInterval
Minimum time, in seconds, between groups of FixedUpdate diagnostic messages. Set to 0 to log every physics update.
### controlAuthorityRampTime
Time, in seconds, to ramp the control surface and passive stabilizers from zero to their configured authority after the parafoil takes control.
### diagnosticsOnly
When enabled, ModuleParafoil only emits diagnostics and leaves all parachute, drag, rotation, lift, and control behavior to the base module.
## Methods


### OnStart(PartModule.StartState)
Overrides base OnStart to provide custom functionality.
> #### Parameters
> **state:** 


### FixedUpdate
Lets ModuleParachute control deployment, then hands fully deployed flight to the parafoil lifting and control surfaces.

