            
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


### OnLoad(ConfigNode)
Loads configurable EVA module requests. Each EVA_PART_MODULE node names a module registered by KERBAL_EVA_MODULES and may override that definition's runtime fields.
> #### Parameters
> **node:** The wearable item's MODULE configuration.


### GetEVAPartModuleConfigs
Gets copies of the explicitly configured EVA_PART_MODULE requests.

### RequestsEVAModule(System.String)
Reports whether this wearable requests the named EVA module through either the new EVA_PART_MODULE nodes or the legacy semicolon-delimited evaModules field.

