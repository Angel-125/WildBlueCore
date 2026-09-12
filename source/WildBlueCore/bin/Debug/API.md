# WildBlueCore


# PartModules.Aero.ModuleParafoil
            
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

# PartModules.Aero.ModuleParafoilStabilizer
            
Identifies a ModuleLiftingSurface as a passive parafoil stabilizer. ModuleParafoil keeps the surface disabled until the parafoil is fully deployed, then ramps it to its configured deflectionLiftCoeff.
        

# PartModules.Decals.WBIModuleDecal
            
This part module lets you change the decal using the stock flag selector. It does so independently of the mission flag.
        
## Fields

### decalURL
URL to the image that's displayed by the decal.
### isVisible
Flag to indicate whether or not the decal is visible
### alwaysVisible
Override flag to ensure that the decal is always visible.
### allowFieldEdit
Flag to allow users to change the flag while out in the field.
### updateSymmetry
Flag to indicate if the decal updates symmetry parts
### toggleDecalName
GUI name for button that toggles decal visibility
### selectDecalName
GUI name for button that selects the decal.
### reverseDecalName
GUI name for button that reverses the decal.
### decalTransforms
List of transforms that will be changed by the decal. Separate names by semicolon
### normalDecalTransformName
Name of the transform for the normal orientation of the decal.
### reversedDecalTransformName
Name of the transform for the reversed orientation of the decal. This is particularly helpful for creating flags and lettering on the opposite side of the part.
### isReversed
Flag to indicate if the decal is reversed or not.
## Methods


### ToggleDecal
Toggles visibility of the decal.

### SelectDecal
Changes the decal

### ReverseDecal
Reverses the decal if the transform specified by reverseDecalTransformName and normalDecalTransformName both exist.

### onFlagSelected(FlagBrowser.FlagEntry)
Private event handler to respond to flag selection.
> #### Parameters
> **selected:** The selected texture


### ChangeDecal
Changes the decal on all named transforms.

# PartModules.IVA.WBIModuleSeatRotator
            
This module lets users rotate a seat in a part's IVA if the seat is occupied.
            
            
> #### Example
```

            MODULE
            {
                name = WBIModuleSeatRotator
                
                // Name of the seat transform to rotate. This needs to be the same name as in the IVA's 3D model and in the IVA's config file.
                seatName = Seat001
                
                // The name of the prop that the kerbal sits on. This is optional.
                propName = NF_SEAT_Chair_Basic
                
                // If your list of props has more than one prop for the seats, then specify the index of the seat prop.
                propIndex = 2
                
                // The x, y, and z axis to rotate the prop by. The default is 0,0,1
                propRotationAxis = 0,1,0
            }
            
```

            
        
## Fields

### seatName
Name of the seat transform to rotate. This needs to be the same name as in the IVA's 3D model and in the IVA's config file. If you use a prop in addition to the seat transform, be sure to specify the propName and propIndex as well.
### propName
The name of the prop that the kerbals sit on. If the seat transform in your IVA's 3D model is NOT the same thing as the seat prop, then specify the propName as wel as the propIndex in order to rotate the prop along with the seat transform.
### propRotationAxis
The x, y, and z axis to rotate the prop by. The default is 0,0,1
### propIndex
If your list of props has more than one prop for the seats, then specify the index of the seat prop (as it appears in order in the config file) to rotate.
### rotationRate
Rate at which to rotate the seat, in degrees per second.
### rotationAmount
How far to rotate the seat when commanded to rotate the seat
## Methods


### RotateLeft
Rotates the seat to the left.

### RotateRight
Rotates the seat to the right.

# PartModules.Resources.WBIModuleFuelPump
            
This part module pumps one or more resources from the host part to other parts that have the same resource. The module can be directly added to a resource tank part or to a part that is radially attached to a resource tank part. When enabled, WBIModuleFuelPump will automatically pump resources until either the host part's resource is empty or when the destination parts are full. In either case, it will wait until the host part gains more resources to pump or the destination parts gain more room to store the resource.
            
WBIModuleFuelPump will transfer resources based on a part's Flow Priority. Higher priority parts will receive resources before lower priority parts.  
            
WBIModuleFuelPump is designed to pump resources throughout the same vessel, but it can also pump resources to a nearby vessel if it is also equipped with a part that has a WBIModuleFuelPump.  
            
To pump a resource throughout the same vessel, the following conditions must be met:  
            * The fuel pump providing resources must be set to Distribute Localy.  
            * The resource must not be empty.  
            * The resource must be transferrable and unlocked.  
            * The destination parts must have space available to receive the pumped resource.  
            * The destination parts' resource storage must be unlocked.  
            
To pump a resource to another nearby vessel, the following conditions must be met:  
            * The resource must not be empty.  
            * The resource must be transferrable and unlocked.  
            * The destination parts must have space available to receive the pumped resource.  
            * The destination parts' resource storage must be unlocked.  
            * All vessels must be either landed or splashed.  
            * The nearby vessels must be within the provider's pump range.  
            * The pump providing resources must have its pump mode set to Send to remote.  
            * The pumps that receive resources must be set to Receive from remote.  
            
            
> #### Example
```

            MODULE
            {
                name = WBIModuleFuelPump
                maxRemotePumpRange = 200
            }
            
```

            
        
## Fields

### onPumpStateChanged
Signals when the isActivated and/or remotePumpMode changes.
### maxRemotePumpRange
In meters, the maximum range that the fuel pump can reach when remote pumping resources. Default is 2000 meters.
### selfIsHostPart
Flag to indicate that the part that has the WBIModuleFuelPump is the host part.
## Methods


### ActionFuelPumpOn(KSPActionParam)
Turns off the fuel pump.
> #### Parameters
> **param:** A KSPActionParam containing the action parameters.


### ActionFuelPumpOff(KSPActionParam)
Turns off the fuel pump.
> #### Parameters
> **param:** A KSPActionParam containing the action parameters.


### ActionFuelPumpLocal(KSPActionParam)
Sets pump mode to local distribution.
> #### Parameters
> **param:** A KSPActionParam containing the action parameters.


### ActionFuelPumpRemoteSend(KSPActionParam)
Sets the pump mode to send to remote pumps.
> #### Parameters
> **param:** A KSPActionParam containing the action parameters.


### ActionFuelPumpModeReceive(KSPActionParam)
Sets the pump mode to receive from remote pumps.
> #### Parameters
> **param:** A KSPActionParam containing the action parameters.


