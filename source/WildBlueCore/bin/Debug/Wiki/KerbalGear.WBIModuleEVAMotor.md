            
Provides a stock-compatible electric engine for a KerbalGear wearable. The environmental propellant is virtual: its resource definition supplies the density used by ModuleEngines, while ElectricCharge is the only resource actually consumed.
        
## Fields

### atmosphericResourceName
Resource definition used as the working fluid while in an atmosphere. An empty value disables atmospheric operation.
### aquaticResourceName
Resource definition used as the working fluid while underwater. An empty value disables aquatic operation.
### propellantResourceName
Resource consumed to power the motor.
### propellantResourceRate
Resource amount consumed per second at full throttle.
### atmosphericFlowCurve
Multiplies atmospheric mass flow and thrust based on static pressure in atmospheres. Aquatic operation is not affected by this curve. If no keys are configured, atmospheric flow remains at full strength for backward compatibility.
### environmentDisplay
Displays the currently selected propulsion environment.
### evaThrottle
Throttle commanded for the EVA vessel. Stock FlightInputHandler does not process gradual throttle input for EVA vessels, so the motor owns and supplies this value.
### forwardThrustTransformName
Model transform whose orientation defines forward thrust. Its position is ignored because the generated stock-engine thrust transform is always placed at the vessel center of mass.
### reverseThrustTransformName
Optional model transform whose orientation defines reverse thrust. If it is absent, the forward transform is rotated 180 degrees.
### canReverseThrust
Indicates whether reverse thrust is available.
### reverseThrust
Indicates whether the motor is currently producing reverse thrust.
### reverseThrustAnimation
Optional animation played when changing thrust direction.
### animationLayer
Animation layer used by the reverse-thrust animation.
### rotorTransformName
Name of the transform containing the visible rotor.
### rotorRotationAxis
Comma-separated local axis around which the rotor turns.
### rotorRPM
Base rotor speed used by the visual animation.
### rotorSpoolTime
Seconds required to spool the visible rotor up or down.
### blurredRotorFactor
Multiplier applied to the normal rotor while the blurred rotor is visible.
### minThrustRotorBlur
Percentage of maximum thrust at which the blurred rotor becomes visible.
### blurredRotorName
Name of the blurred rotor transform.
### blurredRotorRPM
Rotation speed of the blurred rotor transform.
### isBlurred
Indicates whether the blurred rotor is currently displayed.
### neutralSpinRate
Degrees per second used to return the rotor to its neutral orientation after shutdown.
### restoreToNeutralRotation
Indicates whether the rotor returns to its neutral orientation after stopping.
### runningSound
GameDatabase URL of the looping motor sound, without a file extension.
### runningSoundVolume
Maximum running-sound volume before the stock ship-volume setting is applied.
### runningSoundPitchMin
Running-sound pitch when the rotor first begins turning.
### runningSoundPitchMax
Running-sound pitch at full rotor spool.
## Methods


### ToggleThrustDirection
Toggles forward and reverse thrust and updates the rotor direction.

### ToggleThrustDirectionAction(KSPActionParam)
Action-group wrapper for toggling forward and reverse thrust.
> #### Parameters
> **param:** KSP action parameters.


### OnStart(PartModule.StartState)
Creates the center-of-mass thrust transform before stock ModuleEngines locates its transforms, then resolves the wearable's visual transforms.
> #### Parameters
> **state:** Current part-module start state.


### OnInactive
Shuts the motor down and removes its generated thrust transform when KerbalGear removes the dynamic EVA module.

### OnDestroy
Removes the vessel input callback during scene or part teardown.

### OnUpdate
Supplies the throttle controls that stock FlightInputHandler skips for EVA vessels.

### GetModuleDisplayName
Reports the EVA-specific module title in the part action window.
> #### Return value
> Localized EVA electric motor title.

### CheckDeprived(System.Double,System.String@)
Treats the selected atmospheric or aquatic resource as an unlimited environmental working fluid while still requiring ElectricCharge.
> #### Parameters
> **requiredPropellant:** Stock virtual-propellant requirement.

> **propName:** Name of the unavailable resource.

> #### Return value
> True when the environment or ElectricCharge requirement is not met.

### RequestPropellant(System.Double)
Supplies the virtual environmental propellant and consumes ElectricCharge in proportion to the stock engine's requested mass flow.
> #### Parameters
> **mass:** Propellant mass requested by ModuleEngines for this physics tick.

> #### Return value
> Fraction of the request that ElectricCharge can support.

