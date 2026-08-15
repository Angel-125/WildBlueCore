            
Receives the exact inventory providers assigned to a dynamic KerbalGear module instance. Implement this interface when module behavior or state belongs to particular carried items.
        
## Methods


### OnKerbalGearProvidersChanged(ModuleInventoryPart,WildBlueCore.KerbalGear.KerbalGearModuleProvider[])
Refreshes provider-specific state after KerbalGear reconciles the EVA inventory.
> #### Parameters
> **inventory:** The EVA inventory containing the providers.

> **providers:** The providers assigned according to the configured module mode.