### DistributeResources(System.Single)
This method will attempt to distribute any resources that the host part has to other parts in the vessel or to nearby vessels. The resources must be capable of being transferred, and they must be unlocked. Additionally, to remotely distribute the resources, remotePumpMode must be set to true, the nearby vessel must have at least one WBIModuleFuelPump, and the nearby vessel's fuel pump' isActivated must be set to true.

### DistributeResourceLocally(PartResource,System.Double,System.Boolean)
Distributes the desired resource locally throughout the vessel.
> #### Parameters
> **resource:** The PartResource to distribute.

> **transferAmount:** A double containing how much of the resource to distrubute.

> **isFromRemotePump:** A bool indicating whether or not the source is from a remote pump. Default is false.

> #### Return value
> True if the distribution was successful, false if not.

# PartModules.Resources.WBIModuleSupplyLine
            
Derived from ModuleFuelPup, this part module provides periodic refills of the resources contained in the storage tank. The storage tank can be either the part that hosts this part module, or, if the host part has no resources, then the tank is the part that the supply line part is attached to. It makes the assumption that the storage tank is completely full when it arrives at the desired destination; how it gets there is up to the player. The part module allows players to specify how long, in hours, it takes between supply runs. It also can optionally charge for the cost of the resources upon delivery. When a delivery is made, the part module can play an EFFECT and/or run an animation.
        
## Fields

### transfersEnabled
Flag to enable periodic transfers. Every transferPeriod, the fuel pump will immediately refill the tank and distribute the contents
### transferTime
In hours, how long to wait before magically refilling the tank and distributing the contents.
### lastUpdated
Last time the pump was updated.
### isRecordingTime
Flag indicating that the pump is recording mission time.
### missionStartTime
Last time the pump was updated.
### missionStopTime
Last time the pump was updated.
### missionElapsedTime
In seconds, elapsed mission time.
### chargeForResources
Flag to indicate whether or not the player should be charged for resource deliveries
### payFlatFee
Flag to indicate whether or not the player should be charged a flat fee to deliver resources
### selfIsHostPart
Flag to indicate that the part that has the WBIModuleSupplyLine is the host part.

# PartModules.WBIModuleAnimateGenericExtended
            
This part module derives from ModuleAnimatedGeneric. It adds the ability to play sound effects while animating. It also adds several options to check before playing the animation. First, it can check the active vessel for a necessary skill and skill level. Second, it can check for and consume required resources. Those resources can be pulled from the part's vessel and/or from a remote vessel. Third, it can check for and consume parts stored in an inventory. Those parts can be pulled form the parts vessel and/or from a remote vessel. Finally, you can provide a list of part modules that will be enabled after tne animation completes, and disabled when when not complete.
        
## Fields

### debugMode
Debug flag
### startEventRequiresSkillCheck
Flag indicating whether or not to require a skill check before playing the start event animation. Default is false.
### endEventRequiresSkillCheck
Flag indicating whether or not to require a skill check before playing the end event animation. Default is false.
### skillToCheck
The skill required to unpack the part.
### minimumSkillLevel
The minimum skill level required to unpack the box. Default is 0.
### startEventRequiresResources
Flag to indicate whether or not to make a resource requirements check before playing the start event animation. Default is false.
### endEventGivesResources
Flag to indicate whether or not to make a resource requirements check before playing the end event animation. Default is false.
### allowLoopingStop
Flag to indicate whether or not looped animations are allowed to be stopped.
### canUseRemoteResources
Flag to indicate whether or not when checking resources, resources can come from other vessels. Default is true.
### deployedMass
Mass of the part after being deployed.
### startSoundURL
URL for the start sound played when the animation starts.
### startSoundPitch
Pitch level for the start sound.
### startSoundVolume
Volume level for the start sound.
### loopSoundURL
URL for the loop sound, played while the animation is playing.
### loopSoundPitch
Pitch level for the loop sound.
### loopSoundVolume
Volume level for the loop sound.
### stopSoundURL
URL four the stop sound, played when the animation is completed.
### stopSoundPitch
Pitch level for the stop sound.
### stopSoundVolume
Volume level for the stop sound.
### isDeployed
Flag indicating if the animation is deployed.
## Methods


### OnStartFinished(PartModule.StartState)
This gets called after all part modules in the part have been started. Overriding this gives us the opportunity to disable all the part modules that we manage regardless of their load order.
> #### Parameters
> **state:** The StartState upon finishing the start process.


### Toggle
Toggles the animation.

### ToggleAction(KSPActionParam)
Action to toggle the animation.
> #### Parameters
> **param:** 


### StopLoop
Stops the looping animation

### SetProgress(System.Single)
Sets the Progress level. Call this instead of setting deployPercent directly.
> #### Parameters
> **toValue:** A float between 0 and 100.


### PlayStartSound
Plays the start sound.

### PlayEndSound
Plays the end sound.

# PartModules.Variants.WBIModuleInternalVariants
            
Use this module to change the INTERNAL model (the IVA) of a part.
        
## Fields

### variantIndex
Index for the internal variant.
## Methods


### GetModuleDisplayName
Gets the module display name.
> #### Return value
> A string containing the display name.

### GetInfo
Gets the module description.
> #### Return value
> A string containing the module description.

# PartModules.WBIModulePartSubvariants
            
Helper part module to handle part mesh and texture switching. Stock ModulePartVariants doesn't cooperate with multiple ModulePartVariants in the same part, so this class gets around the issue and adds a few enhancements. When you define a ModulePartVariants, be sure to place its config node AFTER ModulePartVariants. When you define a ModulePartVariants, you can specify some EXTRA_INFO that SWPartVariants uses to configure itself:
            
            
> #### Example
```

            MODULE
            {
                name = ModulePartVariants
                ...
                VARIANT
                {
                    name = someVariantName
                    ...
                    EXTRA_INFO
                    {
                        // WBIModulePartSubvariants can be GUI enabled/disabled using the "enableVariantModuleIDs" and "disableVariantModuleIDs" fields, respectively.
                        // Simply specify the SWPartVariants' moduleID. For multiple moduleIDs, separate them with a semicolon.
                        disableVariantModuleIDs = mirroring
                        
                        // Similarly you can re-apply the WBIModulePartSubvariants' applied variant when this variant is applied.
                        updateVariantModuleIDs = texturing
                    }
                }
            }
            
```

            
            
