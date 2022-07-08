# WildBlueCore


# PartModules.Resources.ModuleFuelPump
            
This part module pumps one or more resources from the host part to other parts that have the same resource. The module can be directly added to a resource tank part or to a part that is radially attached to a resource tank part. When enabled, ModuleFuelPump will automatically pump resources until either the host part's resource is empty or when the destination parts are full. In either case, it will wait until the host part gains more resources to pump or the destination parts gain more room to store the resource.
            
ModuleFuelPump will transfer resources based on a part's Flow Priority. Higher priority parts will receive resources before lower priority parts.  
            
ModuleFuelPump is designed to pump resources throughout the same vessel, but it can also pump resources to a nearby vessel if it is also equipped with a part that has a ModuleFuelPump.  
            
To pump a resource throughout the same vessel, the following conditions must be met:  
            * The fuel pump providing resources must be set to Enabled.  
            * The resource must not be empty.  
            * The resource must be transferrable and unlocked.  
            * The destination parts must have space available to receive the pumped resource.  
            * The destination parts' resource storage must be unlocked.  
            
To pump a resource to another nearby vessel, in addition to the above conditions, the following conditions must also be met:  
            * All vessels must be either landed or splashed.  
            * The nearby vessels must be within the provider's pump range.  
            * The pump providing resources must have its pump mode set to Remote.  
            * The pumps that receive resources must be set to Enabled.  
            
            
> #### Example
```

            MODULE
            {
                name = ModuleFuelPump
                maxRemotePumpRange = 200
            }
            
```

            
        
## Fields

### maxRemotePumpRange
In meters, the maximum range that the fuel pump can reach when remote pumping resources. Default is 200 meters.
## Methods


### DistributeResources
This method will attempt to distribute any resources that the host part has to other parts in the vessel or to nearby vessels. The resources must be capable of being transferred, and they must be unlocked. Additionally, to remotely distribute the resources, remotePumpMode must be set to true, the nearby vessel must have at least one ModuleFuelPump, and the nearby vessel's fuel pump' isActivated must be set to true.

### DistributeResourceLocally(PartResource,System.Double,System.Boolean)
Distributes the desired resource locally throughout the vessel.
> #### Parameters
> **resource:** The PartResource to distribute.

> **transferAmount:** A double containing how much of the resource to distrubute.

> **isFromRemotePump:** A bool indicating whether or not the source is from a remote pump. Default is false.

> #### Return value
> True if the distribution was successful, false if not.

# KerbalGear.ModuleKerbalEVAModules
            
Special thanks to Vali for figuring out this issue! :) The Vintage, Standard, and Future suits are all defined in separate part modules that are combined when KSP starts. The problem is that when Module Manager is used to add part modules to the kerbal, you'll get duplicates. One solution is to disable or outright remove the duplicate part module, but we have several part modules to manage. So to get around that problem, the ModuleKerbalEVAModules adds a custom LoadingSystem that adds any part modules defined by a KERBAL_EVA_MODULES node to the kerbals. Simply define a KERBAL_EVA_MODULES config node with one or more standard MODULE config nodes, and they'll be added to the kerbals.
            
            
> #### Example
```

            KERBAL_EVA_MODULES
            {
                MODULE
                {
                    name = ModuleWearablesController
                    debugMode = false
                }
                
                MODULE
                {
                    name = ModuleEVAOverrides
                }
            }
            
```

            
        

# KerbalGear.ModuleKerbalEVAModules.EVAModulesLoader
            
An internal helper class that reads KERVAL_EVA_MODULES for MODULE nodes to add to a kerbal.
        

# KerbalGear.ModuleSuitSwitcher
            
This part module allows kerbals to change their outfits after the vessel leaves the VAB/SPH.
            
            
> #### Example
```

            MODULE
            {
                name = ModuleSuitSwitcher
            }
            
```

            
        
## Methods


### OpenWardrobe
Opens the wardrobe GUI.

# KerbalGear.BodyLocations
            
Various locations where an wearable item can be placed. This is primarily used for ModuleWearableItem.
        
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

# KerbalGear.ModuleWearableItem
            
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

# KerbalGear.SWearableProp
            
Represents an instance of a wearable prop. One SWearableProp corresponds to a part's ModuleWearableItem part module. Since ModuleWearableItem is created in relation to the part prefab, we use SWearableProp per kerbal on EVA.
        
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

# KerbalGear.ModuleWearablesController
            
A utility class to handle wearable items and the part modules associated with them. This part module is added to a kerbal via a KERBAL_EVA_MODULES config node, NOT a standard KSP part.
            
            
> #### Example
```

            KERBAL_EVA_MODULES
            {
                MODULE
                {
                    name = ModuleWearablesController
                    debugMode = false
                }
            }
            
```

            
        
## Fields

### debugMode
Flag to turn on/off debug mode.
## Methods


### ShowPropOffsetView
Debug button that shows the prop offset view.

# BasePartModule
            
This is a simple base class that defines common functionality. Part modules should derive from it; it's not intended to be used directly in a part config.
            
            
> #### Example
```

            MODULE
            {
                name = BasePartModule
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


# ModulePowerUnitConverter
            
This module converts Power Units from Breaking Ground Science to Electric Charge and vice-versa.
            
            
> #### Example
```

            MODULE
            {
                name = ModulePowerUnitConverter
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


# ModulePowerUnitDistributor
            
Manages power unit to electric charge distribution. Add this module to parts with a ModuleGroundExpControl (Probodobodyne Experiment Control Station is one example)
            
            
> #### Example
```

            MODULE
            {
                name = ModulePowerUnitDistributor
            }
            
```

            
        

# ModuleIVAVariants
            
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

# ModulePartGridVariants
            
This is a specialized class that creates a two-dimensional grid of meshes from a collection of meshes provided by the model. While it is possible to duplicate multiple copies of a single transform, research shows that the part's radial attachment system gets messed up when you do that. So for now, we have a grid that is limited by the total number of meshes in the model.
            
            
> #### Example
```

            MODULE
            {
                name = ModulePartGridVariants
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


# ModuleResourceVariants
            
A small helper class to update a part's resources when a part variant is applied. ModulePartVariants defines one or more VARIANT config nodes, and each node can have a EXTRA_INFO within its config. EXTRA_INFO uses key/value pairs to define its data. ModuleResourceVariants can also define its own VARIANT nodes. When ModulePartVariants fires its onVariantApplied event, and the name of the event matches one of ModuleResourceVariants's VARIANT nodes, then ModuleResourceVariants's variant will be applied. Currently ModuleResourceVariants only supports RESOURCE nodes in its VARIANT node.
            
ModulePartVariants can define EXTRA_INFO as part of its VARIANT node, and ModuleResourceVariants can read some of the values defined in the EXTRA_INFO. here's an example:  
            
            
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

            
            
To define ModuleResourceVariants:  
            
            
> #### Example
```

            MODULE
            {
                name = ModuleResourceVariants
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

# ModuleWheelSFX
            
This part module adds sound effects to wheels when their motors are engaged. Effects are defined via the standard EFFECT config node.
            
            
> #### Example
```

            MODULE
            {
                name = ModuleWheelSFX
                runningEffect = running
                revTime = 0.05
            }
            
```

            
        
## Fields

### runningEffect
The name of the effect to play when the wheel is running (motors are producing torque).
### revTime
How quickly, in %, to play the effect from 0 (fully off) to 1 (fully on)