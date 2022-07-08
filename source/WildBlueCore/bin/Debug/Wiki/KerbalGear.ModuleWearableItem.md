            
This module represents an equippable cargo item that appears as a 3D model on the kerbal. When equipping the item, this part module can also activate one or more part modules on the kerbal that provide various abilities. For example, an item can activate the ModuleEVAOverrides to improve the kerbal's swim speed. The activated part modules are defined in KERBAL_EVA_MODULES config nodes. You can have more than one ModuleWearableItem part module per cargo part.
            
            
> #### Example
```

               MODULE
               {
                    name = ModuleWearableItem
                    moduleID = SCUBA Tank
                    bodyLocation = back
                    anchorTransform = scubaTank
                    meshTransform = tankMesh
                    positionOffset = 0.0000, 0.0200, 0.0900
                    positionOffsetJetpack = 0,0,0
                    rotationOffset = -70.0000, 0.0000, 0.0000
                    evaModules = ModuleEVADiveComputer
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
### evaModules
Name of the part modules to enable on the kerbal when you equip the wearable item. Separate names with a semicolon.