To define a WBIModulePartSubvariants module:  
            
            
> #### Example
```

            MODULE
            {
                name = WBIModulePartSubvariants
                moduleID = texturing
                updateSymmetry = false
                allowFieldUpdate = false
                
                VARIANT
                {
                    displayName = #LOC_SUNKWORKS_yachtDeck
                    primaryColor = #caa472
                    secondaryColor = #caa472
                    // GAMEOBJECTS, EXTRA_INFO, etc. found in a typical ModulePartVariants are supported but omitted for brevity.
                    
                    // The textures will be applied to all the transforms named in the config node.
                    TEXTURES
                    {
                        mainTextureURL = WildBlueIndustries/SunkWorks/Parts/Structural/BoatHulls/boatHull1Yacht
                        bumpMapURL = WildBlueIndustries/SunkWorks/Parts/Structural/BoatHulls/boatHull1YachtNrm		
                        transformName = cargoKeelBowFull
                        transformName = cargoKeelBowInsert
                        transformName = cargoKeelBowPortHalf
                        transformName = cargoKeelBowStarboardHalf
                        // Add as many as you like
                        transformName = ...
                    }
                }
            }
            
```

            
        
## Fields

### baseVariant
Name of the variant to apply if we haven't selected a variant yet.
### variantIndex
Index for the texture variants.
### updateSymmetry
Flag to indicate if the symmetry parts should also apply the selected variant. Default is true.
### allowFieldUpdate
Flag to indicate whether the variant can be applied post launch. Default is false.
### variantApplied
Field indicating whether or not we have applied the part variant.
### meshSets
If, during a part variant update event, the meshSet field is set in EXTRA_INFO, then we'll record what the meshSet's value is and apply the set IF the value is on our list. If our meshSets is empty (the default), then we'll ignore any meshSet fields passed in with EXTRA_INFO.
### currentMeshSet
The currently selected mesh set.
## Methods


### OnStart(PartModule.StartState)
Handles the OnStart event.
> #### Parameters
> **state:** A StartState containing the starting state.


### OnAwake
Handles OnAwake event

### OnDestroy
Handles the OnDestroy event

### GetModuleDisplayName
Gets the module display name.
> #### Return value
> A string containing the display name.

### GetInfo
Gets the module description.
> #### Return value
> A string containing the module description.

### GetModuleCost(System.Single,ModifierStagingSituation)
Returns the Module cost modifier. It is added to the part's total cost.
> #### Parameters
> **defaultCost:** Default cost of the part

> **sit:** The situation in which the call is being made.

> #### Return value
> A float containing the modified cost.

### GetModuleCostChangeWhen
Describes when the part modifier changes.
> #### Return value
> A ModifierChangeWhen indicating when the modifier is applied.

### GetModuleMass(System.Single,ModifierStagingSituation)
Returns the Module cost modifier. It is added to the part's total mass.
> #### Parameters
> **defaultMass:** Default mass of the part

> **sit:** The situation in which the call is being made.

> #### Return value
> A float containing the modified mass.

### GetModuleMassChangeWhen
Describes when the part modifier changes.
> #### Return value
> A ModifierChangeWhen indicating when the modifier is applied.

# KerbalGear.WBIModuleEVAMotor
            
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

# KerbalGear.WBIModuleEVAResourceTransfer
            
Enables resource transfers between inventory parts and exposes selected inventory resources through the EVA Kerbal's stock vessel-resource interface.
        
## Fields

### managedResourceNames
Names of transient proxy resources created during the previous save session. This lets the module discard saved proxies before rebuilding them from inventory snapshots.
## Methods


### InventoryResourceContributor.GetAmount
Returns the resource amount represented by this inventory stack.

### InventoryResourceContributor.GetMaxAmount
Returns the resource capacity represented by this inventory stack.

### InventoryResourceContributor.SetAmount(System.Double)
Sets the stack's total amount while retaining a single per-item snapshot value.

### InventoryResourceAggregate.GetAmount
Calculates the current resource amount across all contributing inventory snapshots.

### InventoryResourceAggregate.GetMaxAmount
Calculates the resource capacity across all contributing inventory snapshots.

### OnStart(PartModule.StartState)
Initializes the EVA and inventory references used by the resource bridge.
> #### Parameters
> **state:** KSP's current part-module startup state.


### OnActive
Rebuilds inventory resource proxies when a carried item activates this EVA ability.

### OnInactive
Commits proxy changes and removes them when the enabling cargo item is unequipped.

### OnKerbalGearInventoryChanged(ModuleInventoryPart)
Rebuilds proxy contributors once after a retained KerbalGear module's inventory changes.
> #### Parameters
> **changedInventory:** The EVA inventory whose contents changed.


### FixedUpdate
Synchronizes stock resource requests with the contributing inventory snapshots.

### OnDestroy
Flushes inventory amounts and removes transient proxy resources during EVA teardown.

### GetModuleMass(System.Single,ModifierStagingSituation)
Cancels the resource mass represented by live proxies because ModuleInventoryPart already includes the same inventory resource mass.

### GetModuleMassChangeWhen
Reports that proxy mass can change whenever its resource amount changes.

### GetModuleCost(System.Single,ModifierStagingSituation)
Cancels the resource cost represented by live proxies because ModuleInventoryPart already includes the same inventory resource cost.

### GetModuleCostChangeWhen
Reports that proxy cost can change whenever its resource amount changes.

### EnsureInitialized
Lazily initializes references because the wearables controller can call OnActive before KSP invokes this module's OnStart method.

### RebuildResourceProxies
Recreates live Kerbal resources from the inventory types selected by provider parts.

### GetExposedResourceNames
Gets resource names carried by parts that activate WBIModuleEVAResourceTransfer.

### IsResourceProviderPart(System.String)
Reports whether a cargo part uses WBIModuleWearableItem to activate this module.

### BuildResourceAggregates(System.Collections.Generic.HashSet{System.String})
Builds contribution lists for selected resource types across the entire EVA inventory.

### CaptureResourceProxies
Captures live proxy objects so inventory refreshes can reuse them without rebuilding PAW rows.

### ReconcileResourceProxies(System.Collections.Generic.Dictionary{System.String,PartResource})
Reuses proxies whose resource types remain exposed, removes stale proxies, and creates new types.

### SynchronizeResourceProxies
Applies proxy resource changes to inventory snapshots and refreshes aggregate totals.

