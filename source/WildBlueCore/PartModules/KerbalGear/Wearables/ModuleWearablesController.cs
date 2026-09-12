using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace WildBlueCore.KerbalGear
{
    /// <summary>
    /// Receives a single notification after KerbalGear has reconciled an EVA inventory change.
    /// Implement this interface when an active EVA module needs to refresh data derived from
    /// inventory contents without being deactivated and reactivated.
    /// </summary>
    public interface IKerbalGearInventoryListener
    {
        /// <summary>
        /// Refreshes inventory-derived state after the EVA inventory reaches its final state.
        /// </summary>
        /// <param name="inventory">The EVA inventory whose contents changed.</param>
        void OnKerbalGearInventoryChanged(ModuleInventoryPart inventory);
    }

    /// <summary>
    /// Receives the exact inventory providers assigned to a dynamic KerbalGear module instance.
    /// Implement this interface when module behavior or state belongs to particular carried items.
    /// </summary>
    public interface IKerbalGearProviderListener
    {
        /// <summary>
        /// Refreshes provider-specific state after KerbalGear reconciles the EVA inventory.
        /// </summary>
        /// <param name="inventory">The EVA inventory containing the providers.</param>
        /// <param name="providers">The providers assigned according to the configured module mode.</param>
        void OnKerbalGearProvidersChanged(ModuleInventoryPart inventory,
            KerbalGearModuleProvider[] providers);
    }

    /// <summary>
    /// Identifies one stored cargo stack that requests a dynamic EVA module.
    /// </summary>
    public sealed class KerbalGearModuleProvider
    {
        /// <summary>
        /// Gets the stable provider key used while reconciling and persisting dynamic modules.
        /// </summary>
        public string ProviderKey { get; private set; }

        /// <summary>
        /// Gets the current inventory slot.
        /// </summary>
        public int SlotIndex { get; private set; }

        /// <summary>
        /// Gets the stored cargo stack represented by this provider.
        /// </summary>
        public StoredPart StoredPart { get; private set; }

        /// <summary>
        /// Gets the wearable-specific overrides for the requested EVA module.
        /// </summary>
        internal ConfigNode ModuleConfig { get; private set; }

        /// <summary>
        /// Creates a provider descriptor for a stored cargo stack.
        /// </summary>
        /// <param name="providerKey">The stable reconciliation key.</param>
        /// <param name="slotIndex">The current inventory slot.</param>
        /// <param name="storedPart">The stored cargo stack.</param>
        internal KerbalGearModuleProvider(string providerKey, int slotIndex, StoredPart storedPart,
            ConfigNode moduleConfig)
        {
            ProviderKey = providerKey;
            SlotIndex = slotIndex;
            StoredPart = storedPart;
            ModuleConfig = moduleConfig;
        }
    }

    #region SWearableProp
    /// <summary>
    /// Represents an instance of a wearable prop. One SWearableProp corresponds to a part's WBIModuleWearableItem part module.
    /// Since WBIModuleWearableItem is created in relation to the part prefab, we use SWearableProp per kerbal on EVA.
    /// </summary>
    internal struct SWearableProp
    {
        /// <summary>
        /// The game object representing the prop.
        /// </summary>
        public GameObject prop;

        /// <summary>
        /// The physical prop mesh.
        /// </summary>
        public Transform meshTransform;

        /// <summary>
        /// Name of the prop.
        /// </summary>
        public string name;

        /// <summary>
        /// Name of the part containing the prop
        /// </summary>
        public string partName;

        /// <summary>
        /// Location of the prop on the kerbal's body.
        /// </summary>
        public BodyLocations bodyLocation;

        /// <summary>
        /// Position offset of the prop.
        /// </summary>
        public Vector3 positionOffset;

        /// <summary>
        /// Position offset of the prop if the kerbal has a jetpack and bodyLocation is backOrJetpack.
        /// </summary>
        public Vector3 positionOffsetJetpack;

        /// <summary>
        /// Rotation offset of the prop.
        /// </summary>
        public Vector3 rotationOffset;

        /// <summary>
        /// Flag to indicate whether the compact stock ChuteStTransform should remain visible while the prop is equipped on the kerbal's back.
        /// </summary>
        public bool showChuteTransforms;
    }
    #endregion

    /// <summary>
    /// A utility class to handle wearable items and the part modules associated with them. This part module is added to a kerbal via a KERBAL_EVA_MODULES config node, NOT a standard KSP part.
    /// </summary>
    /// <example>
    /// <code>
    /// KERBAL_EVA_MODULES
    /// {
    ///     MODULE
    ///     {
    ///         name = WBIModuleWearablesController
    ///         debugMode = false
    ///     }
    /// }
    /// </code>
    /// </example>
    public class WBIModuleWearablesController : WBIBasePartModule
    {
        #region Constants
        public const string kJetpackPartName = "evaJetpack";
        public const string kChutePartName = "evaChute";
        const string kSavedModuleNode = "KERBAL_GEAR_MODULE";
        const string kSavedInstanceKey = "kerbalGearInstanceKey";
        #endregion

        #region Fields
        /// <summary>
        /// Flag to turn on/off debug mode.
        /// </summary>
        [KSPField]
        public bool debugMode;
        #endregion

        #region Housekeeping
        KerbalEVA kerbalEVA;
        ModuleEvaChute evaChute;
        ModuleInventoryPart inventory;
        WBIPropOffsetGUI propOffsetView = null;
        Dictionary<string, List<SWearableProp>> wearablePartProps;
        Dictionary<string, Dictionary<string, WearableEVAModuleRequest>> wearablePartModules;
        readonly HashSet<string> wearablePartsHidingStockPacks = new HashSet<string>();
        readonly Dictionary<string, ActiveEVAModule> activeEVAModules =
            new Dictionary<string, ActiveEVAModule>();
        readonly Dictionary<string, ConfigNode> savedEVAModuleStates =
            new Dictionary<string, ConfigNode>();
        readonly HashSet<string> missingModuleWarnings = new HashSet<string>();
        readonly HashSet<string> exclusiveModuleWarnings = new HashSet<string>();
        readonly HashSet<string> conflictingModuleConfigWarnings = new HashSet<string>();
        bool inventoryRefreshPending;
        bool inventoryRefreshCoroutineRunning;
        bool inventoryEventsSubscribed;

        /// <summary>
        /// Stores a cargo prefab's request and whether it came from the preferred node syntax.
        /// </summary>
        sealed class WearableEVAModuleRequest
        {
            internal ConfigNode moduleConfig;
            internal bool isExplicit;
        }

        /// <summary>
        /// Tracks one module instance created and owned by this controller.
        /// </summary>
        sealed class ActiveEVAModule
        {
            internal string instanceKey;
            internal KerbalGearModuleDefinition definition;
            internal PartModule module;
            internal KerbalGearModuleProvider[] providers;
            internal string configSignature;
        }

        /// <summary>
        /// Describes one module instance required by the current inventory contents.
        /// </summary>
        sealed class DesiredEVAModule
        {
            internal string instanceKey;
            internal KerbalGearModuleDefinition definition;
            internal KerbalGearModuleProvider[] providers;
            internal ConfigNode moduleConfig;
            internal string configSignature;
        }
        #endregion

        #region Overrides
        /// <summary>
        /// Restores state saved for dynamic modules. The modules themselves are created after the
        /// live EVA inventory becomes available during OnStart.
        /// </summary>
        /// <param name="node">The controller's saved configuration.</param>
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (!node.HasNode(kSavedModuleNode))
                return;

            savedEVAModuleStates.Clear();
            ConfigNode[] savedNodes = node.GetNodes(kSavedModuleNode);
            for (int index = 0; index < savedNodes.Length; index++)
            {
                ConfigNode savedNode = savedNodes[index];
                string instanceKey = savedNode.GetValue(kSavedInstanceKey);
                if (!string.IsNullOrEmpty(instanceKey))
                    savedEVAModuleStates[instanceKey] = savedNode.CreateCopy();
            }
        }

        /// <summary>
        /// Persists each live dynamic module inside the always-present controller so its state can
        /// be restored even though the ability module is intentionally absent from the EVA prefab.
        /// </summary>
        /// <param name="node">The controller's save node.</param>
        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            foreach (ActiveEVAModule activeModule in activeEVAModules.Values)
            {
                if (activeModule.module == null)
                    continue;

                ConfigNode savedNode = node.AddNode(kSavedModuleNode);
                activeModule.module.Save(savedNode);
                savedNode.AddValue(kSavedInstanceKey, activeModule.instanceKey);
            }
        }

        /// <summary>
        /// Initializes wearable visuals and creates only the EVA modules requested by carried gear.
        /// </summary>
        /// <param name="state">KSP's current startup state.</param>
        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            List<WBIModuleWearablesController> controllers = part.FindModulesImplementing<WBIModuleWearablesController>();
            if (controllers[0] != this)
            {
                return;
            }

            if (!HighLogic.LoadedSceneIsFlight)
                return;

            getKerbalModules();
            if (kerbalEVA == null || inventory == null)
                return;

            setupWearableParts();

            GameEvents.onModuleInventoryChanged.Add(onModuleInventoryChanged);
            GameEvents.onModuleInventorySlotChanged.Add(onModuleInventorySlotChanged);
            inventoryEventsSubscribed = true;

            reconcileInventory();

            if (debugMode)
            {
                Events["ShowPropOffsetView"].guiActive = true;
                propOffsetView = new WBIPropOffsetGUI();
                propOffsetView.getAttachTransform = getAttachTransform;
            }
        }

        /// <summary>
        /// Coalesces stock inventory events and keeps wearable meshes synchronized.
        /// </summary>
        public override void OnUpdate()
        {
            base.OnUpdate();

            // Stock commonly fires both inventory events for one operation. The reconciliation can
            // add or remove PartModules, so do not perform it inline during Part.ModulesOnUpdate.
            // Deferring it until the end of the frame keeps KSP's index-based module update loop
            // from seeing its PartModuleList change underneath it.
            if (inventoryRefreshPending && !inventoryRefreshCoroutineRunning)
                StartCoroutine(reconcileInventoryDeferred());

            if (kerbalEVA == null)
                return;
        }

        /// <summary>
        /// Applies wearable pack visibility after stock KerbalEVA has updated its own pack models.
        /// </summary>
        public void LateUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || kerbalEVA == null)
                return;

            hidePackMeshes();
        }

        /// <summary>
        /// Stops listening to stock inventory events when this EVA controller is destroyed.
        /// Module teardown is left to KSP so scene changes do not generate duplicate OnInactive calls.
        /// </summary>
        public void OnDestroy()
        {
            if (propOffsetView != null)
                propOffsetView.SetVisible(false);

            if (!inventoryEventsSubscribed)
                return;

            GameEvents.onModuleInventoryChanged.Remove(onModuleInventoryChanged);
            GameEvents.onModuleInventorySlotChanged.Remove(onModuleInventorySlotChanged);
            inventoryEventsSubscribed = false;
            inventoryRefreshPending = false;
            inventoryRefreshCoroutineRunning = false;
            activeEVAModules.Clear();
            savedEVAModuleStates.Clear();
        }
        #endregion

        #region Events
        /// <summary>
        /// Debug button that shows the prop offset view.
        /// </summary>
        [KSPEvent(guiName = "#LOC_WILDBLUECORE_propOffsetButton")]
        public void ShowPropOffsetView()
        {
            propOffsetView.wearablePartProps = wearablePartProps;
            propOffsetView.kerbalEVA = kerbalEVA;
            propOffsetView.SetVisible(true);
        }
        #endregion

        #region Helpers
        public Transform getAttachTransform(BodyLocations bodyLocation)
        {
            string transformName = string.Empty;

            switch (bodyLocation)
            {
                case BodyLocations.back:
                case BodyLocations.backOrJetpack:
                    transformName = "bn_jetpack01";
                    break;

                case BodyLocations.leftFoot:
                    transformName = "bn_l_foot01";
                    break;

                case BodyLocations.rightFoot:
                    transformName = "bn_r_foot01";
                    break;

                case BodyLocations.leftBicep:
                    transformName = "bn_l_elbow_a01";
                    break;

                case BodyLocations.rightBicep:
                    transformName = "bn_r_elbow_a01";
                    break;

                case BodyLocations.head:
                    transformName = "bn_upperJaw01";
                    break;

                case BodyLocations.neck:
                    transformName = "bn_neck01";
                    break;

                case BodyLocations.chest:
                    transformName = "bn_spD01";
                    break;

                case BodyLocations.waist:
                    transformName = "bn_spA01";
                    break;

                case BodyLocations.leftShoulder:
                    transformName = "bn_l_shld01";
                    break;

                case BodyLocations.rightShoulder:
                    transformName = "bn_r_shld01";
                    break;

                case BodyLocations.leftForearm:
                    transformName = "bn_l_elbow_b01";
                    break;

                case BodyLocations.rightForearm:
                    transformName = "bn_r_elbow_b01";
                    break;

                case BodyLocations.leftHand:
                    transformName = "bn_l_wrist01";
                    break;

                case BodyLocations.rightHand:
                    transformName = "bn_r_wrist01";
                    break;

                case BodyLocations.leftPalm:
                    transformName = "bn_l_mid_a01";
                    break;

                case BodyLocations.rightPalm:
                    transformName = "bn_r_mid_a01";
                    break;

                case BodyLocations.leftThigh:
                    transformName = "bn_l_hip01";
                    break;

                case BodyLocations.rightThigh:
                    transformName = "bn_r_hip01";
                    break;

                case BodyLocations.leftCalf:
                    transformName = "bn_l_knee_b01";
                    break;

                case BodyLocations.rightCalf:
                    transformName = "bn_r_knee_b01";
                    break;

                case BodyLocations.leftToes:
                    transformName = "bn_l_ball01";
                    break;

                case BodyLocations.rightToes:
                    transformName = "bn_r_ball01";
                    break;

                default:
                    return null;
            }

            Transform transform = kerbalEVA.part.GetComponentsInChildren<Transform>(true).Where(t => t.name == transformName).FirstOrDefault();
            return transform;
        }

        private void onModuleInventoryChanged(ModuleInventoryPart partInventory)
        {
            if (!HighLogic.LoadedSceneIsFlight || partInventory != inventory)
                return;

            inventoryRefreshPending = true;
        }

        /// <summary>
        /// Queues the same coalesced refresh used by the general inventory-changed event.
        /// </summary>
        /// <param name="partInventory">The inventory whose slot changed.</param>
        /// <param name="slotIndex">The changed inventory slot.</param>
        private void onModuleInventorySlotChanged(ModuleInventoryPart partInventory, int slotIndex)
        {
            if (!HighLogic.LoadedSceneIsFlight || partInventory != inventory)
                return;

            inventoryRefreshPending = true;
        }

        /// <summary>
        /// Runs the pending inventory reconciliation after the current frame's module update pass.
        /// KSP iterates Part.Modules by index inside Part.ModulesOnUpdate, so changing that list from
        /// this controller's OnUpdate can throw an ArgumentOutOfRangeException on the next module.
        /// </summary>
        private System.Collections.IEnumerator reconcileInventoryDeferred()
        {
            inventoryRefreshCoroutineRunning = true;
            yield return new WaitForEndOfFrame();
            inventoryRefreshCoroutineRunning = false;

            if (isCargoPartHeld())
                yield break;

            if (inventoryRefreshPending)
                reconcileInventory();
        }

        /// <summary>
        /// Reports whether stock inventory UI is currently holding a cargo part between source and
        /// destination slots. KerbalGear must not add or remove EVA modules during that transaction,
        /// because the stock cargo UI keeps the held Part and source slot state in static fields.
        /// </summary>
        private static bool isCargoPartHeld()
        {
            UIPartActionControllerInventory inventoryController =
                UIPartActionControllerInventory.Instance;
            return inventoryController != null && inventoryController.CurrentCargoPart != null;
        }

        /// <summary>
        /// Reconciles wearable props and EVA modules with the inventory's current contents.
        /// The desired module map is rebuilt from scratch so duplicate stock events cannot corrupt
        /// lifetime counts or provider ownership.
        /// </summary>
        private void reconcileInventory()
        {
            inventoryRefreshPending = false;
            if (!HighLogic.LoadedSceneIsFlight || inventory == null || wearablePartProps == null ||
                wearablePartModules == null)
                return;

            HashSet<string> storedPartNames = new HashSet<string>();
            Dictionary<string, List<KerbalGearModuleProvider>> moduleProviders =
                new Dictionary<string, List<KerbalGearModuleProvider>>();
            int[] storedPartKeys = inventory.storedParts.Keys.OrderBy(key => key).ToArray();
            for (int index = 0; index < storedPartKeys.Length; index++)
            {
                int slotIndex = storedPartKeys[index];
                StoredPart storedPart = inventory.storedParts[slotIndex];
                if (storedPart == null || string.IsNullOrEmpty(storedPart.partName))
                    continue;

                storedPartNames.Add(storedPart.partName);

                Dictionary<string, WearableEVAModuleRequest> moduleConfigs;
                if (!wearablePartModules.TryGetValue(storedPart.partName, out moduleConfigs))
                    continue;

                foreach (KeyValuePair<string, WearableEVAModuleRequest> moduleEntry in moduleConfigs)
                {
                    string moduleName = moduleEntry.Key;

                    List<KerbalGearModuleProvider> providers;
                    if (!moduleProviders.TryGetValue(moduleName, out providers))
                    {
                        providers = new List<KerbalGearModuleProvider>();
                        moduleProviders.Add(moduleName, providers);
                    }

                    providers.Add(new KerbalGearModuleProvider(
                        getProviderKey(slotIndex, storedPart), slotIndex, storedPart,
                        moduleEntry.Value.moduleConfig.CreateCopy()));
                }
            }

            reconcileWearableProps(storedPartNames);
            if (propOffsetView != null && propOffsetView.IsVisible())
                propOffsetView.RefreshCarriedProps();

            Dictionary<string, DesiredEVAModule> desiredEVAModules =
                buildDesiredEVAModules(moduleProviders);

            string[] activeInstanceKeys = activeEVAModules.Keys.ToArray();
            for (int index = 0; index < activeInstanceKeys.Length; index++)
            {
                if (!desiredEVAModules.ContainsKey(activeInstanceKeys[index]))
                    removeEVAModule(activeInstanceKeys[index]);
            }

            foreach (DesiredEVAModule desiredModule in desiredEVAModules.Values)
            {
                ActiveEVAModule activeModule;
                if (!activeEVAModules.TryGetValue(desiredModule.instanceKey, out activeModule))
                {
                    addEVAModule(desiredModule);
                    continue;
                }

                if (activeModule.configSignature != desiredModule.configSignature)
                {
                    removeEVAModule(desiredModule.instanceKey);
                    addEVAModule(desiredModule);
                    continue;
                }

                activeModule.providers = desiredModule.providers;
                notifyModuleProviders(activeModule);
            }
        }

        /// <summary>
        /// Converts provider requests into concrete module instances according to each registered
        /// module's Exclusive, Aggregate, or PerProvider policy.
        /// </summary>
        /// <param name="moduleProviders">Providers grouped by requested PartModule class.</param>
        /// <returns>The exact dynamic module instances required by the current inventory.</returns>
        private Dictionary<string, DesiredEVAModule> buildDesiredEVAModules(
            Dictionary<string, List<KerbalGearModuleProvider>> moduleProviders)
        {
            Dictionary<string, DesiredEVAModule> desiredModules =
                new Dictionary<string, DesiredEVAModule>();

            foreach (KeyValuePair<string, List<KerbalGearModuleProvider>> providerEntry in
                moduleProviders)
            {
                KerbalGearModuleDefinition definition;
                if (!WBIModuleKerbalEVAModules.TryGetModuleDefinition(providerEntry.Key,
                    out definition))
                {
                    if (missingModuleWarnings.Add(providerEntry.Key))
                    {
                        Debug.LogWarning("[WildBlueCore] KerbalGear item requests " +
                            providerEntry.Key +
                            ", but no matching KERBAL_EVA_MODULES definition was found.");
                    }
                    continue;
                }

                List<KerbalGearModuleProvider> providers = providerEntry.Value;
                switch (definition.Mode)
                {
                    case KerbalGearModuleMode.Aggregate:
                        bool providerAware = isProviderAware(definition);
                        if (!providerAware)
                            warnAboutConflictingConfigs(definition, providers);
                        addDesiredModule(desiredModules, definition,
                            getAggregateInstanceKey(definition), providers.ToArray(),
                            providerAware ? null : providers[0].ModuleConfig);
                        break;

                    case KerbalGearModuleMode.PerProvider:
                        for (int providerIndex = 0; providerIndex < providers.Count; providerIndex++)
                        {
                            KerbalGearModuleProvider provider = providers[providerIndex];
                            addDesiredModule(desiredModules, definition,
                                definition.ModuleName + ":provider:" + provider.ProviderKey,
                                new KerbalGearModuleProvider[] { provider }, provider.ModuleConfig);
                        }
                        break;

                    default:
                        if (providers.Count > 1 &&
                            exclusiveModuleWarnings.Add(definition.ModuleName))
                        {
                            Debug.LogWarning("[WildBlueCore] Multiple carried items request " +
                                definition.ModuleName +
                                " in Exclusive mode; the first inventory provider owns the module.");
                        }

                        KerbalGearModuleProvider exclusiveProvider = providers[0];
                        addDesiredModule(desiredModules, definition,
                            definition.ModuleName + ":exclusive:" +
                                exclusiveProvider.ProviderKey,
                            new KerbalGearModuleProvider[] { exclusiveProvider },
                            exclusiveProvider.ModuleConfig);
                        break;
                }
            }

            return desiredModules;
        }

        /// <summary>
        /// Provider-aware aggregate modules receive all contributing configurations directly and
        /// therefore use their registered defaults instead of inheriting the first provider's
        /// EVA_PART_MODULE overrides. For example, WBIModuleEVAExperienceEffects receives every
        /// carried item's provider descriptor and combines duplicate RepairSkill requests into one
        /// active effect using the highest requested tier.
        /// </summary>
        private static bool isProviderAware(KerbalGearModuleDefinition definition)
        {
            Type moduleType = AssemblyLoader.GetClassByName(typeof(PartModule),
                definition.ModuleName);
            return moduleType != null &&
                typeof(IKerbalGearProviderListener).IsAssignableFrom(moduleType);
        }

        /// <summary>
        /// Adds one concrete module request to the desired instance map.
        /// </summary>
        private static void addDesiredModule(Dictionary<string, DesiredEVAModule> desiredModules,
            KerbalGearModuleDefinition definition, string instanceKey,
            KerbalGearModuleProvider[] providers, ConfigNode overrides)
        {
            ConfigNode moduleConfig = mergeModuleConfig(definition.ModuleConfig, overrides);
            desiredModules[instanceKey] = new DesiredEVAModule
            {
                instanceKey = instanceKey,
                definition = definition,
                providers = providers,
                moduleConfig = moduleConfig,
                configSignature = moduleConfig.ToString()
            };
        }

        /// <summary>
        /// Applies wearable-specific values and child nodes over the registered module defaults.
        /// Child nodes with matching names are replaced as a group, which supports curves such as
        /// atmosphereCurve without combining incompatible keys from two definitions.
        /// </summary>
        private static ConfigNode mergeModuleConfig(ConfigNode defaults, ConfigNode overrides)
        {
            ConfigNode mergedConfig = defaults.CreateCopy();
            if (overrides == null)
                return mergedConfig;

            foreach (ConfigNode.Value value in overrides.values)
            {
                if (value.name != "name")
                    mergedConfig.SetValue(value.name, value.value, true);
            }

            HashSet<string> replacedNodeNames = new HashSet<string>();
            foreach (ConfigNode childNode in overrides.nodes)
            {
                if (replacedNodeNames.Add(childNode.name))
                    mergedConfig.RemoveNodes(childNode.name);
                mergedConfig.AddNode(childNode.CreateCopy());
            }
            return mergedConfig;
        }

        /// <summary>
        /// Aggregate modules use the first inventory provider's configuration. Warn once when
        /// another provider requests the same shared instance with different overrides.
        /// </summary>
        private void warnAboutConflictingConfigs(KerbalGearModuleDefinition definition,
            List<KerbalGearModuleProvider> providers)
        {
            if (providers.Count < 2)
                return;

            string firstConfig = providers[0].ModuleConfig.ToString();
            for (int index = 1; index < providers.Count; index++)
            {
                if (providers[index].ModuleConfig.ToString() == firstConfig)
                    continue;

                if (conflictingModuleConfigWarnings.Add(definition.ModuleName))
                {
                    Debug.LogWarning("[WildBlueCore] Multiple carried items request aggregate " +
                        definition.ModuleName + " with different EVA_PART_MODULE settings; " +
                        "the first inventory provider's settings will be used.");
                }
                return;
            }
        }

        /// <summary>
        /// Creates the stable key used by the single Aggregate module instance.
        /// </summary>
        private static string getAggregateInstanceKey(KerbalGearModuleDefinition definition)
        {
            return definition.ModuleName + ":aggregate";
        }

        /// <summary>
        /// Creates a provider key that survives inventory slot moves whenever the stored snapshot
        /// has a persistent KSP part identifier.
        /// </summary>
        private static string getProviderKey(int slotIndex, StoredPart storedPart)
        {
            if (storedPart.snapshot != null)
            {
                if (storedPart.snapshot.persistentId != 0U)
                    return "pid-" + storedPart.snapshot.persistentId;
                if (storedPart.snapshot.flightID != 0U)
                    return "fid-" + storedPart.snapshot.flightID;
            }

            return "slot-" + slotIndex + "-" + storedPart.partName;
        }

        /// <summary>
        /// Shows only the wearable props represented by parts currently stored in the EVA inventory.
        /// </summary>
        /// <param name="storedPartNames">Unique part names currently present in the inventory.</param>
        private void reconcileWearableProps(HashSet<string> storedPartNames)
        {
            bool hasJetpack = storedPartNames.Contains(kJetpackPartName);
            foreach (KeyValuePair<string, List<SWearableProp>> wearableEntry in wearablePartProps)
            {
                bool shouldBeActive = storedPartNames.Contains(wearableEntry.Key);
                List<SWearableProp> wearableProps = wearableEntry.Value;
                for (int propIndex = 0; propIndex < wearableProps.Count; propIndex++)
                {
                    SWearableProp wearableProp = wearableProps[propIndex];
                    if (wearableProp.prop != null && wearableProp.prop.activeSelf != shouldBeActive)
                        wearableProp.prop.SetActive(shouldBeActive);

                    if (!shouldBeActive || wearableProp.meshTransform == null)
                        continue;

                    wearableProp.meshTransform.localEulerAngles = wearableProp.rotationOffset;
                    wearableProp.meshTransform.localPosition =
                        wearableProp.bodyLocation == BodyLocations.backOrJetpack && hasJetpack
                            ? wearableProp.positionOffsetJetpack
                            : wearableProp.positionOffset;
                }
            }
        }

        /// <summary>
        /// Creates, restores, starts, and activates one requested EVA module.
        /// </summary>
        /// <param name="desiredModule">The desired instance and its assigned providers.</param>
        private void addEVAModule(DesiredEVAModule desiredModule)
        {
            ConfigNode moduleConfig = desiredModule.moduleConfig.CreateCopy();
            PartModule evaModule = part.AddModule(moduleConfig, true);
            if (evaModule == null)
            {
                Debug.LogError("[WildBlueCore] Unable to dynamically add KerbalGear module " +
                    desiredModule.definition.ModuleName + ".");
                return;
            }

            ConfigNode savedState;
            if (savedEVAModuleStates.TryGetValue(desiredModule.instanceKey, out savedState))
                evaModule.Load(savedState);

            // Start from an inactive baseline so modules that inspect their enabled state in
            // OnStart do not perform their active work twice.
            evaModule.moduleIsEnabled = false;
            evaModule.enabled = false;
            evaModule.OnStart(getStartState());
            evaModule.moduleIsEnabled = true;
            evaModule.enabled = true;
            evaModule.OnActive();

            ActiveEVAModule activeModule = new ActiveEVAModule
            {
                instanceKey = desiredModule.instanceKey,
                definition = desiredModule.definition,
                module = evaModule,
                providers = desiredModule.providers,
                configSignature = desiredModule.configSignature
            };
            activeEVAModules.Add(activeModule.instanceKey, activeModule);
            notifyModuleProviders(activeModule, false);
            MonoUtilities.RefreshContextWindows(part);
        }

        /// <summary>
        /// Deactivates and destroys one module instance after its last applicable request disappears.
        /// </summary>
        /// <param name="instanceKey">The dynamic module instance key.</param>
        private void removeEVAModule(string instanceKey)
        {
            ActiveEVAModule activeModule;
            if (!activeEVAModules.TryGetValue(instanceKey, out activeModule))
                return;

            activeEVAModules.Remove(instanceKey);
            if (activeModule.module != null)
            {
                activeModule.module.OnInactive();
                activeModule.module.moduleIsEnabled = false;
                activeModule.module.enabled = false;
                part.RemoveModule(activeModule.module);
            }

            savedEVAModuleStates.Remove(instanceKey);
            MonoUtilities.RefreshContextWindows(part);
        }

        /// <summary>
        /// Notifies a retained or newly created module about the providers assigned by its mode.
        /// Provider-aware modules receive exact ownership; legacy aggregate modules receive the
        /// coalesced inventory notification used by the existing KerbalGear API.
        /// </summary>
        /// <param name="activeModule">The live dynamic module.</param>
        /// <param name="notifyLegacyListener">Whether an existing inventory-only listener should
        /// receive a retained-module refresh.</param>
        private void notifyModuleProviders(ActiveEVAModule activeModule,
            bool notifyLegacyListener = true)
        {
            IKerbalGearProviderListener providerListener =
                activeModule.module as IKerbalGearProviderListener;
            if (providerListener != null)
            {
                providerListener.OnKerbalGearProvidersChanged(inventory,
                    activeModule.providers);
                return;
            }

            if (notifyLegacyListener)
            {
                IKerbalGearInventoryListener inventoryListener =
                    activeModule.module as IKerbalGearInventoryListener;
                if (inventoryListener != null)
                    inventoryListener.OnKerbalGearInventoryChanged(inventory);
            }
        }

        /// <summary>
        /// Maps the live EVA vessel situation to the startup state expected by a new PartModule.
        /// </summary>
        private StartState getStartState()
        {
            if (vessel == null)
                return StartState.None;

            switch (vessel.situation)
            {
                case Vessel.Situations.LANDED:
                    return StartState.Landed;
                case Vessel.Situations.SPLASHED:
                    return StartState.Splashed;
                case Vessel.Situations.SUB_ORBITAL:
                    return StartState.SubOrbital;
                case Vessel.Situations.ORBITING:
                    return StartState.Orbital;
                case Vessel.Situations.FLYING:
                    return StartState.Flying;
                default:
                    return StartState.None;
            }
        }

        private bool hasBackpackProp()
        {
            if (hasStockPackHidingPart())
                return true;

            List<SWearableProp> wearableProps;
            SWearableProp wearableProp;
            string[] keys = wearablePartProps.Keys.ToArray();
            int count;
            for (int index = 0; index < keys.Length; index++)
            {
                wearableProps = wearablePartProps[keys[index]];
                count = wearableProps.Count;
                for (int propIndex = 0; propIndex < count; propIndex++)
                {
                    wearableProp = wearableProps[propIndex];
                    if (inventory.ContainsPart(wearableProp.partName) && (wearableProp.bodyLocation == BodyLocations.back || wearableProp.bodyLocation == BodyLocations.backOrJetpack))
                        return true;
                }
            }
            return false;
        }

        private bool hasStockPackHidingPart()
        {
            foreach (string partName in wearablePartsHidingStockPacks)
            {
                if (inventory.ContainsPart(partName))
                    return true;
            }
            return false;
        }

        private bool shouldShowChuteTransforms()
        {
            if (!inventory.ContainsPart(kChutePartName))
                return false;

            List<SWearableProp> wearableProps;
            SWearableProp wearableProp;
            string[] keys = wearablePartProps.Keys.ToArray();
            int count;
            for (int index = 0; index < keys.Length; index++)
            {
                wearableProps = wearablePartProps[keys[index]];
                count = wearableProps.Count;
                for (int propIndex = 0; propIndex < count; propIndex++)
                {
                    wearableProp = wearableProps[propIndex];
                    if (wearableProp.showChuteTransforms &&
                        inventory.ContainsPart(wearableProp.partName) &&
                        (wearableProp.bodyLocation == BodyLocations.back || wearableProp.bodyLocation == BodyLocations.backOrJetpack))
                        return true;
                }
            }
            return false;
        }

        private void hidePackMeshes()
        {
            bool hideAllStockPacks = hasStockPackHidingPart();

            // Make sure we have a backpack prop
            if (!hideAllStockPacks && !hasBackpackProp())
            {
                return;
            }

            List<FlagDecal> flags = part.FindModulesImplementing<FlagDecal>();
            FlagDecal decal;
            int flagCount = flags.Count;
            for (int flagIndex = 0; flagIndex < flagCount; flagIndex++)
            {
                decal = flags[flagIndex];
                if ((decal.textureQuadName.Contains("EVAStorage") && (kerbalEVA.StorageTransform.gameObject.activeSelf || kerbalEVA.StorageSlimTransform.gameObject.activeSelf)) ||
                    (decal.textureQuadName.Contains("kbEVA_flagDecals") && inventory.ContainsPart("evaJetpack")))
                {
                    decal.flagDisplayed = false;
                    decal.UpdateDisplay();
                }
            }

            kerbalEVA.BackpackTransform.gameObject.SetActive(false);
            kerbalEVA.BackpackStTransform.gameObject.SetActive(false);
            kerbalEVA.StorageTransform.gameObject.SetActive(false);
            kerbalEVA.StorageSlimTransform.gameObject.SetActive(false);
            kerbalEVA.ChuteJetpackTransform.gameObject.SetActive(false);
            if (!hideAllStockPacks && shouldShowChuteTransforms())
            {
                // A wearable counts as an additional inventory item, which makes stock KSP select
                // ChuteContainerTransform. Stock UpdatePackModels can make that selection again
                // without notifying this controller, so keep ModuleEvaChute pointed at the compact
                // hierarchy until deployment starts. Once semi-deployed, SetCanopy must not be
                // called because it deliberately hides the canopy it selects.
                if (evaChute != null &&
                    (evaChute.deploymentState == ModuleParachute.deploymentStates.STOWED ||
                     evaChute.deploymentState == ModuleParachute.deploymentStates.ACTIVE))
                {
                    evaChute.SetCanopy(kerbalEVA.ChuteStTransform);
                }
                kerbalEVA.ChuteContainerTransform.gameObject.SetActive(false);
                kerbalEVA.ChuteStTransform.gameObject.SetActive(true);
            }
            else
            {
                kerbalEVA.ChuteStTransform.gameObject.SetActive(false);
                kerbalEVA.ChuteContainerTransform.gameObject.SetActive(false);
            }
        }

        private void setupWearableParts()
        {
            List<AvailablePart> cargoParts = PartLoader.Instance.GetAvailableAndPurchaseableCargoParts();
            AvailablePart availablePart;
            List<WBIModuleWearableItem> wearableItems;
            WBIModuleWearableItem wearableItem;
            int count = cargoParts.Count;
            int itemCount;
            Transform anchorTransform;
            GameObject prefab;
            GameObject prop;
            Transform attachTransform;
            Collider[] colliders;
            SWearableProp wearableProp;
            List<SWearableProp> wearableProps;

            wearablePartProps = new Dictionary<string, List<SWearableProp>>();
            wearablePartModules =
                new Dictionary<string, Dictionary<string, WearableEVAModuleRequest>>();
            wearablePartsHidingStockPacks.Clear();

            for (int index = 0; index < count; index++)
            {
                availablePart = cargoParts[index];
                if (availablePart.partPrefab.HasModuleImplementing<WBIModuleWearableItem>())
                {
                    wearableItems = availablePart.partPrefab.FindModulesImplementing<WBIModuleWearableItem>();

                    // Hide directives are part-wide so a configuration-only wearable module can
                    // remove ground/display meshes from every applicable wearable clone without
                    // needing to create a prop of its own. Stock pack suppression is also
                    // part-wide: any wearable module can request it for the carried item.
                    HashSet<string> hiddenTransformNames = new HashSet<string>(StringComparer.Ordinal);
                    bool hideStockPacksWhenWorn = false;
                    for (int hideItemIndex = 0; hideItemIndex < wearableItems.Count; hideItemIndex++)
                    {
                        WBIModuleWearableItem hideItem = wearableItems[hideItemIndex];
                        if (hideItem.hideStockPacksWhenWorn)
                            hideStockPacksWhenWorn = true;

                        string configuredNames = hideItem.hideTransformsWhenWorn;
                        if (string.IsNullOrEmpty(configuredNames))
                            continue;

                        string[] transformNames = configuredNames.Split(new char[] { ';' },
                            StringSplitOptions.RemoveEmptyEntries);
                        for (int transformIndex = 0; transformIndex < transformNames.Length;
                            transformIndex++)
                        {
                            string transformName = transformNames[transformIndex].Trim();
                            if (!string.IsNullOrEmpty(transformName))
                                hiddenTransformNames.Add(transformName);
                        }
                    }

                    if (hideStockPacksWhenWorn)
                        wearablePartsHidingStockPacks.Add(availablePart.name);

                    // Setup our wearable props for this part.
                    wearableProps = new List<SWearableProp>();
                    wearablePartProps.Add(availablePart.name, wearableProps);

                    // Setup the props- Special thanks to Vali and Issac for showing the way how!
                    itemCount = wearableItems.Count;
                    for (int itemIndex = 0; itemIndex < itemCount; itemIndex++)
                    {
                        wearableItem = wearableItems[itemIndex];

                        // Create new wearable prop instance.
                        wearableProp = new SWearableProp();
                        wearableProp.name = wearableItem.moduleID;
                        wearableProp.partName = availablePart.name;
                        wearableProp.bodyLocation = wearableItem.bodyLocation;
                        wearableProp.positionOffset = wearableItem.positionOffset;
                        wearableProp.positionOffsetJetpack = wearableItem.positionOffsetJetpack;
                        wearableProp.rotationOffset = wearableItem.rotationOffset;
                        wearableProp.showChuteTransforms = wearableItem.showChuteTransforms;

                        // Setup configurable EVA module requests. Explicit EVA_PART_MODULE nodes
                        // take precedence over legacy evaModules entries with no overrides.
                        Dictionary<string, WearableEVAModuleRequest> evaModuleConfigs;
                        if (!wearablePartModules.TryGetValue(availablePart.name,
                            out evaModuleConfigs))
                        {
                            evaModuleConfigs =
                                new Dictionary<string, WearableEVAModuleRequest>();
                            wearablePartModules.Add(availablePart.name, evaModuleConfigs);
                        }

                        ConfigNode[] configuredModuleNodes =
                            wearableItem.GetEVAPartModuleConfigs();
                        for (int moduleIndex = 0; moduleIndex < configuredModuleNodes.Length;
                            moduleIndex++)
                        {
                            ConfigNode moduleConfig = configuredModuleNodes[moduleIndex];
                            string moduleName = moduleConfig.GetValue("name");
                            WearableEVAModuleRequest existingRequest;
                            if (evaModuleConfigs.TryGetValue(moduleName, out existingRequest) &&
                                existingRequest.isExplicit &&
                                existingRequest.moduleConfig.ToString() != moduleConfig.ToString())
                            {
                                string warningKey = availablePart.name + ":" + moduleName;
                                if (conflictingModuleConfigWarnings.Add(warningKey))
                                {
                                    Debug.LogWarning("[WildBlueCore] " + availablePart.name +
                                        " contains different EVA_PART_MODULE settings for " +
                                        moduleName + "; the first settings will be used.");
                                }
                                continue;
                            }
                            evaModuleConfigs[moduleName] = new WearableEVAModuleRequest
                            {
                                moduleConfig = moduleConfig.CreateCopy(),
                                isExplicit = true
                            };
                        }

                        // Legacy compatibility: an explicit node wins if both request the same
                        // module. Otherwise create an empty override node that uses global defaults.
                        if (!string.IsNullOrEmpty(wearableItem.evaModules))
                        {
                            string[] configuredModules = wearableItem.evaModules.Split(new char[] { ';' });
                            for (int moduleIndex = 0; moduleIndex < configuredModules.Length; moduleIndex++)
                            {
                                string moduleName = configuredModules[moduleIndex].Trim();
                                if (string.IsNullOrEmpty(moduleName) ||
                                    evaModuleConfigs.ContainsKey(moduleName))
                                    continue;

                                ConfigNode legacyConfig = new ConfigNode("EVA_PART_MODULE");
                                legacyConfig.AddValue("name", moduleName);
                                evaModuleConfigs.Add(moduleName, new WearableEVAModuleRequest
                                {
                                    moduleConfig = legacyConfig,
                                    isExplicit = false
                                });
                            }
                        }

                        // EXPERIENCE_EFFECT nodes are declarative part-level overrides. Their
                        // presence implicitly requests the aggregate manager, so authors do not
                        // also need evaModules or an EVA_PART_MODULE node.
                        if (WBIModuleEVAExperienceEffects.PartHasExperienceEffects(availablePart) &&
                            !evaModuleConfigs.ContainsKey(
                                WBIModuleEVAExperienceEffects.ModuleName))
                        {
                            ConfigNode effectConfig = new ConfigNode("EVA_PART_MODULE");
                            effectConfig.AddValue("name",
                                WBIModuleEVAExperienceEffects.ModuleName);
                            evaModuleConfigs.Add(WBIModuleEVAExperienceEffects.ModuleName,
                                new WearableEVAModuleRequest
                                {
                                    moduleConfig = effectConfig,
                                    isExplicit = false
                                });
                        }

                        // Get the attachment transform
                        attachTransform = getAttachTransform(wearableItem.bodyLocation);

                        // Get the anchor transform and prefab
                        anchorTransform = availablePart.partPrefab.FindModelTransform(wearableItem.anchorTransform);
                        if (anchorTransform == null)
                            continue;
                        prefab = anchorTransform.gameObject;

                        // Create instance
                        prop = Instantiate(prefab, attachTransform.position, attachTransform.rotation, kerbalEVA.transform);
                        prop.name = wearableItem.moduleID;
                        wearableProp.prop = prop;

                        // Only the instantiated Kerbal prop is modified. KSP will therefore use
                        // the untouched part prefab, with these transforms enabled, when the cargo
                        // item is dropped back into the world.
                        hideTransforms(prop, hiddenTransformNames);

                        // Add the TrackingRigObject. The tracking rig moves the prop (GameObject) associated with the anchorTransform.
                        TrackRigObject trackRig = prop.AddComponent<TrackRigObject>();
                        trackRig.target = attachTransform;
                        trackRig.keepInitialOffset = false;
                        trackRig.trackingMode = TrackRigObject.TrackMode.LateUpdate;

                        // Now we need the child mesh that we'll apply the position and rotation offsets to.
                        Transform meshTransform = prop.transform.Find(wearableItem.meshTransform);
                        wearableProp.meshTransform = meshTransform;

                        /*
                        meshTransform.localEulerAngles = wearableItem.rotationOffset;
                        if (wearableItem.bodyLocation != BodyLocations.backOrJetpack)
                            meshTransform.localPosition = wearableItem.positionOffset;
                        else
                            meshTransform.localPosition = inventory.ContainsPart(kJetpackPartName) ? wearableItem.positionOffsetJetpack : wearableItem.positionOffset;
                        */

                        // Remove colliders
                        colliders = prop.GetComponentsInChildren<Collider>(true);
                        for (int colliderIndex = 0; colliderIndex < colliders.Length; colliderIndex++)
                            DestroyImmediate(colliders[colliderIndex]);

                        // Hide the prop for now.
                        prop.SetActive(false);

                        // Add the wearable prop to our list.
                        wearablePartProps[availablePart.name].Add(wearableProp);
                    }
                }
            }
        }

        private void getKerbalModules()
        {
            kerbalEVA = part.FindModuleImplementing<KerbalEVA>();
            if (kerbalEVA != null)
            {
                inventory = kerbalEVA.ModuleInventoryPartReference;
                evaChute = part.FindModuleImplementing<ModuleEvaChute>();
            }
        }

        /// <summary>
        /// Hides each configured transform found within a wearable prop hierarchy.
        /// A part can create multiple props, so names that do not occur in this particular clone
        /// are intentionally ignored.
        /// </summary>
        /// <param name="prop">The instantiated wearable prop.</param>
        /// <param name="hiddenTransformNames">Case-sensitive transform names to hide.</param>
        private static void hideTransforms(GameObject prop, HashSet<string> hiddenTransformNames)
        {
            if (prop == null || hiddenTransformNames == null || hiddenTransformNames.Count == 0)
                return;

            Transform[] transforms = prop.GetComponentsInChildren<Transform>(true);
            for (int transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
            {
                Transform candidate = transforms[transformIndex];
                if (candidate != null && hiddenTransformNames.Contains(candidate.name))
                    candidate.gameObject.SetActive(false);
            }
        }
        #endregion
    }
}
