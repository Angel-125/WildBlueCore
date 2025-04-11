            
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