### SynchronizeResourceProxiesInternal
Performs synchronization while the caller owns the recursion guard.

### DrainContributors(WildBlueCore.KerbalGear.WBIModuleEVAResourceTransfer.InventoryResourceAggregate,System.Double)
Drains provider parts first, then uses the same ascending resourcePriorityOffset ordering that KerbalEVA uses for EVA Propellant.

### FillContributors(WildBlueCore.KerbalGear.WBIModuleEVAResourceTransfer.InventoryResourceAggregate,System.Double)
Fills contributors in the same deterministic priority order used for draining.

### RemoveResourceProxies
Synchronizes and removes all live proxy resources owned by this module.

### RemoveResourceProxiesInternal
Removes proxies while the caller owns the recursion guard.

### RemovePersistedResourceProxies
Removes proxy resources restored by KSP before rebuilding from authoritative snapshots.

# KerbalGear.WBIModuleEVAAblator
            
Provides stock-style ablative cooling for an EVA kerbal. KerbalGear creates this module while carried equipment requests it, and WBIModuleEVAResourceTransfer exposes the equipment's stored coolant as a resource on the EVA part.
            
            
> #### Example
```

            MODULE
            {
                name = WBIModuleWearableItem
                moduleID = EVA Cooling Pack
                evaModules = WBIModuleEVAResourceTransfer;WBIModuleEVAAblator
            }
            RESOURCE
            {
                name = Ablator
                amount = 10
                maxAmount = 10
            }
            
```

            
        
## Fields

### ablativeResource
Resource consumed to remove heat from the EVA kerbal.
### lossConst
Constant multiplier in the stock ablation-rate equation.
### lossExp
Exponent numerator in the stock ablation-rate equation. This must be negative.
### pyrolysisLossFactor
Multiplier applied to the resource's specific heat to determine removed thermal flux.
### ablationTempThresh
Skin temperature in kelvin above which cooling begins.
### outputResource
Optional byproduct generated from consumed coolant.
### outputMult
Units of output generated per unit of coolant consumed.
### loss
Current coolant mass-loss rate in kilograms per second. Visible with thermal data.
### flux
Current thermal flux removed from the EVA kerbal. Visible with thermal data.
## Methods


### OnStart(PartModule.StartState)
Resolves configured resources and initializes the thermal-data fields.
> #### Parameters
> **state:** KSP's current PartModule startup state.


### OnActive
Enables cooling when KerbalGear creates or activates the requested module.

### OnInactive
Stops cooling immediately when the last contributing inventory item is removed. No conductivity restoration is necessary because this module never changes it.

### OnUpdate
Updates thermal-data visibility while this dynamically created module is active.

### GetModuleDisplayName
Returns the localized title shown in the EVA kerbal's action window.
> #### Return value
> The localized EVA Ablator title.

### OnKerbalGearInventoryChanged(ModuleInventoryPart)
Refreshes resource definitions after inventory changes alter the available proxies.
> #### Parameters
> **inventory:** The EVA inventory whose contents changed.


### FixedUpdate
Consumes coolant and applies stock-style negative exposed thermal flux to the EVA part.

### OnDestroy
Clears transient display state when Unity destroys the dynamic component.

### resolveResourceDefinitions
Resolves the configured coolant and optional output resource definitions.

### updateFieldVisibility
Shows stock thermal diagnostics only while the module and thermal-data overlay are active.

### resetThermalData
Clears per-tick diagnostic values without modifying persistent kerbal thermal properties.

# KerbalGear.WBISuitAssignmentRepair
            
Repairs stale suit combo and EVA mesh assignments whenever a flight scene starts.
        

# KerbalGear.WBIModuleEVAExperienceEffects
            
Grants temporary stock experience effects while their contributing wearable cargo is carried by an EVA kerbal. Add EXPERIENCE_EFFECT child nodes to the wearable part's part-level EVA_OVERRIDES node; KerbalGear creates this module automatically when needed.
            
            
> #### Example
```

            EVA_OVERRIDES
            {
                EXPERIENCE_EFFECT
                {
                    name = RepairSkill
                    tier = 2
                }
            }
            
```

            
        
## Methods


### OnKerbalGearProvidersChanged(ModuleInventoryPart,WildBlueCore.KerbalGear.KerbalGearModuleProvider[])
Rebuilds the temporary effects from all inventory providers assigned to this aggregate module. Duplicate effect types result in one active effect with reference-counted claims.

### PartHasExperienceEffects(AvailablePart)
Determines whether a cargo part declares at least one experience effect.

# KerbalGear.WBIModuleEVAExperienceEffects.EffectClaim
            
Describes one EXPERIENCE_EFFECT request read from one carried inventory provider. Claims are short-lived value data collected before duplicate effect types and tiers are consolidated, so this is a struct rather than persistent runtime state.
        

# KerbalGear.WBIModuleEVAExperienceEffects.DesiredEffect
            
Describes the single effect that should exist after every matching EffectClaim has been consolidated. It holds the winning configuration, effective tier, and comparison signature used to decide whether the currently active effect must be replaced.
        

# KerbalGear.WBIModuleEVAExperienceEffects.ActiveEffect
            
Tracks an instantiated ExperienceEffect and its live PartValues registration. This is mutable runtime state and therefore remains a class: tierProvider captures this exact instance, and its stable identity is needed to unregister the same delegate later.
        

# KerbalGear.KerbalGearModuleMode
            
Determines how KerbalGear creates a requested EVA PartModule.
        
## Fields

### Exclusive
Creates one module for the first deterministic provider. This is the default.
### Aggregate
Creates one module shared by all providers. The module must aggregate inventory state.
### PerProvider
Creates a separate module for each stored-part snapshot that requests it.

# KerbalGear.KerbalGearModuleDefinition
            
Describes a dynamically available KerbalGear EVA module.
        
## Properties

### ModuleName
Gets the PartModule class name.
### Mode
Gets the configured module multiplicity.
### ModuleConfig
Gets a copy of the MODULE configuration without KerbalGear-only values.
## Methods


### Constructor
Creates a dynamic module definition from a KERBAL_EVA_MODULES entry.
> #### Parameters
> **moduleName:** The PartModule class name.

> **mode:** The requested module multiplicity.

> **moduleConfig:** The configuration passed to the dynamic PartModule.


# KerbalGear.WBIModuleKerbalEVAModules
            
