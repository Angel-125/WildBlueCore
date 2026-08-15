            
Receives a single notification after KerbalGear has reconciled an EVA inventory change. Implement this interface when an active EVA module needs to refresh data derived from inventory contents without being deactivated and reactivated.
        
## Methods


### OnKerbalGearInventoryChanged(ModuleInventoryPart)
Refreshes inventory-derived state after the EVA inventory reaches its final state.
> #### Parameters
> **inventory:** The EVA inventory whose contents changed.


