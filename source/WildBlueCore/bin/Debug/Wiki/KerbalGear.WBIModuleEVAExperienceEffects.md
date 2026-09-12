            
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