Adds the always-present KerbalGear controller to EVA prefabs and registers the remaining KERBAL_EVA_MODULES entries for on-demand creation by that controller.
        
## Methods


### TryGetModuleDefinition(System.String,WildBlueCore.KerbalGear.KerbalGearModuleDefinition@)
Finds the configuration used to create a requested EVA module at runtime.
> #### Parameters
> **moduleName:** The requested PartModule class name.

> **definition:** The registered definition, when found.

> #### Return value
> True when KERBAL_EVA_MODULES defines the requested module.

### EVAModulesLoader.IsReady
Indicates that the loader has no asynchronous work to finish.
> #### Return value
> Always true.

### EVAModulesLoader.StartLoad
Builds the dynamic module registry and installs the KerbalGear controller.

### EVAModulesLoader.registerDynamicModule(System.String,ConfigNode)
Registers a dynamic module and removes loader-only values from its runtime config.
> #### Parameters
> **moduleName:** The PartModule class name.

> **sourceConfig:** The KERBAL_EVA_MODULES module node.


### Awake
Inserts the KerbalGear loader immediately after KSP's PartLoader.

# KerbalGear.WBIModuleKerbalEVAModules.EVAModulesLoader
            
Reads KERBAL_EVA_MODULES after parts load, registers dynamic definitions, and adds only the lightweight controller to each EVA prefab.
        
## Methods


### IsReady
Indicates that the loader has no asynchronous work to finish.
> #### Return value
> Always true.

### StartLoad
Builds the dynamic module registry and installs the KerbalGear controller.

### registerDynamicModule(System.String,ConfigNode)
Registers a dynamic module and removes loader-only values from its runtime config.
> #### Parameters
> **moduleName:** The PartModule class name.

> **sourceConfig:** The KERBAL_EVA_MODULES module node.


# KerbalGear.WBIModuleSuitSwitcher
            
This part module allows kerbals to change their outfits after the vessel leaves the VAB/SPH.
            
            
> #### Example
```

            MODULE
            {
                name = WBIModuleSuitSwitcher
            }
            
```

            
        
## Methods


### OpenWardrobe
Opens the wardrobe GUI.

# KerbalGear.BodyLocations
            
Various locations where an wearable item can be placed. This is primarily used for WBIModuleWearableItem.
        
## Fields

### back
On the back of the kerbal.
### backOrJetpack
On the back of the kerbal, or the back of the jetpack if the kerbal has a jetpack.
### leftFoot
The left foot of the kerbal.
### rightFoot
The right foot of the kerbal.
### leftBicep
The left bicep of the kerbal.
### rightBicep
The right bicep of the kerbal.
### head
The head of the kerbal.
### neck
The neck of the kerbal.
### chest
The chest of the kerbal.
### waist
The waist of the kerbal.
### leftShoulder
The left shoulder of the kerbal.
### rightShoulder
The right shoulder of the kerbal.
### leftForearm
The left forearm of the kerbal.
### rightForearm
The right forearm of the kerbal.
### leftHand
The left hand of the kerbal.
### rightHand
The right hand of the kerbal.
### leftPalm
The left palm of the kerbal.
### rightPalm
The right palm of the kerbal.
### leftThigh
The left thigh of the kerbal.
### rightThigh
The right thigh of the kerbal.
### leftCalf
The left calf of the kerbal.
### rightCalf
The right calf of the kerbal.
### leftToes
The left toes of the kerbal.
### rightToes
The right toes of the kerbal.

# KerbalGear.WBIModuleWearableItem
            
This module represents an equippable cargo item that appears as a 3D model on the kerbal. When equipping the item, this part module can also request one or more part modules on the kerbal that provide various abilities. For example, an item can request the WBIModuleEVAOverrides to improve the kerbal's swim speed. The requested part modules and their multiplicity are registered in KERBAL_EVA_MODULES config nodes and are created dynamically while needed. EVA_PART_MODULE child nodes request modules and may override their runtime configuration for this wearable. You can have more than one WBIModuleWearableItem part module per cargo part.
            
            
> #### Example
```

               MODULE
               {
                    name = WBIModuleWearableItem
                    moduleID = SCUBA Tank
                    bodyLocation = back
                    anchorTransform = scubaTank
                    meshTransform = tankMesh
                    positionOffset = 0.0000, 0.0200, 0.0900
                    positionOffsetJetpack = 0,0,0
                    rotationOffset = -70.0000, 0.0000, 0.0000
                    showChuteTransforms = false
                    hideStockPacksWhenWorn = true
                    hideTransformsWhenWorn = displayModel;groundBase
                    EVA_PART_MODULE
                    {
                        name = WBIModuleEVADiveComputer
                    }
               }
            
```

            
        
## Fields

### moduleID
ID of the module. This should be unique to the part.
### bodyLocation
Where to place the item, such as on the back of the kerbal, the end of the backpack. etc. See [[BodyLocations|KerbalGear.BodyLocations]].
### anchorTransform
Name of the high-level anchor transform. This will follow the bodyLocation bone as it moves.
### meshTransform
Name of the 3D model. This will be rotated and positioned relative to the anchorTransform.
### hideMeshTransformWhenDropped
When true, hides meshTransform on the physical part while it is dropped in flight. The wearable copy remains visible on the Kerbal. Defaults to false.
### hideTransformsWhenWorn
Semicolon-delimited names of model transforms to hide on the wearable copy while this part is carried in a Kerbal's inventory. Transform names are case-sensitive. The source part prefab is not changed, so these transforms remain visible when the part is dropped. This field may be placed on a hide-only WBIModuleWearableItem that does not specify an anchorTransform or meshTransform.
### hideStockPacksWhenWorn
When true, hides the Kerbal's stock backpack, storage pack, jetpack/chute pack, and chute models while this item is worn. Defaults to true.
### positionOffset
Position offsets (x,y,z).
### positionOffsetJetpack
Position offset that is used when the kerbal has a jetpack in addition to the wearable item (x,y,z). Requires bodyLocation = backOrJetpack
### rotationOffset
Rotation offsets in degrees
### showChuteTransforms
Flag to indicate whether the compact stock ChuteStTransform should remain visible while this wearable item is equipped on the kerbal's back. The kerbal must also be carrying the stock evaChute inventory part.
### evaModules
Legacy list of part modules to create on the kerbal when you equip the wearable item. Separate names with a semicolon. Prefer EVA_PART_MODULE child nodes for new configs.
## Methods


