            
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

