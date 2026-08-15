using System;
using System.Collections.Generic;
using UnityEngine;

namespace WildBlueCore.KerbalGear
{
    /// <summary>
    /// Determines how KerbalGear creates a requested EVA PartModule.
    /// </summary>
    public enum KerbalGearModuleMode
    {
        /// <summary>
        /// Creates one module for the first deterministic provider. This is the default.
        /// </summary>
        Exclusive,

        /// <summary>
        /// Creates one module shared by all providers. The module must aggregate inventory state.
        /// </summary>
        Aggregate,

        /// <summary>
        /// Creates a separate module for each stored-part snapshot that requests it.
        /// </summary>
        PerProvider
    }

    /// <summary>
    /// Describes a dynamically available KerbalGear EVA module.
    /// </summary>
    public sealed class KerbalGearModuleDefinition
    {
        /// <summary>
        /// Gets the PartModule class name.
        /// </summary>
        public string ModuleName { get; private set; }

        /// <summary>
        /// Gets the configured module multiplicity.
        /// </summary>
        public KerbalGearModuleMode Mode { get; private set; }

        /// <summary>
        /// Gets a copy of the MODULE configuration without KerbalGear-only values.
        /// </summary>
        public ConfigNode ModuleConfig { get; private set; }

        /// <summary>
        /// Creates a dynamic module definition from a KERBAL_EVA_MODULES entry.
        /// </summary>
        /// <param name="moduleName">The PartModule class name.</param>
        /// <param name="mode">The requested module multiplicity.</param>
        /// <param name="moduleConfig">The configuration passed to the dynamic PartModule.</param>
        internal KerbalGearModuleDefinition(string moduleName, KerbalGearModuleMode mode,
            ConfigNode moduleConfig)
        {
            ModuleName = moduleName;
            Mode = mode;
            ModuleConfig = moduleConfig;
        }
    }

    /// <summary>
    /// Adds the always-present KerbalGear controller to EVA prefabs and registers the remaining
    /// KERBAL_EVA_MODULES entries for on-demand creation by that controller.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Instantly, false)]
    sealed class WBIModuleKerbalEVAModules : MonoBehaviour
    {
        #region Constants
        const string kKerbalEVAModulesNode = "KERBAL_EVA_MODULES";
        const string kModuleNode = "MODULE";
        const string kNameField = "name";
        const string kModeField = "kerbalGearMode";
        const string kControllerModuleName = "WBIModuleWearablesController";
        #endregion

        #region Housekeeping
        static readonly Dictionary<string, KerbalGearModuleDefinition> moduleDefinitions =
            new Dictionary<string, KerbalGearModuleDefinition>();
        #endregion

        /// <summary>
        /// Finds the configuration used to create a requested EVA module at runtime.
        /// </summary>
        /// <param name="moduleName">The requested PartModule class name.</param>
        /// <param name="definition">The registered definition, when found.</param>
        /// <returns>True when KERBAL_EVA_MODULES defines the requested module.</returns>
        internal static bool TryGetModuleDefinition(string moduleName,
            out KerbalGearModuleDefinition definition)
        {
            return moduleDefinitions.TryGetValue(moduleName, out definition);
        }

        /// <summary>
        /// Reads KERBAL_EVA_MODULES after parts load, registers dynamic definitions, and adds only
        /// the lightweight controller to each EVA prefab.
        /// </summary>
        class EVAModulesLoader : LoadingSystem
        {
            /// <summary>
            /// Indicates that the loader has no asynchronous work to finish.
            /// </summary>
            /// <returns>Always true.</returns>
            public override bool IsReady()
            {
                return true;
            }

            /// <summary>
            /// Builds the dynamic module registry and installs the KerbalGear controller.
            /// </summary>
            public override void StartLoad()
            {
                moduleDefinitions.Clear();
                ConfigNode controllerConfig = null;
                ConfigNode[] evaNodes = GameDatabase.Instance.GetConfigNodes(kKerbalEVAModulesNode);

                for (int evaNodeIndex = 0; evaNodeIndex < evaNodes.Length; evaNodeIndex++)
                {
                    ConfigNode evaNode = evaNodes[evaNodeIndex];
                    if (!evaNode.HasNode(kModuleNode))
                        continue;

                    ConfigNode[] evaModules = evaNode.GetNodes(kModuleNode);
                    for (int moduleIndex = 0; moduleIndex < evaModules.Length; moduleIndex++)
                    {
                        ConfigNode moduleConfig = evaModules[moduleIndex];
                        if (!moduleConfig.HasValue(kNameField))
                            continue;

                        string moduleName = moduleConfig.GetValue(kNameField);
                        if (moduleName == kControllerModuleName)
                        {
                            if (controllerConfig == null)
                                controllerConfig = moduleConfig.CreateCopy();
                            continue;
                        }

                        registerDynamicModule(moduleName, moduleConfig);
                    }
                }

                if (controllerConfig == null)
                {
                    Debug.LogError("[WildBlueCore] KERBAL_EVA_MODULES does not define " +
                        kControllerModuleName + "; dynamic KerbalGear modules are unavailable.");
                    return;
                }

                int count = PartLoader.LoadedPartsList.Count;
                for (int index = 0; index < count; index++)
                {
                    AvailablePart availablePart = PartLoader.LoadedPartsList[index];
                    if (!availablePart.partPrefab.HasModuleImplementing<KerbalEVA>() ||
                        availablePart.partPrefab.Modules.Contains(kControllerModuleName))
                    {
                        continue;
                    }

                    availablePart.partPrefab.AddModule(controllerConfig.CreateCopy(), true);
                }
            }

            /// <summary>
            /// Registers a dynamic module and removes loader-only values from its runtime config.
            /// </summary>
            /// <param name="moduleName">The PartModule class name.</param>
            /// <param name="sourceConfig">The KERBAL_EVA_MODULES module node.</param>
            private static void registerDynamicModule(string moduleName, ConfigNode sourceConfig)
            {
                KerbalGearModuleMode mode = KerbalGearModuleMode.Exclusive;
                string configuredMode = sourceConfig.GetValue(kModeField);
                if (!string.IsNullOrEmpty(configuredMode) &&
                    !Enum.TryParse(configuredMode, true, out mode))
                {
                    Debug.LogWarning("[WildBlueCore] Invalid kerbalGearMode '" + configuredMode +
                        "' for " + moduleName + "; using Exclusive.");
                    mode = KerbalGearModuleMode.Exclusive;
                }

                if (moduleDefinitions.ContainsKey(moduleName))
                {
                    Debug.LogWarning("[WildBlueCore] Multiple KERBAL_EVA_MODULES entries define " +
                        moduleName + "; the first definition will be used.");
                    return;
                }

                ConfigNode runtimeConfig = sourceConfig.CreateCopy();
                runtimeConfig.RemoveValue(kModeField);
                moduleDefinitions.Add(moduleName,
                    new KerbalGearModuleDefinition(moduleName, mode, runtimeConfig));
            }
        }

        #region Overrides
        /// <summary>
        /// Inserts the KerbalGear loader immediately after KSP's PartLoader.
        /// </summary>
        public void Awake()
        {
            List<LoadingSystem> loaders = LoadingScreen.Instance.loaders;
            if (loaders == null)
                return;

            int count = loaders.Count;
            for (int index = 0; index < count; index++)
            {
                if (!(loaders[index] is PartLoader))
                    continue;

                GameObject gameObject = new GameObject();
                EVAModulesLoader modulesLoader = gameObject.AddComponent<EVAModulesLoader>();
                loaders.Insert(index + 1, modulesLoader);
                break;
            }
        }
        #endregion
    }
}