### OnStart(PartModule.StartState)
Hides the configured wearable mesh on a physical part loaded in flight.

### OnPartCreatedFomInventory(ModuleInventoryPart)
Ensures the configured wearable mesh is hidden immediately when stock inventory creates the physical dropped part.

### OnLoad(ConfigNode)
Loads configurable EVA module requests. Each EVA_PART_MODULE node names a module registered by KERBAL_EVA_MODULES and may override that definition's runtime fields.
> #### Parameters
> **node:** The wearable item's MODULE configuration.


### GetEVAPartModuleConfigs
Gets copies of the explicitly configured EVA_PART_MODULE requests.

### RequestsEVAModule(System.String)
Reports whether this wearable requests the named EVA module through either the new EVA_PART_MODULE nodes or the legacy semicolon-delimited evaModules field.

# KerbalGear.IKerbalGearInventoryListener
            
Receives a single notification after KerbalGear has reconciled an EVA inventory change. Implement this interface when an active EVA module needs to refresh data derived from inventory contents without being deactivated and reactivated.
        
## Methods


### OnKerbalGearInventoryChanged(ModuleInventoryPart)
Refreshes inventory-derived state after the EVA inventory reaches its final state.
> #### Parameters
> **inventory:** The EVA inventory whose contents changed.


# KerbalGear.IKerbalGearProviderListener
            
Receives the exact inventory providers assigned to a dynamic KerbalGear module instance. Implement this interface when module behavior or state belongs to particular carried items.
        
## Methods


### OnKerbalGearProvidersChanged(ModuleInventoryPart,WildBlueCore.KerbalGear.KerbalGearModuleProvider[])
Refreshes provider-specific state after KerbalGear reconciles the EVA inventory.
> #### Parameters
> **inventory:** The EVA inventory containing the providers.

> **providers:** The providers assigned according to the configured module mode.


# KerbalGear.KerbalGearModuleProvider
            
Identifies one stored cargo stack that requests a dynamic EVA module.
        
## Properties

### ProviderKey
Gets the stable provider key used while reconciling and persisting dynamic modules.
### SlotIndex
Gets the current inventory slot.
### StoredPart
Gets the stored cargo stack represented by this provider.
### ModuleConfig
Gets the wearable-specific overrides for the requested EVA module.
## Methods


### Constructor
Creates a provider descriptor for a stored cargo stack.
> #### Parameters
> **providerKey:** The stable reconciliation key.

> **slotIndex:** The current inventory slot.

> **storedPart:** The stored cargo stack.


# KerbalGear.SWearableProp
            
Represents an instance of a wearable prop. One SWearableProp corresponds to a part's WBIModuleWearableItem part module. Since WBIModuleWearableItem is created in relation to the part prefab, we use SWearableProp per kerbal on EVA.
        
## Fields

### prop
The game object representing the prop.
### meshTransform
The physical prop mesh.
### name
Name of the prop.
### partName
Name of the part containing the prop
### bodyLocation
Location of the prop on the kerbal's body.
### positionOffset
Position offset of the prop.
### positionOffsetJetpack
Position offset of the prop if the kerbal has a jetpack and bodyLocation is backOrJetpack.
### rotationOffset
Rotation offset of the prop.
### showChuteTransforms
Flag to indicate whether the compact stock ChuteStTransform should remain visible while the prop is equipped on the kerbal's back.

# KerbalGear.WBIModuleWearablesController
            
A utility class to handle wearable items and the part modules associated with them. This part module is added to a kerbal via a KERBAL_EVA_MODULES config node, NOT a standard KSP part.
            
            
> #### Example
```

            KERBAL_EVA_MODULES
            {
                MODULE
                {
                    name = WBIModuleWearablesController
                    debugMode = false
                }
            }
            
```

            
        
## Fields

### debugMode
Flag to turn on/off debug mode.
## Methods


### OnLoad(ConfigNode)
Restores state saved for dynamic modules. The modules themselves are created after the live EVA inventory becomes available during OnStart.
> #### Parameters
> **node:** The controller's saved configuration.


### OnSave(ConfigNode)
Persists each live dynamic module inside the always-present controller so its state can be restored even though the ability module is intentionally absent from the EVA prefab.
> #### Parameters
> **node:** The controller's save node.


### OnStart(PartModule.StartState)
Initializes wearable visuals and creates only the EVA modules requested by carried gear.
> #### Parameters
> **state:** KSP's current startup state.


### OnUpdate
Coalesces stock inventory events and keeps wearable meshes synchronized.

### LateUpdate
Applies wearable pack visibility after stock KerbalEVA has updated its own pack models.

### OnDestroy
Stops listening to stock inventory events when this EVA controller is destroyed. Module teardown is left to KSP so scene changes do not generate duplicate OnInactive calls.

### ShowPropOffsetView
Debug button that shows the prop offset view.

### onModuleInventorySlotChanged(ModuleInventoryPart,System.Int32)
Queues the same coalesced refresh used by the general inventory-changed event.
> #### Parameters
> **partInventory:** The inventory whose slot changed.

> **slotIndex:** The changed inventory slot.


### reconcileInventoryDeferred
Runs the pending inventory reconciliation after the current frame's module update pass. KSP iterates Part.Modules by index inside Part.ModulesOnUpdate, so changing that list from this controller's OnUpdate can throw an ArgumentOutOfRangeException on the next module.

### isCargoPartHeld
Reports whether stock inventory UI is currently holding a cargo part between source and destination slots. KerbalGear must not add or remove EVA modules during that transaction, because the stock cargo UI keeps the held Part and source slot state in static fields.

### reconcileInventory
Reconciles wearable props and EVA modules with the inventory's current contents. The desired module map is rebuilt from scratch so duplicate stock events cannot corrupt lifetime counts or provider ownership.

### buildDesiredEVAModules(System.Collections.Generic.Dictionary{System.String,System.Collections.Generic.List{WildBlueCore.KerbalGear.KerbalGearModuleProvider}})
Converts provider requests into concrete module instances according to each registered module's Exclusive, Aggregate, or PerProvider policy.
> #### Parameters
> **moduleProviders:** Providers grouped by requested PartModule class.

> #### Return value
> The exact dynamic module instances required by the current inventory.

