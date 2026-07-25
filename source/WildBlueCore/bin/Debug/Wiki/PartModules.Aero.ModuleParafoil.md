            
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
## Methods


### OnStart(PartModule.StartState)
Overrides base OnStart to provide custom functionality.
> #### Parameters
> **state:** 


### FixedUpdate
Lets ModuleParachute control deployment, then hands fully deployed flight to the parafoil lifting and control surfaces.

