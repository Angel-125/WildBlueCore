            
A utility class to handle wearable items and the part modules associated with them. This part module is added to a kerbal via a KERBAL_EVA_MODULES config node, NOT a standard KSP part.
            
            
> #### Example
```

            KERBAL_EVA_MODULES
            {
                MODULE
                {
                    name = WBIModuleWearablesController
                    debugMode = false
                }
            }
            
```

            
        
## Fields

### debugMode
Flag to turn on/off debug mode.
## Methods


### OnLoad(ConfigNode)
Restores state saved for dynamic modules. The modules themselves are created after the live EVA inventory becomes available during OnStart.
> #### Parameters
> **node:** The controller's saved configuration.


### OnSave(ConfigNode)
Persists each live dynamic module inside the always-present controller so its state can be restored even though the ability module is intentionally absent from the EVA prefab.
> #### Parameters
> **node:** The controller's save node.


### OnStart(PartModule.StartState)
Initializes wearable visuals and creates only the EVA modules requested by carried gear.
> #### Parameters
> **state:** KSP's current startup state.


### OnUpdate
Coalesces stock inventory events and keeps wearable meshes synchronized.

### LateUpdate
Applies wearable pack visibility after stock KerbalEVA has updated its own pack models.

### OnDestroy
Stops listening to stock inventory events when this EVA controller is destroyed. Module teardown is left to KSP so scene changes do not generate duplicate OnInactive calls.

### ShowPropOffsetView
Debug button that shows the prop offset view.

### onModuleInventorySlotChanged(ModuleInventoryPart,System.Int32)
Queues the same coalesced refresh used by the general inventory-changed event.
> #### Parameters
> **partInventory:** The inventory whose slot changed.

> **slotIndex:** The changed inventory slot.


### reconcileInventoryDeferred
Runs the pending inventory reconciliation after the current frame's module update pass. KSP iterates Part.Modules by index inside Part.ModulesOnUpdate, so changing that list from this controller's OnUpdate can throw an ArgumentOutOfRangeException on the next module.

### isCargoPartHeld
Reports whether stock inventory UI is currently holding a cargo part between source and destination slots. KerbalGear must not add or remove EVA modules during that transaction, because the stock cargo UI keeps the held Part and source slot state in static fields.

### reconcileInventory
Reconciles wearable props and EVA modules with the inventory's current contents. The desired module map is rebuilt from scratch so duplicate stock events cannot corrupt lifetime counts or provider ownership.

### buildDesiredEVAModules(System.Collections.Generic.Dictionary{System.String,System.Collections.Generic.List{WildBlueCore.KerbalGear.KerbalGearModuleProvider}})
Converts provider requests into concrete module instances according to each registered module's Exclusive, Aggregate, or PerProvider policy.
> #### Parameters
> **moduleProviders:** Providers grouped by requested PartModule class.

> #### Return value
> The exact dynamic module instances required by the current inventory.

### addDesiredModule(System.Collections.Generic.Dictionary{System.String,WildBlueCore.KerbalGear.WBIModuleWearablesController.DesiredEVAModule},WildBlueCore.KerbalGear.KerbalGearModuleDefinition,System.String,WildBlueCore.KerbalGear.KerbalGearModuleProvider[],ConfigNode)
Adds one concrete module request to the desired instance map.

### mergeModuleConfig(ConfigNode,ConfigNode)
Applies wearable-specific values and child nodes over the registered module defaults. Child nodes with matching names are replaced as a group, which supports curves such as atmosphereCurve without combining incompatible keys from two definitions.

### warnAboutConflictingConfigs(WildBlueCore.KerbalGear.KerbalGearModuleDefinition,System.Collections.Generic.List{WildBlueCore.KerbalGear.KerbalGearModuleProvider})
Aggregate modules use the first inventory provider's configuration. Warn once when another provider requests the same shared instance with different overrides.

### getAggregateInstanceKey(WildBlueCore.KerbalGear.KerbalGearModuleDefinition)
Creates the stable key used by the single Aggregate module instance.

### getProviderKey(System.Int32,StoredPart)
Creates a provider key that survives inventory slot moves whenever the stored snapshot has a persistent KSP part identifier.

### reconcileWearableProps(System.Collections.Generic.HashSet{System.String})
Shows only the wearable props represented by parts currently stored in the EVA inventory.
> #### Parameters
> **storedPartNames:** Unique part names currently present in the inventory.


### addEVAModule(WildBlueCore.KerbalGear.WBIModuleWearablesController.DesiredEVAModule)
Creates, restores, starts, and activates one requested EVA module.
> #### Parameters
> **desiredModule:** The desired instance and its assigned providers.


### removeEVAModule(System.String)
Deactivates and destroys one module instance after its last applicable request disappears.
> #### Parameters
> **instanceKey:** The dynamic module instance key.


### notifyModuleProviders(WildBlueCore.KerbalGear.WBIModuleWearablesController.ActiveEVAModule,System.Boolean)
Notifies a retained or newly created module about the providers assigned by its mode. Provider-aware modules receive exact ownership; legacy aggregate modules receive the coalesced inventory notification used by the existing KerbalGear API.
> #### Parameters
> **activeModule:** The live dynamic module.

> **notifyLegacyListener:** Whether an existing inventory-only listener should receive a retained-module refresh.


### getStartState
Maps the live EVA vessel situation to the startup state expected by a new PartModule.

### hideTransforms(UnityEngine.GameObject,System.Collections.Generic.HashSet{System.String})
Hides each configured transform found within a wearable prop hierarchy. A part can create multiple props, so names that do not occur in this particular clone are intentionally ignored.
> #### Parameters
> **prop:** The instantiated wearable prop.

> **hiddenTransformNames:** Case-sensitive transform names to hide.


