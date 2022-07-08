            
This class works in conjunction with the stock ModulePartVariants. When the event onVariantApplied is received from the same part that has ModuleIVAVariants, and the name of the new variant matches the name of one of ModuleIVAVariants' VARIANT nodes, then the GAMEOBJECTS in the node will be enabled/disabled accordingly. The meshes must appear in the IVA meshes or in the depth mask. The format of the IVA's VARIANT node follows the same format of ModulePartVariants.
            
            
> #### Example
```

            MODULE
            {
                name = ModuleIVAVariants
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

