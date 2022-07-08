# WildBlueCore #

#### Field KerbalGear.ModuleEVAOverrides.buoyancyOverride

 The buoyancy override 



---
#### Field KerbalGear.ModuleEVAOverrides.evaOverrideParts

 These inventory parts contain eva overrides that are specified by EVA_OVERRIDES nodes. 



---
#### Method KerbalGear.ModuleEVAOverrides.OnInactive

 Overrides OnInactive. Called when an inventory item is unequipped and the module is disabled. 



---
#### Method KerbalGear.ModuleEVAOverrides.OnActive

 Overrides OnActive. Called when an inventory item is equipped and the module is enabled. 



---
## Type KerbalGear.ModuleKerbalEVAModules

 Special thanks to Vali for figuring out this issue! :) The Vintage, Standard, and Future suits are all defined in separate part modules that are combined when KSP starts. The problem is that when Module Manager is used to add part modules to the kerbal, you'll get duplicates. One solution is to disable or outright remove the duplicate part module, but we have several part modules to manage. So to get around that problem, the ModuleKerbalEVAModules adds a custom LoadingSystem that adds any part modules defined by a KERBAL_EVA_MODULES node to the kerbals. Simply define a KERBAL_EVA_MODULES config node with one or more standard MODULE config nodes, and they'll be added to the kerbals. 

##### Example: 

######  code

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





---
## Type KerbalGear.ModuleKerbalEVAModules.EVAModulesLoader

 An internal helper class that reads KERVAL_EVA_MODULES for MODULE nodes to add to a kerbal. 



---
## Type KerbalGear.ModuleSuitSwitcher

 This part module allows kerbals to change their outfits after the vessel leaves the VAB/SPH. 

##### Example: 

######  code

```
    MODULE
    {
        name = ModuleSuitSwitcher
    }
```





---
#### Method KerbalGear.ModuleSuitSwitcher.OpenWardrobe

 Opens the wardrobe GUI. 



---
## Type KerbalGear.BodyLocations

 Various locations where an wearable item can be placed. This is primarily used for ModuleWearableItem. 



---
#### Field KerbalGear.BodyLocations.back

 On the back of the kerbal. 



---
#### Field KerbalGear.BodyLocations.backOrJetpack

 On the back of the kerbal, or the back of the jetpack if the kerbal has a jetpack. 



---
#### Field KerbalGear.BodyLocations.leftFoot

 The left foot of the kerbal. 



---
#### Field KerbalGear.BodyLocations.rightFoot

 The right foot of the kerbal. 



---
#### Field KerbalGear.BodyLocations.leftBicep

 The left bicep of the kerbal. 



---
#### Field KerbalGear.BodyLocations.rightBicep

 The right bicep of the kerbal. 



---
## Type KerbalGear.ModuleWearableItem

 This module represents an equippable cargo item that appears as a 3D model on the kerbal. When equipping the item, this part module can also activate one or more part modules on the kerbal that provide various abilities. For example, an item can activate the ModuleEVAOverrides to improve the kerbal's swim speed. The activated part modules are defined in KERBAL_EVA_MODULES config nodes. You can have more than one ModuleWearableItem part module per cargo part. 

##### Example: 

######  code

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





---
#### Field KerbalGear.ModuleWearableItem.moduleID

 ID of the module. This should be unique to the part. 



---
#### Field KerbalGear.ModuleWearableItem.bodyLocation

 Where to place the item, such as on the back of the kerbal, the end of the backpack. etc. See [[BodyLocations|KerbalGear.BodyLocations]]. 



---
#### Field KerbalGear.ModuleWearableItem.anchorTransform

 Name of the high-level anchor transform. This will follow the bodyLocation bone as it moves. 



---
#### Field KerbalGear.ModuleWearableItem.meshTransform

 Name of the 3D model. This will be rotated and positioned relative to the anchorTransform. 



