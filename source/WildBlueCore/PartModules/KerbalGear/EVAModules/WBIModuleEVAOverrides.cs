using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WildBlueCore.KerbalGear
{
    /// <summary>
    /// This module modifies the statistics of the kerbal that equips a cargo part that has a WBIModuleWearableItem part module.
    /// It can adjust the kerbal's swim speed, buoyancy, and maximum pressure, as well as other statistics. Overrides are defined by EVA_OVERRIDES config nodes that are
    /// placed in the wearable cargo part's config file. EVA_OVERRIDES is placed at the same level as a MODULE config node.
    /// DO NOT PLACE THIS MODULE IN A PART CONFIG!
    /// </summary>
    /// <example>
    /// <code>
    /// // Adding EVA_OVERRIDES to a part
    /// PART
    /// {
    ///     ...
    ///     MODULE
    ///     {
    ///         name = WBIModuleWearableItem
    ///         ...
    ///         // Be sure to add WBIModuleEVAOverrides to the list of evaModules, and add EVA_OVERRIDES to the part config to specify the overidden values.
    ///         evaModules = WBIModuleEVAOverrides
    ///     }
    ///     
    ///     EVA_OVERRIDES
    ///     {
    ///         // A value <= 0 will cause the kerbal to sink while a value >= 1 will keep it afloat. In between 0 and 1 the kerbal will be partially submerged.
    ///         buoyancy = 1.5
    ///         
    ///         // max pressure in kPA
    ///         maxPressure = 90000
    ///         
    ///         // How much to multiply the kerbal's standard swim speed by.
    ///         swimSpeedMultiplier = 2
    ///     }
    /// }
    /// </code>
    /// <remarks>
    /// WBIModuleEVAOverrides is added to the baseline KERBAL_EVA_MODULES config node. DO NOT place it in a part config file.
    /// </remarks>
    /// <code>
    /// KERBAL_EVA_MODULES
    /// {
    ///     MODULE
    ///     {
    ///         name = WBIModuleEVAOverrides
    ///     }
    /// }
    /// </code>
    /// </example>
    public class WBIModuleEVAOverrides : WBIBasePartModule, IKerbalGearInventoryListener
    {
        #region Fields
        /// <summary>
        /// The buoyancy override
        /// </summary>
        [KSPField]
        public float buoyancyOverride = 1.25f;

        /// <summary>
        /// These inventory parts contain eva overrides that are specified by EVA_OVERRIDES nodes.
        /// </summary>
        [KSPField]
        public string evaOverrideParts = string.Empty;
        #endregion

        #region Housekeeping
        KerbalEVA kerbalEVA;
        bool setInitialValues = false;
        double originalMaxPressure;
        float originalSwimSpeed;
        float originalBuoyancy;
        double maxPressureOverride = 0;
        float maxBuoyancy = 0;
        float swimSpeedMultiplier = 0;
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            kerbalEVA = part.FindModuleImplementing<KerbalEVA>();
            if (kerbalEVA == null)
                return;

            // Get original values
            originalSwimSpeed = kerbalEVA.swimSpeed;
            originalBuoyancy = part.buoyancy;
            originalMaxPressure = part.maxPressure;

            refreshInventoryOverrides(kerbalEVA.ModuleInventoryPartReference);

            // Set initial values if needed.
            if (setInitialValues)
                applyActiveOverrides();
        }

        /// <summary>
        /// Overrides OnInactive. Called when an inventory item is unequipped and the module is disabled.
        /// </summary>
        public override void OnInactive()
        {
            base.OnInactive();
            setInitialValues = false;

            if (kerbalEVA == null)
                return;
            kerbalEVA.swimSpeed = originalSwimSpeed;
            part.buoyancy = originalBuoyancy;
            part.maxPressure = originalMaxPressure;
        }

        /// <summary>
        /// Overrides OnActive. Called when an inventory item is equipped and the module is enabled.
        /// </summary>
        public override void OnActive()
        {
            base.OnActive();
            setInitialValues = true;

            if (kerbalEVA != null)
            {
                refreshInventoryOverrides(kerbalEVA.ModuleInventoryPartReference);
                applyActiveOverrides();
            }
        }

        /// <summary>
        /// Recalculates active EVA overrides without cycling this retained KerbalGear module.
        /// </summary>
        /// <param name="changedInventory">The EVA inventory whose contents changed.</param>
        public void OnKerbalGearInventoryChanged(ModuleInventoryPart changedInventory)
        {
            if (!setInitialValues || kerbalEVA == null || changedInventory == null ||
                changedInventory != kerbalEVA.ModuleInventoryPartReference)
            {
                return;
            }

            refreshInventoryOverrides(changedInventory);
            applyActiveOverrides();
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Rebuilds the strongest EVA overrides supplied by the current inventory contents.
        /// </summary>
        /// <param name="inventory">The EVA inventory containing KerbalGear parts.</param>
        private void refreshInventoryOverrides(ModuleInventoryPart inventory)
        {
            maxPressureOverride = 0;
            maxBuoyancy = 0;
            swimSpeedMultiplier = 0;

            if (inventory == null || inventory.storedParts.Count <= 0)
                return;

            int[] keys = inventory.storedParts.Keys.ToArray();
            for (int index = 0; index < keys.Length; index++)
                updatePartOverrides(inventory.storedParts[keys[index]].partName);
        }

        /// <summary>
        /// Applies the currently calculated inventory overrides to the active EVA Kerbal.
        /// </summary>
        private void applyActiveOverrides()
        {
            if (kerbalEVA == null)
                return;

            kerbalEVA.swimSpeed = swimSpeedMultiplier > 0
                ? originalSwimSpeed * swimSpeedMultiplier
                : originalSwimSpeed;
            part.buoyancy = maxBuoyancy > 0
                ? maxBuoyancy
                : buoyancyOverride > 0 ? buoyancyOverride : originalBuoyancy;
            part.maxPressure = maxPressureOverride > 0
                ? maxPressureOverride
                : originalMaxPressure;
        }

        /// <summary>
        /// Accumulates the strongest EVA overrides supplied by one carried cargo part.
        /// </summary>
        /// <param name="partName">The internal part name whose EVA_OVERRIDES node is inspected.</param>
        void updatePartOverrides(string partName)
        {
            // Get the part config
            AvailablePart availablePart = PartLoader.getPartInfoByName(partName);
            if (availablePart == null)
                return;
            ConfigNode node = availablePart.partConfig;
            if (node == null)
                return;

            // Get the EVA_OVERRIDES node
            if (!node.HasNode("EVA_OVERRIDES"))
                return;
            node = node.GetNode("EVA_OVERRIDES");

            // Get the overrides
            double pressureOverride = 0;
            float swimSpeedOverride = 0;
            float buoyancyOverride = 0;
            if (node.HasValue("buoyancy"))
                float.TryParse(node.GetValue("buoyancy"), out buoyancyOverride);
            if (node.HasValue("swimSpeedMultiplier"))
                float.TryParse(node.GetValue("swimSpeedMultiplier"), out swimSpeedOverride);
            if (node.HasValue("maxPressure"))
                double.TryParse(node.GetValue("maxPressure"), out pressureOverride);

            // Set the overrides
            if (buoyancyOverride > maxBuoyancy)
                maxBuoyancy = buoyancyOverride;

            if (swimSpeedOverride > swimSpeedMultiplier)
                swimSpeedMultiplier = swimSpeedOverride;

            if (pressureOverride > maxPressureOverride)
                maxPressureOverride = pressureOverride;
        }
        #endregion
    }
}
