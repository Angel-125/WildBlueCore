using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KSP.IO;
using KSP.Localization;

namespace WildBlueCore.PartModules.KerbalGear
{
    /// <summary>
    /// This part module enables resource transfers between inventory parts.
    /// </summary>
    public class WBIModuleEVAResourceTransfer : WBIBasePartModule
    {
        #region Fields
        [KSPField]
        public bool debugMode = true;
        #endregion

        #region Housekeeping
        KerbalEVA kerbalEVA;
        WBIResourceTransferGUI resourceTransferGUI = null;
        #endregion

        #region Events
        [KSPEvent(guiActive = true, guiName = "#LOC_WILDBLUECORE_transferResources")]
        public void TransferResources()
        {
            List<StoredPart> inventoryPartsWithResources = getInventoryPartsWithResources();

            if (inventoryPartsWithResources.Count <= 0)
            {
                return;
            }

            resourceTransferGUI.storedParts = inventoryPartsWithResources;
            resourceTransferGUI.SetVisible(true);
        }
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            kerbalEVA = part.FindModuleImplementing<KerbalEVA>();
            resourceTransferGUI = new WBIResourceTransferGUI();
        }

        /// <summary>
        /// Overrides OnInactive. Called when an inventory item is unequipped and the module is disabled.
        /// </summary>
        public override void OnInactive()
        {
            base.OnInactive();

            if (kerbalEVA == null)
                return;
        }

        /// <summary>
        /// Overrides OnActive. Called when an inventory item is equipped and the module is enabled.
        /// </summary>
        public override void OnActive()
        {
            base.OnActive();
        }
        #endregion

        #region helpers
        private List<StoredPart> getInventoryPartsWithResources()
        {
            List<StoredPart> inventoryPartsWithResources = new List<StoredPart>();
            StoredPart storedPart;
            ProtoPartResourceSnapshot resourceSnapshot;
            ModuleInventoryPart inventory;

            if (kerbalEVA == null)
                return inventoryPartsWithResources;

            // Get list of resources
            if (kerbalEVA.ModuleInventoryPartReference != null && kerbalEVA.ModuleInventoryPartReference.storedParts.Count > 0)
            {
                inventory = kerbalEVA.ModuleInventoryPartReference;

                int[] keys = inventory.storedParts.Keys.ToArray();
                for (int index = 0; index < keys.Length; index++)
                {
                    storedPart = inventory.storedParts[keys[index]];

                    int count = storedPart.snapshot.resources.Count;
                    if (count > 0)
                    {
                        inventoryPartsWithResources.Add(storedPart);

                        // List out the resources
                        if (debugMode)
                        {
                            Debug.Log("[WBIModuleEVAResourceTransfer] - Gathering resources for " + storedPart.partName);

                            for (int resourceIndex = 0; resourceIndex < count; resourceIndex++)
                            {
                                resourceSnapshot = storedPart.snapshot.resources[resourceIndex];
                                Debug.Log("[WBIModuleEVAResourceTransfer] - " + resourceSnapshot.resourceName + string.Format("{0:n2}/{1:n2}", resourceSnapshot.amount, resourceSnapshot.maxAmount));
                            }
                        }
                    }
                }
            }

            return inventoryPartsWithResources;
        }
        #endregion
    }
}