---
#### Field KerbalGear.ModuleWearableItem.positionOffset

 Position offsets (x,y,z). 



---
#### Field KerbalGear.ModuleWearableItem.positionOffsetJetpack

 Position offset that is used when the kerbal has a jetpack in addition to the wearable item (x,y,z). Requires bodyLocation = backOrJetpack 



---
#### Field KerbalGear.ModuleWearableItem.rotationOffset

 Rotation offsets in degrees 



---
#### Field KerbalGear.ModuleWearableItem.evaModules

 Name of the part modules to enable on the kerbal when you equip the wearable item. Separate names with a semicolon. 



---
## Type KerbalGear.SWearableProp

 Represents an instance of a wearable prop. One SWearableProp corresponds to a part's ModuleWearableItem part module. Since ModuleWearableItem is created in relation to the part prefab, we use SWearableProp per kerbal on EVA. 



---
#### Field KerbalGear.SWearableProp.prop

 The game object representing the prop. 



---
#### Field KerbalGear.SWearableProp.meshTransform

 The physical prop mesh. 



---
#### Field KerbalGear.SWearableProp.name

 Name of the prop. 



---
#### Field KerbalGear.SWearableProp.partName

 Name of the part containing the prop 



---
#### Field KerbalGear.SWearableProp.bodyLocation

 Location of the prop on the kerbal's body. 



---
#### Field KerbalGear.SWearableProp.positionOffset

 Position offset of the prop. 



---
#### Field KerbalGear.SWearableProp.positionOffsetJetpack

 Position offset of the prop if the kerbal has a jetpack and bodyLocation is backOrJetpack. 



---
#### Field KerbalGear.SWearableProp.rotationOffset

 Rotation offset of the prop. 



---
## Type KerbalGear.ModuleWearablesController

 A utility class to handle wearable items and the part modules associated with them. This part module is added to a kerbal via a KERBAL_EVA_MODULES config node, NOT a standard KSP part. 

##### Example: 

######  code

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





---
#### Field KerbalGear.ModuleWearablesController.debugMode

 Flag to turn on/off debug mode. 



---
#### Method KerbalGear.ModuleWearablesController.ShowPropOffsetView

 Debug button that shows the prop offset view. 



---
## Type BasePartModule

 This is a simple base class that defines common functionality. Part modules should derive from it; it's not intended to be used directly in a part config. 

##### Example: 

######  code

```
    MODULE
    {
        name = BasePartModule
        moduleId = warpEngine
        debugMode = true
    }
```





---
#### Field BasePartModule.debugMode

 Flag to indicate whether or not the module is in debug mode. 



---
#### Field BasePartModule.moduleID

 ID of the module. Used to find the proper config node. 



---
#### Method BasePartModule.getPartConfigNode(System.String)

 Retrieves the module's config node from the part config. 

|Name | Description |
|-----|------|
|className: |Optional. The name of the part module to search for.|
**Returns**: A ConfigNode for the part module.



---
#### Method BasePartModule.loadCurve(FloatCurve,System.String,ConfigNode)

 Loads the desired FloatCurve from the desired config node. 

|Name | Description |
|-----|------|
|curve: |The FloatCurve to load|
|curveNodeName: |The name of the curve to load|
|defaultCurve: |An optional default curve to use in case the curve's node doesn't exist in the part module's config.|


---
## Type ModulePowerUnitConverter

 This module converts Power Units from Breaking Ground Science to Electric Charge and vice-versa. 

##### Example: 

######  code

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





---
#### Field ModulePowerUnitConverter.isActive

 Indicates whether or not the converter is running. 



---
#### Field ModulePowerUnitConverter.isConsuming

 Indicates whether or not the converter is consuming (true) or sharing (false). 



---
#### Field ModulePowerUnitConverter.maxPowerAvailable

 The maximum number of Power Units that the part may produce. This value ranges between 1 and maxPowerUnitsProduced. 