### isProviderAware(WildBlueCore.KerbalGear.KerbalGearModuleDefinition)
Provider-aware aggregate modules receive all contributing configurations directly and therefore use their registered defaults instead of inheriting the first provider's EVA_PART_MODULE overrides. For example, WBIModuleEVAExperienceEffects receives every carried item's provider descriptor and combines duplicate RepairSkill requests into one active effect using the highest requested tier.

### addDesiredModule(System.Collections.Generic.Dictionary{System.String,WildBlueCore.KerbalGear.WBIModuleWearablesController.DesiredEVAModule},WildBlueCore.KerbalGear.KerbalGearModuleDefinition,System.String,WildBlueCore.KerbalGear.KerbalGearModuleProvider[],ConfigNode)
Adds one concrete module request to the desired instance map.

### mergeModuleConfig(ConfigNode,ConfigNode)
Applies wearable-specific values and child nodes over the registered module defaults. Child nodes with matching names are replaced as a group, which supports curves such as atmosphereCurve without combining incompatible keys from two definitions.

### warnAboutConflictingConfigs(WildBlueCore.KerbalGear.KerbalGearModuleDefinition,System.Collections.Generic.List{WildBlueCore.KerbalGear.KerbalGearModuleProvider})
Aggregate modules use the first inventory provider's configuration. Warn once when another provider requests the same shared instance with different overrides.

### getAggregateInstanceKey(WildBlueCore.KerbalGear.KerbalGearModuleDefinition)
Creates the stable key used by the single Aggregate module instance.

### getProviderKey(System.Int32,StoredPart)
Creates a provider key that survives inventory slot moves whenever the stored snapshot has a persistent KSP part identifier.

### reconcileWearableProps(System.Collections.Generic.HashSet{System.String})
Shows only the wearable props represented by parts currently stored in the EVA inventory.
> #### Parameters
> **storedPartNames:** Unique part names currently present in the inventory.


### addEVAModule(WildBlueCore.KerbalGear.WBIModuleWearablesController.DesiredEVAModule)
Creates, restores, starts, and activates one requested EVA module.
> #### Parameters
> **desiredModule:** The desired instance and its assigned providers.


### removeEVAModule(System.String)
Deactivates and destroys one module instance after its last applicable request disappears.
> #### Parameters
> **instanceKey:** The dynamic module instance key.


### notifyModuleProviders(WildBlueCore.KerbalGear.WBIModuleWearablesController.ActiveEVAModule,System.Boolean)
Notifies a retained or newly created module about the providers assigned by its mode. Provider-aware modules receive exact ownership; legacy aggregate modules receive the coalesced inventory notification used by the existing KerbalGear API.
> #### Parameters
> **activeModule:** The live dynamic module.

> **notifyLegacyListener:** Whether an existing inventory-only listener should receive a retained-module refresh.


### getStartState
Maps the live EVA vessel situation to the startup state expected by a new PartModule.

### hideTransforms(UnityEngine.GameObject,System.Collections.Generic.HashSet{System.String})
Hides each configured transform found within a wearable prop hierarchy. A part can create multiple props, so names that do not occur in this particular clone are intentionally ignored.
> #### Parameters
> **prop:** The instantiated wearable prop.

> **hiddenTransformNames:** Case-sensitive transform names to hide.


# KerbalGear.WBIModuleWearablesController.WearableEVAModuleRequest
            
Stores a cargo prefab's request and whether it came from the preferred node syntax.
        

# KerbalGear.WBIModuleWearablesController.ActiveEVAModule
            
Tracks one module instance created and owned by this controller.
        

# KerbalGear.WBIModuleWearablesController.DesiredEVAModule
            
Describes one module instance required by the current inventory contents.
        

# WBIBasePartModule
            
This is a simple base class that defines common functionality. Part modules should derive from it; it's not intended to be used directly in a part config.
            
            
> #### Example
```

            MODULE
            {
                name = WBIBasePartModule
                moduleId = warpEngine
                debugMode = true
            }
            
```

            
        
## Fields

### debugMode
Flag to indicate whether or not the module is in debug mode.
### moduleID
ID of the module. Used to find the proper config node.
## Methods


### getPartConfigNode(System.String)
Retrieves the module's config node from the part config.
> #### Parameters
> **className:** Optional. The name of the part module to search for.

> #### Return value
> A ConfigNode for the part module.

### loadCurve(FloatCurve,System.String,ConfigNode)
Loads the desired FloatCurve from the desired config node.
> #### Parameters
> **curve:** The FloatCurve to load

> **curveNodeName:** The name of the curve to load

> **defaultCurve:** An optional default curve to use in case the curve's node doesn't exist in the part module's config.


# WBIModulePowerUnitConverter
            
This module converts Power Units from Breaking Ground Science to Electric Charge and vice-versa.
            
            
> #### Example
```

            MODULE
            {
                name = WBIModulePowerUnitConverter
                isActive = true
                isConsuming = false
                ecPerPowerUnit = 0.25
                maxPowerUnitsProduced = 10
            }
            
```

            
        
## Fields

### isActive
Indicates whether or not the converter is running.
### isConsuming
Indicates whether or not the converter is consuming (true) or sharing (false).
### maxPowerAvailable
The maximum number of Power Units that the part may produce. This value ranges between 1 and maxPowerUnitsProduced.
### ecPerPowerUnit
In Breaking Ground Science, Power Unit is an integer, but resources like ElectricCharge use decimals. The default is 0.25, so 1.0 EC = 4 PU. This number was derived by comparing the size of the Breaking Ground Mini-NUK-PB RTG to the stock PB-NUK RTG, and looking how how much ElectricCharge the stock RTG produces. That actually gives us 0.375 (the Mini-NUK is about half as tall as the stock RTG), but we dropped that to 0.25 to make the math easier.
### maxPowerUnitsProduced
The maximum number of Power Units that the converter can provide. Note that this is an integer value. The default is 10. Multiply by ecPerPowerUnit to calculate how much ElectricCharge/sec that the power converter will consume. If you leave focus on the vessel and come back, then the E.C. will be drained accordingly.
### lastUpdated
Timestamp of the last time the module was updated.
## Properties

### CanDistributeEC
Indicates whether or not the power converter can distribute Electric Charge.
### CanConsumeEC
Indicates whether or not the power converter can consume Electric Charge.
## Methods


### GetPowerAvailable(System.Int32,System.Double)
Returns the number of Power Units available.
> #### Parameters
> **totalConverterCount:** An int containing the total number of converters to distribute power to.

