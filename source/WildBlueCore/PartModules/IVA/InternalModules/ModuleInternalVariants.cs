using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using WildBlueCore.Wrappers;
using KSP.Localization;

namespace WildBlueCore.PartModules.Variants
{
    /// <summary>
    /// Use this module to change the INTERNAL model (the IVA) of a part.
    /// </summary>
    public class WBIModuleInternalVariants: WBIBasePartModule
    {
        #region Fields
        /// <summary>
        /// Index for the internal variant.
        /// </summary>
        [KSPField(guiActiveEditor = true, isPersistant = true, unfocusedRange = 10.0f)]
        [UI_VariantSelector(affectSymCounterparts = UI_Scene.All, controlEnabled = true, scene = UI_Scene.All)]
        public int variantIndex;
        #endregion

        #region Housekeeping
        List<PartVariant> variants;
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            setupVariants();
            if (HighLogic.LoadedSceneIsEditor)
                applyVariant();
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            if (variants == null || variants.Count <= 0)
                setupVariants();

            if (part.vessel == null || !part.vessel.loaded)
                applyVariant();
        }

        public override void OnAwake()
        {
            base.OnAwake();
            GameEvents.OnEVAConstructionMode.Add(onEVAConstructionMode);
            setupVariants();
        }

        public void OnDestroy()
        {
            GameEvents.OnEVAConstructionMode.Remove(onEVAConstructionMode);
        }

        /// <summary>
        /// Gets the module display name.
        /// </summary>
        /// <returns>A string containing the display name.</returns>
        public override string GetModuleDisplayName()
        {
            return Localizer.Format("#LOC_SUNKWORKS_textureVariants");
        }

        /// <summary>
        /// Gets the module description.
        /// </summary>
        /// <returns>A string containing the module description.</returns>
        public override string GetInfo()
        {
            return Localizer.Format("#LOC_SUNKWORKS_textureVariantsInfo");
        }
        #endregion

        #region Helpers
        private void applyVariant()
        {
            if (part.vessel != null && part.vessel.loaded)
                part.DespawnIVA();

            ConfigNode internalConfig = new ConfigNode("INTERNAL");
            internalConfig.AddValue("name", variants[variantIndex].Name);

            // Get offset info, if any.
            string offsetValues = variants[variantIndex].GetExtraInfoValue("offset");
            if (!string.IsNullOrEmpty(offsetValues))
                internalConfig.AddValue("offset", offsetValues);

            // Courtesy of Reviva. Clone the part info so we don't affect every single part of this type.
            part.partInfo = new AvailablePart(part.partInfo);
            part.partInfo.internalConfig = internalConfig;

            if (part.vessel != null && part.vessel.loaded)
                part.SpawnIVA();
        }

        private void setupVariants()
        {
            ConfigNode node = getPartConfigNode();
            if (node == null)
            {
                Debug.Log("[WBIModuleInternalVariants] - Part config node not found.");
                return;
            }
            loadVariantConfigs(node);
            if (variants.Count == 0)
            {
                Debug.Log("[WBIModuleInternalVariants] - No VARIANT nodes found.");
                return;
            }

            UI_VariantSelector variantSelector = getVariantSelector();

            variantSelector.onFieldChanged += new Callback<BaseField, object>(this.onVariantChanged);

            // Setup variant list
            variantSelector.variants = new List<PartVariant>();
            int count = variants.Count;
            for (int index = 0; index < count; index++)
            {
                variantSelector.variants.Add(variants[index]);
            }
        }

        private void loadVariantConfigs(ConfigNode node)
        {
            variants = new List<PartVariant>();
            if (!node.HasNode("VARIANT"))
                return;
            ConfigNode[] variantNodes = node.GetNodes("VARIANT");
            PartVariant variant;
            string variantName = string.Empty;
            string displayName = string.Empty;
            ConfigNode variantNode;

            for (int index = 0; index < variantNodes.Length; index++)
            {
                variantNode = variantNodes[index];

                // Make sure that we have a variant name and list of game objects
                if (!variantNode.HasValue("name"))
                    continue;

                variant = new PartVariant(variantName, displayName, null);
                variant.Load(variantNode);
                variants.Add(variant);
            }
        }

        private void onVariantChanged(BaseField baseField, object obj)
        {
            if (HighLogic.LoadedSceneIsFlight)
                applyVariant();
        }

        private void onEVAConstructionMode(bool inConstructionMode)
        {
            // Don't allow EVA construction switching if the part is occupied.
            if (part.protoModuleCrew.Count > 0)
            {
                Fields["variantIndex"].guiActiveUnfocused = false;
                Fields["variantIndex"].guiActive = false;
                Debug.Log("[WBIModuleInternalVariants] - Cannot allow IVA switch, there are crew aboard the part. Count: " + part.protoModuleCrew.Count);

                //Dirty the GUI
                MonoUtilities.RefreshContextWindows(part);

                return;
            }

            Fields["variantIndex"].guiActiveUnfocused = inConstructionMode;
            Fields["variantIndex"].guiActive = inConstructionMode;

            //Dirty the GUI
            MonoUtilities.RefreshContextWindows(part);

            UI_VariantSelector variantSelector = getVariantSelector();
            if (variantSelector.variants == null)
                setupVariants();

            Debug.Log("[WBIModuleInternalVariants] - inConstructionMode: " + inConstructionMode);

            if (variantSelector.variants != null)
                Debug.Log("[WBIModuleInternalVariants] - variants: " + variantSelector.variants.Count);
        }

        private UI_VariantSelector getVariantSelector()
        {
            UI_VariantSelector variantSelector = null;

            // Setup variant selector
            if (HighLogic.LoadedSceneIsFlight)
            {
                variantSelector = Fields["variantIndex"].uiControlFlight as UI_VariantSelector;
            }
            else //if (HighLogic.LoadedSceneIsEditor)
            {
                variantSelector = Fields["variantIndex"].uiControlEditor as UI_VariantSelector;
            }

            return variantSelector;
        }
        #endregion
    }
}