---
#### Field ModulePowerUnitConverter.ecPerPowerUnit

 In Breaking Ground Science, Power Unit is an integer, but resources like ElectricCharge use decimals. The default is 0.25, so 1.0 EC = 4 PU. This number was derived by comparing the size of the Breaking Ground Mini-NUK-PB RTG to the stock PB-NUK RTG, and looking how how much ElectricCharge the stock RTG produces. That actually gives us 0.375 (the Mini-NUK is about half as tall as the stock RTG), but we dropped that to 0.25 to make the math easier. 



---
#### Field ModulePowerUnitConverter.maxPowerUnitsProduced

 The maximum number of Power Units that the converter can provide. Note that this is an integer value. The default is 10. Multiply by ecPerPowerUnit to calculate how much ElectricCharge/sec that the power converter will consume. If you leave focus on the vessel and come back, then the E.C. will be drained accordingly. 



---
#### Field ModulePowerUnitConverter.lastUpdated

 Timestamp of the last time the module was updated. 



---
#### Property ModulePowerUnitConverter.CanDistributeEC

 Indicates whether or not the power converter can distribute Electric Charge. 



---
#### Property ModulePowerUnitConverter.CanConsumeEC

 Indicates whether or not the power converter can consume Electric Charge. 



---
#### Method ModulePowerUnitConverter.GetPowerAvailable(System.Int32,System.Double)

 Returns the number of Power Units available. 

|Name | Description |
|-----|------|
|totalConverterCount: |An int containing the total number of converters to distribute power to.|
|deltaTime: |A double containing the current time duration.|
**Returns**: An int containing the total Power Units available.



---
#### Method ModulePowerUnitConverter.DistributePower(System.Single)

 Asks the converter to convert the supplied available power into Electric Charge and distribute it throughout the vessel. 

|Name | Description |
|-----|------|
|availablePower: |An int containing the total Power Units to distribute.|


---
## Type ModulePowerUnitDistributor

 Manages power unit to electric charge distribution. Add this module to parts with a ModuleGroundExpControl (Probodobodyne Experiment Control Station is one example) 

##### Example: 

######  code

```
    MODULE
    {
        name = ModulePowerUnitDistributor
    }
```





---
## Type ModuleIVAVariants

 This class works in conjunction with the stock ModulePartVariants. When the event onVariantApplied is received from the same part that has ModuleIVAVariants, and the name of the new variant matches the name of one of ModuleIVAVariants' VARIANT nodes, then the GAMEOBJECTS in the node will be enabled/disabled accordingly. The meshes must appear in the IVA meshes or in the depth mask. The format of the IVA's VARIANT node follows the same format of ModulePartVariants. 

##### Example: 

######  code

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





---
#### Field ModuleIVAVariants.selectedVariant

 The currently selected IVA Variant. 



---
## Type ModulePartGridVariants

 This is a specialized class that creates a two-dimensional grid of meshes from a collection of meshes provided by the model. While it is possible to duplicate multiple copies of a single transform, research shows that the part's radial attachment system gets messed up when you do that. So for now, we have a grid that is limited by the total number of meshes in the model. 

##### Example: 

######  code

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





---
#### Field ModulePartGridVariants.elementTransformName

 Base name of the meshes found in the part's model object. All model transforms start with this prefix. Individual elements in the mesh should have " (n)" appended to them. NOTE: Be sure to have a total number of elements equal to totalRows * totalColumns and be sure to label them from (0) to (totalElements - 1) Example: yardFrameFlat37 (0), yardFrameFlat37 (1) ... yardFrameFlat37 (35) Note that there is a space between the prefix and the element id. 



---
#### Field ModulePartGridVariants.elementLength

 Length of a single element, in meters. 



---
#### Field ModulePartGridVariants.elementWidth

 Width of a single element, in meters. 



---
#### Field ModulePartGridVariants.elementHeight

 Height of a single element, in meters. 



