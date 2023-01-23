            
Helper part module to handle part mesh and texture switching. Stock ModulePartVariants doesn't cooperate with multiple ModulePartVariants in the same part, so this class gets around the issue and adds a few enhancements. When you define a ModulePartVariants, be sure to place its config node AFTER ModulePartVariants. When you define a ModulePartVariants, you can specify some EXTRA_INFO that SWPartVariants uses to configure itself:
            
            
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
                        // ModulePartSubvariants can be GUI enabled/disabled using the "enableVariantModuleIDs" and "disableVariantModuleIDs" fields, respectively.
                        // Simply specify the SWPartVariants' moduleID. For multiple moduleIDs, separate them with a semicolon.
                        disableVariantModuleIDs = mirroring
                        
                        // Similarly you can re-apply the ModulePartSubvariants' applied variant when this variant is applied.
                        updateVariantModuleIDs = texturing
                    }
                }
            }
            
```

            
            
To define a ModulePartSubvariants module:  
            
            
> #### Example
```

            MODULE
            {
                name = ModulePartSubvariants
                moduleID = texturing
                updateSymmetry = false
                allowFieldUpdate = false
                
                VARIANT
                {
                    displayName = #LOC_SUNKWORKS_yachtDeck
                    primaryColor = #caa472
                    secondaryColor = #caa472
                    // GAMEOBJECTS, EXTRA_INFO, etc. found in a typical ModulePartVariants are supported but omitted for brevity.
                    
                    // The textures will be applied to all the transforms named in the config node.
                    TEXTURES
                    {
                        mainTextureURL = WildBlueIndustries/SunkWorks/Parts/Structural/BoatHulls/boatHull1Yacht
                        bumpMapURL = WildBlueIndustries/SunkWorks/Parts/Structural/BoatHulls/boatHull1YachtNrm		
                        transformName = cargoKeelBowFull
                        transformName = cargoKeelBowInsert
                        transformName = cargoKeelBowPortHalf
                        transformName = cargoKeelBowStarboardHalf
                        // Add as many as you like
                        transformName = ...
                    }
                }
            }
            
```

            
        
## Fields

### baseVariant
Name of the variant to apply if we haven't selected a variant yet.
### variantIndex
Index for the texture variants.
### updateSymmetry
Flag to indicate if the symmetry parts should also apply the selected variant. Default is true.
### allowFieldUpdate
Flag to indicate whether the variant can be applied post launch. Default is false.
### variantApplied
Field indicating whether or not we have applied the part variant.
### meshSets
If, during a part variant update event, the meshSet field is set in EXTRA_INFO, then we'll record what the meshSet's value is and apply the set IF the value is on our list. If our meshSets is empty (the default), then we'll ignore any meshSet fields passed in with EXTRA_INFO.
### currentMeshSet
The currently selected mesh set.
## Methods


### OnStart(PartModule.StartState)
Handles the OnStart event.
> #### Parameters
> **state:** A StartState containing the starting state.


### OnAwake
Handles OnAwake event

### OnDestroy
Handles the OnDestroy event

### GetModuleDisplayName
Gets the module display name.
> #### Return value
> A string containing the display name.

### GetInfo
Gets the module description.
> #### Return value
> A string containing the module description.

### GetModuleCost(System.Single,ModifierStagingSituation)
Returns the Module cost modifier. It is added to the part's total cost.
> #### Parameters
> **defaultCost:** Default cost of the part

> **sit:** The situation in which the call is being made.

> #### Return value
> A float containing the modified cost.

### GetModuleCostChangeWhen
Describes when the part modifier changes.
> #### Return value
> A ModifierChangeWhen indicating when the modifier is applied.

### GetModuleMass(System.Single,ModifierStagingSituation)
Returns the Module cost modifier. It is added to the part's total mass.
> #### Parameters
> **defaultMass:** Default mass of the part

> **sit:** The situation in which the call is being made.

> #### Return value
> A float containing the modified mass.

### GetModuleMassChangeWhen
Describes when the part modifier changes.
> #### Return value
> A ModifierChangeWhen indicating when the modifier is applied.

