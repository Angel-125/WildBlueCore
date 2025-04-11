            
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