---
#### Field ModulePartGridVariants.totalRows

 Total number of rows that are possible in the grid. 



---
#### Field ModulePartGridVariants.totalColumns

 Total number of columns that are possible in the grid. 



---
#### Field ModulePartGridVariants.rowIndex

 Current selected row variant. 



---
#### Field ModulePartGridVariants.columnIndex

 Current selected column variant. 



---
#### Method ModulePartGridVariants.copyOriginalNodes(System.Collections.Generic.List{AttachNode})

 Called when the part was copied in the editor. 

|Name | Description |
|-----|------|
|copyNodes: |The list of AttachNode objects to copy into our originalNodes field.|


---
## Type ModuleResourceVariants

 A small helper class to update a part's resources when a part variant is applied. ModulePartVariants defines one or more VARIANT config nodes, and each node can have a EXTRA_INFO within its config. EXTRA_INFO uses key/value pairs to define its data. ModuleResourceVariants can also define its own VARIANT nodes. When ModulePartVariants fires its onVariantApplied event, and the name of the event matches one of ModuleResourceVariants's VARIANT nodes, then ModuleResourceVariants's variant will be applied. Currently ModuleResourceVariants only supports RESOURCE nodes in its VARIANT node. 



> ModulePartVariants can define EXTRA_INFO as part of its VARIANT node, and ModuleResourceVariants can read some of the values defined in the EXTRA_INFO. here's an example: 

##### Example: 

######  code

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





> To define ModuleResourceVariants: 

##### Example: 

######  code

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





---
#### Field ModuleResourceVariants.resourceVolume

 Resource volume size, in liters, per unit of volume. When the extra info in onPartVariantApplied contains volumeMultiplier, resource and inventory part modules will be updated to reflect the change. In such a case, the new storage volume will be resourceVolume * volumeMultiplier. 



---
## Type ModuleWheelSFX

 This part module adds sound effects to wheels when their motors are engaged. Effects are defined via the standard EFFECT config node. 

##### Example: 

######  code

```
    MODULE
    {
        name = ModuleWheelSFX
        runningEffect = running
        revTime = 0.05
    }
```





---
#### Field ModuleWheelSFX.runningEffect

 The name of the effect to play when the wheel is running (motors are producing torque). 



---
#### Field ModuleWheelSFX.revTime

 How quickly, in %, to play the effect from 0 (fully off) to 1 (fully on) 



---
#### Method Wrappers.ModuleWaterfallFXWrapper.GetWaterfallModule(Part)

 Attempts to obtain the Waterfall FX module from the supplied part. 

|Name | Description |
|-----|------|
|part: |A Part that might contain a waterfall fx module|
**Returns**: A WFModuleWaterfallFX if the part has a waterfall module, or null if not.



---
#### Method Wrappers.ModuleWaterfallFXWrapper.#ctor(PartModule)

 Instantiates a new ModuleWaterfallFXWrapper 

|Name | Description |
|-----|------|
|module: |The PartModule representing the FX module.|


---
#### Method Wrappers.ModuleWaterfallFXWrapper.SetControllerOverride(System.String,System.Boolean)

 Sets the override state for the specified controller. 

|Name | Description |
|-----|------|
|controllerName: |A string containing the name of the controller to override.|
|overriden: |A bool indicating whether or not to override the controller.|


---
#### Method Wrappers.ModuleWaterfallFXWrapper.SetControllerOverrideValue(System.String,System.Single)

 Sets the override value for the specified controller 

|Name | Description |
|-----|------|
|controllerName: |A string containing the name of the controller to override.|
|value: |A float containing the override value.|


---
#### Method Wrappers.ModuleWaterfallFXWrapper.SetControllerValue(System.String,System.Single)

 Sets the value for the specified controller 

|Name | Description |
|-----|------|
|controllerName: |A string containing the name of the controller to override.|
|value: |A float containing the override value.|


---


