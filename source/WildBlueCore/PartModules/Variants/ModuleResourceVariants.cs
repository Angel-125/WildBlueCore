using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using WildBlueCore.Wrappers;

namespace WildBlueCore
{
    /// <summary>
    /// A small helper class to update a part's resources when a part variant is applied. ModulePartVariants defines one or more VARIANT config nodes, and each node
    /// can have a EXTRA_INFO within its config. EXTRA_INFO uses key/value pairs to define its data. ModuleResourceVariants can also define its own VARIANT nodes.
    /// When ModulePartVariants fires its onVariantApplied event, and the name of the event matches one of ModuleResourceVariants's VARIANT nodes, then ModuleResourceVariants's
    /// variant will be applied. Currently ModuleResourceVariants only supports RESOURCE nodes in its VARIANT node.
    /// </summary>
    /// <remarks>
    /// ModulePartVariants can define EXTRA_INFO as part of its VARIANT node, and ModuleResourceVariants can read some of the values defined in the EXTRA_INFO. here's an example:
    /// </remarks>
    /// <example>
    /// <code>
    /// MODULE
    /// {
    ///     name = ModulePartVariants
    ///     ...
    ///     VARIANT
    ///     {
    ///         name = someVariantName
    ///         ...
    ///         EXTRA_INFO
    ///         {
    ///             // The name of a single resource to modify on the part.
    ///             resourceName = IntakeLqd
    ///             
    ///             // The new amount of resource that will be applied to the part's resource. This can only happen in the VAB/SPH.
    ///             amount = 500
    ///             
    ///             // The new maximum amount of resource that will be applied to the part's resource. This can happen both in the VAB/SPH and in flight.
    ///             maxAmount = 500
    ///             
    ///             // If the part has a ModuleInventoryPart, then its storage limit will be updated. Similarly, if the part has a WBIOmniStorage part module, then
    ///             // its maximum storage volume will be updated as well.
    ///             packedVolumeLimit = 200
    ///             
    ///             // This only applies to ModuleInventoryPart and WBIOmniStorage. It computes their new storage volume, in liters, by multiplying resourceVolume by volumeMultiplier.
    ///             volumeMultiplier = 5
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// To define ModuleResourceVariants:
    /// </remarks>
    /// <example>
    /// <code>
    /// MODULE
    /// {
    ///     name = ModuleResourceVariants
    ///     resourceVolume = 6000
    ///     // You can specify resource variants for the part that will be applied when you change the part's variant.
    ///     VARIANT
    ///     {
    ///         // The name of this variant node must match the name of the VARIANT node specified in the part's ModulePartVariants.
    ///         name = someVariantName
    ///         RESOURCE
    ///         {
    ///             name = Snacks
    ///             amount = 600
    ///             maxAmount = 600
    ///         }
    ///         RESOURCE
    ///         {
    ///             name = FreshAir
    ///             amount = 60
    ///             maxAmount = 60
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    public class ModuleResourceVariants : BasePartModule
    {
        #region Constants
        const string kResourceName = "resourceName";
        const string kAmount = "amount";
        const string kMaxAmount = "maxAmount";
        const string kPackedVolumeLimit = "packedVolumeLimit";
        const string kResourceNode = "RESOURCE";
        const string kVariantNode = "VARIANT";
        const string kName = "name";
        const string kVolumeMultiplier = "volumeMultiplier";
        #endregion

        #region Fields
        /// <summary>
        /// Resource volume size, in liters, per unit of volume. When the extra info in onPartVariantApplied contains volumeMultiplier,
        /// resource and inventory part modules will be updated to reflect the change. In such a case, the new storage volume will be
        /// resourceVolume * volumeMultiplier.
        /// </summary>
        [KSPField]
        public float resourceVolume = 0;
        #endregion

        #region Housekeeping
        WBIOmniStorageWrapper omniStorage;
        #endregion

        #region Overrides
        public void OnDestroy()
        {
            GameEvents.onVariantApplied.Remove(onVariantApplied);
        }

        public override void OnAwake()
        {
            GameEvents.onVariantApplied.Add(onVariantApplied);
            omniStorage = WBIOmniStorageWrapper.GetOmniStorage(part);
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            omniStorage = WBIOmniStorageWrapper.GetOmniStorage(part);
        }
        #endregion

        #region Helpers
        private void onVariantApplied(Part variantPart, PartVariant variant)
        {
            if (variantPart != part)
                return;

            // Handle storage limit (absolute value)
            string packedVolumeLimitStr = variant.GetExtraInfoValue(kPackedVolumeLimit);
            updateStorageCapacity(packedVolumeLimitStr);

            // Handle storage limit (relative unit volumes). ModulePartGridVariants sends this.
            string volumeMultiplierStr = variant.GetExtraInfoValue(kVolumeMultiplier);
            float volumeMultiplier = 0;
            if (resourceVolume > 0 && !string.IsNullOrEmpty(volumeMultiplierStr) && float.TryParse(volumeMultiplierStr, out volumeMultiplier))
            {
                float updatedVolume = resourceVolume * volumeMultiplier;
                updateStorageCapacity(updatedVolume);
            }

            // Handle single resource (absolute value)
            string resourceName = variant.GetExtraInfoValue(kResourceName);
            string amtStr = variant.GetExtraInfoValue(kAmount);
            string maxAmtStr = variant.GetExtraInfoValue(kMaxAmount);
            updateResource(resourceName, amtStr, maxAmtStr);

            // Handle multiple resources (absolute values)
            updateVariantResources(variant.Name);

            //Dirty the GUI
            MonoUtilities.RefreshContextWindows(this.part);
        }

        private void updateStorageCapacity(string packedVolumeLimitStr)
        {
            float packedVolumeLimit = 0;

            if (string.IsNullOrEmpty(packedVolumeLimitStr) || !float.TryParse(packedVolumeLimitStr, out packedVolumeLimit))
                return;

            updateStorageCapacity(packedVolumeLimit);
        }

        private void updateStorageCapacity(float storageVolume)
        {
            ModuleInventoryPart inventory = null;

            inventory = part.FindModuleImplementing<ModuleInventoryPart>();
            if (inventory != null && inventory.HasPackedVolumeLimit)
                inventory.packedVolumeLimit = storageVolume;

            updateOmniStorage(storageVolume);
        }

        private void updateOmniStorage(float storageVolume)
        {
            if (omniStorage == null)
                return;

            omniStorage.UpdateStorageVolume(storageVolume);
        }

        private void updateVariantResources(string variantName)
        {
            if (!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight)
                return;
            ConfigNode node = getPartConfigNode();
            if (!node.HasNode(kVariantNode))
                return;
            ConfigNode[] nodes = node.GetNodes(kVariantNode);

            node = null;
            for (int index = 0; index < nodes.Length; index++)
            {
                if (!nodes[index].HasValue(kName))
                    continue;
                if (nodes[index].GetValue(kName) == variantName)
                {
                    node = nodes[index];
                    break;
                }
            }
            if (node == null)
                return;

            if (!node.HasNode(kResourceNode))
                return;
            nodes = node.GetNodes(kResourceNode);
            string amtStr = string.Empty;
            string maxAmtStr = string.Empty;
            string resourceName;
            for (int index = 0; index < nodes.Length; index++)
            {
                node = nodes[index];
                if (!node.HasValue(kResourceName) && !node.HasValue(kName))
                    continue;

                resourceName = node.GetValue(kResourceName);
                if (string.IsNullOrEmpty(resourceName))
                    resourceName = node.GetValue(kName);

                if (node.HasValue(kAmount))
                    amtStr = node.GetValue(kAmount);

                if (node.HasValue(kMaxAmount))
                    maxAmtStr = node.GetValue(kMaxAmount);

                updateResource(resourceName, amtStr, maxAmtStr);
            }
        }

        private void updateResource(string resourceName, string amtStr, string maxAmtStr)
        {
            double amount = 0;
            double maxAmount = 0;

            if (string.IsNullOrEmpty(resourceName) || !part.Resources.Contains(resourceName))
                return;

            if (HighLogic.LoadedSceneIsEditor && !string.IsNullOrEmpty(amtStr) & double.TryParse(amtStr, out amount))
                part.Resources[resourceName].amount = amount;

            if (!string.IsNullOrEmpty(maxAmtStr) & double.TryParse(maxAmtStr, out maxAmount))
            {
                part.Resources[resourceName].maxAmount = maxAmount;

                if ((part.Resources[resourceName].amount > part.Resources[resourceName].maxAmount) || HighLogic.LoadedSceneIsEditor)
                    part.Resources[resourceName].amount = part.Resources[resourceName].maxAmount;
            }

            MonoUtilities.RefreshContextWindows(part);
            GameEvents.onPartResourceListChange.Fire(part);
        }
        #endregion
    }
}