> **deltaTime:** A double containing the current time duration.

> #### Return value
> An int containing the total Power Units available.

### DistributePower(System.Single)
Asks the converter to convert the supplied available power into Electric Charge and distribute it throughout the vessel.
> #### Parameters
> **availablePower:** An int containing the total Power Units to distribute.


# WBIModulePowerUnitDistributor
            
Manages power unit to electric charge distribution. Add this module to parts with a ModuleGroundExpControl (Probodobodyne Experiment Control Station is one example)
            
            
> #### Example
```

            MODULE
            {
                name = WBIModulePowerUnitDistributor
            }
            
```

            
        

# WBIModuleIVAVariants
            
This class works in conjunction with the stock ModulePartVariants. When the event onVariantApplied is received from the same part that has WBIModuleIVAVariants, and the name of the new variant matches the name of one of WBIModuleIVAVariants' VARIANT nodes, then the GAMEOBJECTS in the node will be enabled/disabled accordingly. The meshes must appear in the IVA meshes or in the depth mask. The format of the IVA's VARIANT node follows the same format of ModulePartVariants.
            
            
> #### Example
```

            MODULE
            {
                name = WBIModuleIVAVariants
                VARIANT
                {
                    name = Rover
                    GAMEOBJECTS
                    {
                        roverCeilingMed = true
                        stationCeilingMed = false
                        roverMask = true
                        stationMask = false
                        superstructureMask = false
                    }
                }
            
```

            
        
## Fields

### selectedVariant
The currently selected IVA Variant.

# WBIModulePartGridVariants
            
This is a specialized class that creates a two-dimensional grid of meshes from a collection of meshes provided by the model. While it is possible to duplicate multiple copies of a single transform, research shows that the part's radial attachment system gets messed up when you do that. So for now, we have a grid that is limited by the total number of meshes in the model.
            
            
> #### Example
```

            MODULE
            {
                name = WBIModulePartGridVariants
                totalRows = 6
                totalColumns = 6
                elementTransformName = yardFrameAngled37-30
                elementLength = 3.75
                elementWidth = 3.75
                elementHeight = 0.1875
            }
            
```

            
        
## Fields

### elementTransformName
Base name of the meshes found in the part's model object. All model transforms start with this prefix. Individual elements in the mesh should have " (n)" appended to them. NOTE: Be sure to have a total number of elements equal to totalRows * totalColumns and be sure to label them from (0) to (totalElements - 1) Example: yardFrameFlat37 (0), yardFrameFlat37 (1) ... yardFrameFlat37 (35) Note that there is a space between the prefix and the element id.
### elementLength
Length of a single element, in meters.
### elementWidth
Width of a single element, in meters.
### elementHeight
Height of a single element, in meters.
### totalRows
Total number of rows that are possible in the grid.
### totalColumns
Total number of columns that are possible in the grid.
### rowIndex
Current selected row variant.
### columnIndex
Current selected column variant.
## Methods


### copyOriginalNodes(System.Collections.Generic.List{AttachNode})
Called when the part was copied in the editor.
> #### Parameters
> **copyNodes:** The list of AttachNode objects to copy into our originalNodes field.


# WBIModuleResourceVariants
            
A small helper class to update a part's resources when a part variant is applied. ModulePartVariants defines one or more VARIANT config nodes, and each node can have a EXTRA_INFO within its config. EXTRA_INFO uses key/value pairs to define its data. WBIModuleResourceVariants can also define its own VARIANT nodes. When ModulePartVariants fires its onVariantApplied event, and the name of the event matches one of WBIModuleResourceVariants's VARIANT nodes, then WBIModuleResourceVariants's variant will be applied. Currently WBIModuleResourceVariants only supports RESOURCE nodes in its VARIANT node.
            
ModulePartVariants can define EXTRA_INFO as part of its VARIANT node, and WBIModuleResourceVariants can read some of the values defined in the EXTRA_INFO. here's an example:  
            
            
> #### Example
```

            MODULE
            {
                name = ModulePartVariants
                ...
                VARIANT
                {
                    name = someVariantName
                    ...
                    EXTRA_INFO
                    {
                        // The name of a single resource to modify on the part.
                        resourceName = IntakeLqd
                        
                        // The new amount of resource that will be applied to the part's resource. This can only happen in the VAB/SPH.
                        amount = 500
                        
                        // The new maximum amount of resource that will be applied to the part's resource. This can happen both in the VAB/SPH and in flight.
                        maxAmount = 500
                        
                        // If the part has a ModuleInventoryPart, then its storage limit will be updated. Similarly, if the part has a WBIOmniStorage part module, then
                        // its maximum storage volume will be updated as well.
                        packedVolumeLimit = 200
                        
                        // This only applies to ModuleInventoryPart and WBIOmniStorage. It computes their new storage volume, in liters, by multiplying resourceVolume by volumeMultiplier.
                        volumeMultiplier = 5
                    }
                }
            }
            
```

            
            
To define WBIModuleResourceVariants:  
            
            
> #### Example
```

            MODULE
            {
                name = WBIModuleResourceVariants
                resourceVolume = 6000
                // You can specify resource variants for the part that will be applied when you change the part's variant.
                VARIANT
                {
                    // The name of this variant node must match the name of the VARIANT node specified in the part's ModulePartVariants.
                    name = someVariantName
                    RESOURCE
                    {
                        name = Snacks
                        amount = 600
                        maxAmount = 600
                    }
                    RESOURCE
                    {
                        name = FreshAir
                        amount = 60
                        maxAmount = 60
                    }
                }
            }
            
```

            
        
## Fields

### resourceVolume
Resource volume size, in liters, per unit of volume. When the extra info in onPartVariantApplied contains volumeMultiplier, resource and inventory part modules will be updated to reflect the change. In such a case, the new storage volume will be resourceVolume * volumeMultiplier.

# WBIModuleWheelSFX
            
This part module adds sound effects to wheels when their motors are engaged. Effects are defined via the standard EFFECT config node.
            
            
> #### Example
```

            MODULE
            {
                name = WBIModuleWheelSFX
                runningEffect = running
                revTime = 0.05
            }
            
```

            
        
## Fields

### runningEffect
The name of the effect to play when the wheel is running (motors are producing torque).
### revTime
How quickly, in %, to play the effect from 0 (fully off) to 1 (fully on)