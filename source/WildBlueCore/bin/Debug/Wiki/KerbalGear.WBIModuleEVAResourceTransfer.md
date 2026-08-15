            
Enables resource transfers between inventory parts and exposes selected inventory resources through the EVA Kerbal's stock vessel-resource interface.
        
## Fields

### managedResourceNames
Names of transient proxy resources created during the previous save session. This lets the module discard saved proxies before rebuilding them from inventory snapshots.
## Methods


### InventoryResourceContributor.GetAmount
Returns the resource amount represented by this inventory stack.

### InventoryResourceContributor.GetMaxAmount
Returns the resource capacity represented by this inventory stack.

### InventoryResourceContributor.SetAmount(System.Double)
Sets the stack's total amount while retaining a single per-item snapshot value.

### InventoryResourceAggregate.GetAmount
Calculates the current resource amount across all contributing inventory snapshots.

### InventoryResourceAggregate.GetMaxAmount
Calculates the resource capacity across all contributing inventory snapshots.

### OnStart(PartModule.StartState)
Initializes the EVA and inventory references used by the resource bridge.
> #### Parameters
> **state:** KSP's current part-module startup state.


### OnActive
Rebuilds inventory resource proxies when a carried item activates this EVA ability.

### OnInactive
Commits proxy changes and removes them when the enabling cargo item is unequipped.

### OnKerbalGearInventoryChanged(ModuleInventoryPart)
Rebuilds proxy contributors once after a retained KerbalGear module's inventory changes.
> #### Parameters
> **changedInventory:** The EVA inventory whose contents changed.


### FixedUpdate
Synchronizes stock resource requests with the contributing inventory snapshots.

### OnDestroy
Flushes inventory amounts and removes transient proxy resources during EVA teardown.

### GetModuleMass(System.Single,ModifierStagingSituation)
Cancels the resource mass represented by live proxies because ModuleInventoryPart already includes the same inventory resource mass.

### GetModuleMassChangeWhen
Reports that proxy mass can change whenever its resource amount changes.

### GetModuleCost(System.Single,ModifierStagingSituation)
Cancels the resource cost represented by live proxies because ModuleInventoryPart already includes the same inventory resource cost.

### GetModuleCostChangeWhen
Reports that proxy cost can change whenever its resource amount changes.

### EnsureInitialized
Lazily initializes references because the wearables controller can call OnActive before KSP invokes this module's OnStart method.

### RebuildResourceProxies
Recreates live Kerbal resources from the inventory types selected by provider parts.

### GetExposedResourceNames
Gets resource names carried by parts that activate WBIModuleEVAResourceTransfer.

### IsResourceProviderPart(System.String)
Reports whether a cargo part uses WBIModuleWearableItem to activate this module.

### BuildResourceAggregates(System.Collections.Generic.HashSet{System.String})
Builds contribution lists for selected resource types across the entire EVA inventory.

### CaptureResourceProxies
Captures live proxy objects so inventory refreshes can reuse them without rebuilding PAW rows.

### ReconcileResourceProxies(System.Collections.Generic.Dictionary{System.String,PartResource})
Reuses proxies whose resource types remain exposed, removes stale proxies, and creates new types.

### SynchronizeResourceProxies
Applies proxy resource changes to inventory snapshots and refreshes aggregate totals.

### SynchronizeResourceProxiesInternal
Performs synchronization while the caller owns the recursion guard.

### DrainContributors(WildBlueCore.KerbalGear.WBIModuleEVAResourceTransfer.InventoryResourceAggregate,System.Double)
Drains provider parts first, then uses the same ascending resourcePriorityOffset ordering that KerbalEVA uses for EVA Propellant.

### FillContributors(WildBlueCore.KerbalGear.WBIModuleEVAResourceTransfer.InventoryResourceAggregate,System.Double)
Fills contributors in the same deterministic priority order used for draining.

### RemoveResourceProxies
Synchronizes and removes all live proxy resources owned by this module.

### RemoveResourceProxiesInternal
Removes proxies while the caller owns the recursion guard.

### RemovePersistedResourceProxies
Removes proxy resources restored by KSP before rebuilding from authoritative snapshots.

