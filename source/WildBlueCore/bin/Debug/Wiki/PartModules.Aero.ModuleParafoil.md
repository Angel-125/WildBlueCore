            
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
## Methods


### FixedUpdate
After the base FixedUpdate is called, handle updates to the liftingSurface and controlSurface.