### ModifyFlow
Scales atmospheric mass flow and thrust with the configured pressure curve while leaving underwater operation at full strength.
> #### Return value
> Environmental flow available to the stock engine simulation.

### FixedUpdate
Updates environmental availability and the center-of-mass transform before running the stock engine simulation, then updates the integrated propeller animation.

### setupPAWGroup
Places this module's fields and events, including the PAW entries inherited from ModuleEngines, in a single EVA Motor group.

### updateEVAThrottle
Applies the standard throttle-up, throttle-down, full-throttle, and cutoff bindings to the active EVA vessel.

### registerFlyByWire
Registers the callback that supplies EVA throttle after stock input processing.

### unregisterFlyByWire
Stops supplying EVA throttle to the vessel.

### onFlyByWire(FlightCtrlState)
Keeps the vessel control state, engine, and stock throttle display synchronized.

### registerThrottleGauge
Registers a pre-render callback for the stock throttle gauge. Stock KSP displays EVA throttle as the binary KerbalEVA.JetpackIsThrusting state instead of mainThrottle.

### unregisterThrottleGauge
Stops overriding the stock EVA throttle gauge.

### updateThrottleGauge
Displays the motor's continuous throttle after the stock EVA gauge applies its binary jetpack indication, but before the canvas is rendered.

### setupRunningSound
Creates the spatial looping sound used by the integrated rotor.

### updateRunningSound
Fades and pitches the motor loop using the same spool value as the visible rotor.

### cleanupRunningSound
Stops and destroys the module-owned motor sound.

### updateConsumedResources
Resolves the resource definition used to power the motor.

### updateEnvironment(System.Boolean)
Detects atmosphere versus water and selects the matching virtual propellant definition.
> #### Parameters
> **forceRefresh:** Whether to rebuild the virtual propellant even if the environment has not changed.


### configureVirtualPropellant
Replaces the stock propellant list with one virtual working fluid. No corresponding PartResource is added to the EVA part.

### environmentIsSupported
Determines whether the selected environment has a valid, non-massless working-fluid definition.
> #### Return value
> True when thrust can be produced in the current environment.

### updateEnvironmentDisplay
Updates the localized environment shown in the part action window.

### createThrustTransform
Creates the transform consumed by ModuleEngines and gives it a unique name.

### updateThrustTransform
Keeps the engine force application point at vessel center of mass while copying orientation from the wearable's configured forward or reverse direction transform.

### updateReverseThrustUI
Updates the reverse-thrust event label and visibility.

### updateReverseAnimation
Initializes and updates the optional reverse-thrust animation.

### setupVisualTransforms
Resolves all wearable-model transforms used by thrust direction and propeller animation.

### updateRotor(System.Boolean)
Advances the rotor spool, spin, blur, and neutral-return states.
> #### Parameters
> **running:** True when the stock engine is producing thrust.


### rotateRotorRunning
Spins and spools the rotor while selecting the blurred meshes at sufficient thrust.

### rotateRotorShutdown
Spools the rotor down and optionally returns it to its neutral local orientation.

### rotateTransform(UnityEngine.Transform,System.Single,System.Boolean)
Rotates a propeller transform and records its accumulated angle.
> #### Parameters
> **target:** Transform to rotate.

> **degreesPerSecond:** Requested angular speed.

> **trackRotation:** Whether this transform contributes to the normal rotor's neutral-return angle.


### setupRotorVisibility
Shows the appropriate normal, mirrored, and blurred blade meshes.

### setupBlurredState(System.Boolean)
Changes blurred-rotor state only when necessary.
> #### Parameters
> **newBlurredState:** True to display the blurred rotor.


### getTransforms(System.String)
Finds all named transforms in a comma-separated list.
> #### Parameters
> **transformNames:** Comma-separated transform names.

> #### Return value
> Transforms found on the EVA part hierarchy.

### findPartTransform(System.String)
Finds a transform anywhere below the EVA part. Wearable props are instantiated beside the stock model hierarchy, so Part.FindModelTransform cannot resolve their transforms.

### setTransformsVisible(UnityEngine.Transform[],System.Boolean)
Shows or hides an array of blade transforms and their colliders.
> #### Parameters
> **transforms:** Transforms to update.

> **isVisible:** Desired visibility.


### parseVector(System.String,UnityEngine.Vector3)
Parses a comma-separated Vector3 field.
> #### Parameters
> **value:** Serialized vector.

> **fallback:** Value returned when parsing fails.

> #### Return value
> Parsed vector or the supplied fallback.

