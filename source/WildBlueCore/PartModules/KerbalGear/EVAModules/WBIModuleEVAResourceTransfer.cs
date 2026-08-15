using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using WildBlueCore.KerbalGear;

namespace WildBlueCore.KerbalGear
{
    /// <summary>
    /// Enables resource transfers between inventory parts and exposes selected inventory
    /// resources through the EVA Kerbal's stock vessel-resource interface.
    /// </summary>
    public class WBIModuleEVAResourceTransfer : WBIBasePartModule, IPartMassModifier,
        IPartCostModifier, IKerbalGearInventoryListener
    {
        private const string ModuleName = "WBIModuleEVAResourceTransfer";
        private const double ResourceTolerance = 1E-08;

        /// <summary>
        /// Names of transient proxy resources created during the previous save session.
        /// This lets the module discard saved proxies before rebuilding them from inventory snapshots.
        /// </summary>
        [KSPField(isPersistant = true)]
        public string managedResourceNames = string.Empty;

        private sealed class InventoryResourceContributor
        {
            internal ProtoPartResourceSnapshot snapshot;
            internal bool isProvider;
            internal int priority;
            internal int quantity;
            internal int slotIndex;

            /// <summary>
            /// Returns the resource amount represented by this inventory stack.
            /// </summary>
            internal double GetAmount()
            {
                return snapshot.amount * quantity;
            }

            /// <summary>
            /// Returns the resource capacity represented by this inventory stack.
            /// </summary>
            internal double GetMaxAmount()
            {
                return snapshot.maxAmount * quantity;
            }

            /// <summary>
            /// Sets the stack's total amount while retaining a single per-item snapshot value.
            /// </summary>
            internal void SetAmount(double totalAmount)
            {
                snapshot.amount = Math.Max(0.0, Math.Min(GetMaxAmount(), totalAmount)) / quantity;
                snapshot.UpdateConfigNodeAmounts();
            }
        }

        private sealed class InventoryResourceAggregate
        {
            internal string resourceName;
            internal PartResourceDefinition definition;
            internal PartResource proxy;
            internal double lastProxyAmount;
            internal readonly List<InventoryResourceContributor> contributors =
                new List<InventoryResourceContributor>();

            /// <summary>
            /// Calculates the current resource amount across all contributing inventory snapshots.
            /// </summary>
            internal double GetAmount()
            {
                double amount = 0.0;
                for (int index = 0; index < contributors.Count; index++)
                    amount += contributors[index].GetAmount();
                return amount;
            }

            /// <summary>
            /// Calculates the resource capacity across all contributing inventory snapshots.
            /// </summary>
            internal double GetMaxAmount()
            {
                double maxAmount = 0.0;
                for (int index = 0; index < contributors.Count; index++)
                    maxAmount += contributors[index].GetMaxAmount();
                return maxAmount;
            }
        }

        private KerbalEVA kerbalEVA;
        private ModuleInventoryPart inventory;
        private readonly Dictionary<string, InventoryResourceAggregate> resourceAggregates =
            new Dictionary<string, InventoryResourceAggregate>();
        private readonly Dictionary<string, bool> providerPartCache = new Dictionary<string, bool>();
        private bool initialized;
        private bool synchronizing;

        /// <summary>
        /// Initializes the EVA and inventory references used by the resource bridge.
        /// </summary>
        /// <param name="state">KSP's current part-module startup state.</param>
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            EnsureInitialized();
            if (enabled && moduleIsEnabled)
                RebuildResourceProxies();
        }

        /// <summary>
        /// Rebuilds inventory resource proxies when a carried item activates this EVA ability.
        /// </summary>
        public override void OnActive()
        {
            base.OnActive();
            EnsureInitialized();
            RebuildResourceProxies();
        }

        /// <summary>
        /// Commits proxy changes and removes them when the enabling cargo item is unequipped.
        /// </summary>
        public override void OnInactive()
        {
            base.OnInactive();
            if (!initialized)
                return;

            SynchronizeResourceProxies();
            RemoveResourceProxies();
        }

        /// <summary>
        /// Rebuilds proxy contributors once after a retained KerbalGear module's inventory changes.
        /// </summary>
        /// <param name="changedInventory">The EVA inventory whose contents changed.</param>
        public void OnKerbalGearInventoryChanged(ModuleInventoryPart changedInventory)
        {
            EnsureInitialized();
            if (!initialized || changedInventory == null || changedInventory != inventory)
                return;

            RebuildResourceProxies();
        }

        /// <summary>
        /// Synchronizes stock resource requests with the contributing inventory snapshots.
        /// </summary>
        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || !initialized || !enabled || !moduleIsEnabled)
                return;

            SynchronizeResourceProxies();
        }

        /// <summary>
        /// Flushes inventory amounts and removes transient proxy resources during EVA teardown.
        /// </summary>
        public void OnDestroy()
        {
            if (!initialized)
                return;

            SynchronizeResourceProxies();
            RemoveResourceProxies();
        }

        /// <summary>
        /// Cancels the resource mass represented by live proxies because ModuleInventoryPart
        /// already includes the same inventory resource mass.
        /// </summary>
        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit)
        {
            double proxyMass = 0.0;
            foreach (InventoryResourceAggregate aggregate in resourceAggregates.Values)
            {
                if (aggregate.proxy != null && aggregate.definition != null)
                    proxyMass += aggregate.proxy.amount * aggregate.definition.density;
            }
            return (float)-proxyMass;
        }

        /// <summary>
        /// Reports that proxy mass can change whenever its resource amount changes.
        /// </summary>
        public ModifierChangeWhen GetModuleMassChangeWhen()
        {
            return ModifierChangeWhen.CONSTANTLY;
        }

        /// <summary>
        /// Cancels the resource cost represented by live proxies because ModuleInventoryPart
        /// already includes the same inventory resource cost.
        /// </summary>
        public float GetModuleCost(float defaultCost, ModifierStagingSituation sit)
        {
            double proxyCost = 0.0;
            foreach (InventoryResourceAggregate aggregate in resourceAggregates.Values)
            {
                if (aggregate.proxy != null && aggregate.definition != null)
                    proxyCost += aggregate.proxy.amount * aggregate.definition.unitCost;
            }
            return (float)-proxyCost;
        }

        /// <summary>
        /// Reports that proxy cost can change whenever its resource amount changes.
        /// </summary>
        public ModifierChangeWhen GetModuleCostChangeWhen()
        {
            return ModifierChangeWhen.CONSTANTLY;
        }

        /// <summary>
        /// Lazily initializes references because the wearables controller can call OnActive
        /// before KSP invokes this module's OnStart method.
        /// </summary>
        private void EnsureInitialized()
        {
            if (initialized || part == null)
                return;

            kerbalEVA = part.FindModuleImplementing<KerbalEVA>();
            if (kerbalEVA == null)
                return;

            inventory = kerbalEVA.ModuleInventoryPartReference;
            if (inventory == null)
                return;

            RemovePersistedResourceProxies();
            initialized = true;
        }

        /// <summary>
        /// Recreates live Kerbal resources from the inventory types selected by provider parts.
        /// </summary>
        private void RebuildResourceProxies()
        {
            if (!initialized || inventory == null || inventory.storedParts == null || synchronizing)
                return;

            synchronizing = true;
            try
            {
                SynchronizeResourceProxiesInternal();
                Dictionary<string, PartResource> existingProxies = CaptureResourceProxies();
                resourceAggregates.Clear();

                HashSet<string> exposedResourceNames = GetExposedResourceNames();
                if (exposedResourceNames.Count > 0)
                    BuildResourceAggregates(exposedResourceNames);

                ReconcileResourceProxies(existingProxies);
            }
            finally
            {
                synchronizing = false;
            }
        }

        /// <summary>
        /// Gets resource names carried by parts that activate WBIModuleEVAResourceTransfer.
        /// </summary>
        private HashSet<string> GetExposedResourceNames()
        {
            HashSet<string> resourceNames = new HashSet<string>();
            int[] keys = inventory.storedParts.Keys.ToArray();
            for (int index = 0; index < keys.Length; index++)
            {
                StoredPart storedPart = inventory.storedParts[keys[index]];
                if (storedPart == null || storedPart.snapshot == null ||
                    storedPart.snapshot.resources == null || !IsResourceProviderPart(storedPart.partName))
                {
                    continue;
                }

                for (int resourceIndex = 0; resourceIndex < storedPart.snapshot.resources.Count; resourceIndex++)
                {
                    ProtoPartResourceSnapshot snapshot = storedPart.snapshot.resources[resourceIndex];
                    if (snapshot == null || string.IsNullOrEmpty(snapshot.resourceName) ||
                        snapshot.resourceName == kerbalEVA.propellantResourceName)
                    {
                        continue;
                    }

                    resourceNames.Add(snapshot.resourceName);
                }
            }
            return resourceNames;
        }

        /// <summary>
        /// Reports whether a cargo part uses WBIModuleWearableItem to activate this module.
        /// </summary>
        private bool IsResourceProviderPart(string partName)
        {
            bool isProvider;
            if (providerPartCache.TryGetValue(partName, out isProvider))
                return isProvider;

            isProvider = false;
            AvailablePart availablePart = PartLoader.getPartInfoByName(partName);
            if (availablePart != null && availablePart.partPrefab != null)
            {
                List<WBIModuleWearableItem> wearableItems =
                    availablePart.partPrefab.FindModulesImplementing<WBIModuleWearableItem>();
                for (int index = 0; index < wearableItems.Count && !isProvider; index++)
                    isProvider = wearableItems[index].RequestsEVAModule(ModuleName);
            }

            providerPartCache[partName] = isProvider;
            return isProvider;
        }

        /// <summary>
        /// Builds contribution lists for selected resource types across the entire EVA inventory.
        /// </summary>
        private void BuildResourceAggregates(HashSet<string> exposedResourceNames)
        {
            int[] keys = inventory.storedParts.Keys.ToArray();
            for (int index = 0; index < keys.Length; index++)
            {
                StoredPart storedPart = inventory.storedParts[keys[index]];
                if (storedPart == null || storedPart.snapshot == null || storedPart.snapshot.resources == null)
                    continue;

                bool isProvider = IsResourceProviderPart(storedPart.partName);

                for (int resourceIndex = 0; resourceIndex < storedPart.snapshot.resources.Count; resourceIndex++)
                {
                    ProtoPartResourceSnapshot snapshot = storedPart.snapshot.resources[resourceIndex];
                    if (snapshot == null || !exposedResourceNames.Contains(snapshot.resourceName))
                        continue;

                    InventoryResourceAggregate aggregate;
                    if (!resourceAggregates.TryGetValue(snapshot.resourceName, out aggregate))
                    {
                        PartResourceDefinition definition = snapshot.definition ??
                            PartResourceLibrary.Instance.GetDefinition(snapshot.resourceName);
                        if (definition == null)
                            continue;

                        aggregate = new InventoryResourceAggregate
                        {
                            resourceName = snapshot.resourceName,
                            definition = definition
                        };
                        resourceAggregates.Add(snapshot.resourceName, aggregate);
                    }

                    aggregate.contributors.Add(new InventoryResourceContributor
                    {
                        snapshot = snapshot,
                        isProvider = isProvider,
                        priority = storedPart.snapshot.resourcePriorityOffset,
                        quantity = Math.Max(1, storedPart.quantity),
                        slotIndex = storedPart.slotIndex
                    });
                }
            }

            foreach (InventoryResourceAggregate aggregate in resourceAggregates.Values)
            {
                aggregate.contributors.Sort(delegate(InventoryResourceContributor first,
                    InventoryResourceContributor second)
                {
                    int providerComparison = second.isProvider.CompareTo(first.isProvider);
                    if (providerComparison != 0)
                        return providerComparison;

                    int priorityComparison = first.priority.CompareTo(second.priority);
                    return priorityComparison != 0
                        ? priorityComparison
                        : first.slotIndex.CompareTo(second.slotIndex);
                });
            }
        }

        /// <summary>
        /// Captures live proxy objects so inventory refreshes can reuse them without rebuilding PAW rows.
        /// </summary>
        private Dictionary<string, PartResource> CaptureResourceProxies()
        {
            Dictionary<string, PartResource> existingProxies =
                new Dictionary<string, PartResource>();
            foreach (InventoryResourceAggregate aggregate in resourceAggregates.Values)
            {
                if (aggregate.proxy != null)
                    existingProxies[aggregate.resourceName] = aggregate.proxy;
            }
            return existingProxies;
        }

        /// <summary>
        /// Reuses proxies whose resource types remain exposed, removes stale proxies, and creates new types.
        /// </summary>
        private void ReconcileResourceProxies(Dictionary<string, PartResource> existingProxies)
        {
            foreach (KeyValuePair<string, PartResource> existingProxy in existingProxies)
            {
                if (resourceAggregates.ContainsKey(existingProxy.Key))
                    continue;

                PartResource liveResource = part.Resources.Get(existingProxy.Key);
                if (ReferenceEquals(liveResource, existingProxy.Value))
                    part.RemoveResource(existingProxy.Value);
            }

            List<string> managedResources = new List<string>();
            foreach (InventoryResourceAggregate aggregate in resourceAggregates.Values)
            {
                PartResource existingProxy;
                if (existingProxies.TryGetValue(aggregate.resourceName, out existingProxy) &&
                    ReferenceEquals(part.Resources.Get(aggregate.resourceName), existingProxy))
                {
                    aggregate.proxy = existingProxy;
                }
                else if (part.Resources.Contains(aggregate.resourceName))
                {
                    Debug.LogWarning("[WBIModuleEVAResourceTransfer] Cannot expose " +
                        aggregate.resourceName + " because the EVA part already contains that resource.");
                    continue;
                }
                else
                {
                    ConfigNode resourceNode = new ConfigNode("RESOURCE");
                    resourceNode.AddValue("name", aggregate.resourceName);
                    resourceNode.AddValue("amount", aggregate.GetAmount().ToString("R", CultureInfo.InvariantCulture));
                    resourceNode.AddValue("maxAmount", aggregate.GetMaxAmount().ToString("R", CultureInfo.InvariantCulture));
                    resourceNode.AddValue("flowState", true);
                    resourceNode.AddValue("isTweakable", false);
                    resourceNode.AddValue("hideFlow", false);
                    resourceNode.AddValue("isVisible", aggregate.definition.isVisible);
                    resourceNode.AddValue("flowMode", PartResource.FlowMode.Both);
                    aggregate.proxy = part.AddResource(resourceNode);
                }

                if (aggregate.proxy == null)
                    continue;

                aggregate.proxy.maxAmount = aggregate.GetMaxAmount();
                aggregate.proxy.amount = Math.Max(0.0,
                    Math.Min(aggregate.proxy.maxAmount, aggregate.GetAmount()));
                aggregate.lastProxyAmount = aggregate.proxy.amount;
                managedResources.Add(aggregate.resourceName);
            }

            managedResourceNames = string.Join(";", managedResources.ToArray());
        }

        /// <summary>
        /// Applies proxy resource changes to inventory snapshots and refreshes aggregate totals.
        /// </summary>
        private void SynchronizeResourceProxies()
        {
            if (synchronizing)
                return;

            synchronizing = true;
            try
            {
                SynchronizeResourceProxiesInternal();
            }
            finally
            {
                synchronizing = false;
            }
        }

        /// <summary>
        /// Performs synchronization while the caller owns the recursion guard.
        /// </summary>
        private void SynchronizeResourceProxiesInternal()
        {
            foreach (InventoryResourceAggregate aggregate in resourceAggregates.Values)
            {
                if (aggregate.proxy == null)
                    continue;

                double change = aggregate.proxy.amount - aggregate.lastProxyAmount;
                if (change < -ResourceTolerance)
                    DrainContributors(aggregate, -change);
                else if (change > ResourceTolerance)
                    FillContributors(aggregate, change);

                aggregate.proxy.maxAmount = aggregate.GetMaxAmount();
                aggregate.proxy.amount = Math.Max(0.0,
                    Math.Min(aggregate.proxy.maxAmount, aggregate.GetAmount()));
                aggregate.lastProxyAmount = aggregate.proxy.amount;
            }
        }

        /// <summary>
        /// Drains provider parts first, then uses the same ascending resourcePriorityOffset
        /// ordering that KerbalEVA uses for EVA Propellant.
        /// </summary>
        private void DrainContributors(InventoryResourceAggregate aggregate, double amount)
        {
            double amountRemaining = amount;
            for (int index = 0; index < aggregate.contributors.Count && amountRemaining > ResourceTolerance; index++)
            {
                InventoryResourceContributor contributor = aggregate.contributors[index];
                double contributorAmount = contributor.GetAmount();
                double amountDrained = Math.Min(contributorAmount, amountRemaining);
                contributor.SetAmount(contributorAmount - amountDrained);
                amountRemaining -= amountDrained;
            }
        }

        /// <summary>
        /// Fills contributors in the same deterministic priority order used for draining.
        /// </summary>
        private void FillContributors(InventoryResourceAggregate aggregate, double amount)
        {
            double amountRemaining = amount;
            for (int index = 0; index < aggregate.contributors.Count && amountRemaining > ResourceTolerance; index++)
            {
                InventoryResourceContributor contributor = aggregate.contributors[index];
                double contributorAmount = contributor.GetAmount();
                double availableCapacity = contributor.GetMaxAmount() - contributorAmount;
                double amountFilled = Math.Min(availableCapacity, amountRemaining);
                contributor.SetAmount(contributorAmount + amountFilled);
                amountRemaining -= amountFilled;
            }
        }

        /// <summary>
        /// Synchronizes and removes all live proxy resources owned by this module.
        /// </summary>
        private void RemoveResourceProxies()
        {
            if (synchronizing)
                return;

            synchronizing = true;
            try
            {
                SynchronizeResourceProxiesInternal();
                RemoveResourceProxiesInternal();
            }
            finally
            {
                synchronizing = false;
            }
        }

        /// <summary>
        /// Removes proxies while the caller owns the recursion guard.
        /// </summary>
        private void RemoveResourceProxiesInternal()
        {
            foreach (InventoryResourceAggregate aggregate in resourceAggregates.Values)
            {
                if (aggregate.proxy != null &&
                    ReferenceEquals(part.Resources.Get(aggregate.resourceName), aggregate.proxy))
                {
                    part.RemoveResource(aggregate.proxy);
                }
            }

            resourceAggregates.Clear();
            managedResourceNames = string.Empty;
        }

        /// <summary>
        /// Removes proxy resources restored by KSP before rebuilding from authoritative snapshots.
        /// </summary>
        private void RemovePersistedResourceProxies()
        {
            if (string.IsNullOrEmpty(managedResourceNames))
                return;

            string[] resourceNames = managedResourceNames.Split(new char[] { ';' },
                StringSplitOptions.RemoveEmptyEntries);
            for (int index = 0; index < resourceNames.Length; index++)
            {
                string resourceName = resourceNames[index].Trim();
                if (!string.IsNullOrEmpty(resourceName) && part.Resources.Contains(resourceName))
                    part.RemoveResource(resourceName);
            }
            managedResourceNames = string.Empty;
        }
    }
}
