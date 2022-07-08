using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KSP.Localization;
using WildBlueCore.Utilities;

namespace WildBlueCore.PartModules.Resources
{
    internal enum FuelPumpState
    {
        disabled,
        pumping,
        sourceIsEmpty,
        destinationsAreFull
    }

    /// <summary>
    /// This part module pumps one or more resources from the host part to other parts that have the same resource. The module can be directly added to a resource tank part or to a part that is
    /// radially attached to a resource tank part. When enabled, ModuleFuelPump will automatically pump resources until either the host part's resource is empty or when the destination parts are full. In either case,
    /// it will wait until the host part gains more resources to pump or the destination parts gain more room to store the resource.
    /// </summary>
    /// <remarks>
    /// ModuleFuelPump will transfer resources based on a part's Flow Priority. Higher priority parts will receive resources before lower priority parts.  
    /// </remarks>
    /// <remarks>
    /// ModuleFuelPump is designed to pump resources throughout the same vessel, but it can also pump resources to a nearby vessel if it is also equipped with a part that has a ModuleFuelPump.  
    /// </remarks>
    /// <remarks>
    /// To pump a resource throughout the same vessel, the following conditions must be met:  
    /// </remarks>
    /// <li>The fuel pump providing resources must be set to Enabled.</li>
    /// <li>The resource must not be empty.</li>
    /// <li>The resource must be transferrable and unlocked.</li>
    /// <li>The destination parts must have space available to receive the pumped resource.</li>
    /// <li>The destination parts' resource storage must be unlocked.</li>
    /// <remarks>
    /// To pump a resource to another nearby vessel, in addition to the above conditions, the following conditions must also be met:  
    /// </remarks>
    /// <li>All vessels must be either landed or splashed.</li>
    /// <li>The nearby vessels must be within the provider's pump range.</li>
    /// <li>The pump providing resources must have its pump mode set to Remote.</li>
    /// <li>The pumps that receive resources must be set to Enabled.</li>
    /// <example>
    /// <code>
    /// MODULE
    /// {
    ///     name = ModuleFuelPump
    ///     maxRemotePumpRange = 200
    /// }
    /// </code>
    /// </example>
    public class ModuleFuelPump: BasePartModule
    {
        #region Fields
        [KSPField(guiActive = true, groupName = "FuelPump", groupDisplayName = "#LOC_WILDBLUECORE_fuelPumpTitle", guiName = "#LOC_WILDBLUECORE_fuelPumpStatus")]
        string pumpStatus = string.Empty;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, groupName = "FuelPump", groupDisplayName = "#LOC_WILDBLUECORE_fuelPumpTitle", guiName = "#LOC_WILDBLUECORE_fuelPumpState")]
        [UI_Toggle(enabledText = "#LOC_WILDBLUECORE_fuelPumpEnabled", disabledText = "#LOC_WILDBLUECORE_fuelPumpDisabled")]
        bool isActivated = false;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, groupName = "FuelPump", groupDisplayName = "#LOC_WILDBLUECORE_fuelPumpTitle", guiName = "#LOC_WILDBLUECORE_fuelPumpMode")]
        [UI_Toggle(enabledText = "#LOC_WILDBLUECORE_fuelPumpModeRemote", disabledText = "#LOC_WILDBLUECORE_fuelPumpModeLocal")]
        bool remotePumpMode = false;

        [KSPField(guiActive = true, isPersistant = true, guiActiveEditor = true, groupName = "FuelPump", groupDisplayName = "#LOC_WILDBLUECORE_fuelPumpTitle", guiName = "#LOC_WILDBLUECORE_fuelPumpRate", guiFormat = "n0", guiUnits = "%")]
        [UI_FloatRange(affectSymCounterparts = UI_Scene.All, minValue = 1f, maxValue = 20f, stepIncrement = 1f)]
        float pumpRate = 10f;

        /// <summary>
        /// In meters, the maximum range that the fuel pump can reach when remote pumping resources. Default is 200 meters.
        /// </summary>
        [KSPField()]
        public float maxRemotePumpRange = 200f;
        #endregion

        #region Housekeeping
        Part hostPart = null;
        MutablePartSet resourcePartSet = null;
        FuelPumpState pumpState = FuelPumpState.disabled;
        int loadedVesselsCount = -1;
        ModuleFuelPump[] remoteFuelPumps;
        bool prevPumpMode = false;
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            // Get host part
            if (part.parent != null)
                hostPart = part.parent;
            else
                hostPart = part;

            // Rebuild the part set
            rebuildPartSet();

            // Update our loaded vessels cache
            updateLoadedVesselsCache();

            // Capture previous pump mode.
            prevPumpMode = remotePumpMode;

            // Add events
            GameEvents.onVesselPartCountChanged.Add(onVesselPartCountChanged);
            GameEvents.onPartResourceFullNonempty.Add(onPartResourceFullNonempty);
            GameEvents.onPartResourceEmptyNonempty.Add(onPartResourceEmptyNonempty);
            GameEvents.onPartResourceNonemptyEmpty.Add(onPartResourceNonemptyEmpty);
            GameEvents.onPartResourceEmptyFull.Add(onPartResourceEmptyFull);
            GameEvents.onPartResourceFullEmpty.Add(onPartResourceFullEmpty);
            GameEvents.onPartPriorityChanged.Add(onPartPriorityChanged);
        }

        public override string GetInfo()
        {
            return Localizer.Format("#LOC_WILDBLUECORE_fuelPumpModuleInfo");
        }
        #endregion

        #region Update Loops
        private void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            if (!isActivated)
            {
                pumpState = FuelPumpState.disabled;
                pumpStatus = "";
                return;
            }

            // Update our loaded vessels cache
            updateLoadedVesselsCache();

            // Enable pump state if needed
            if (isActivated && pumpState == FuelPumpState.disabled)
                pumpState = FuelPumpState.pumping;

            // Check previous pump mode. If it's changed then reset the pump state since we might be stuck.
            if (prevPumpMode != remotePumpMode && pumpState != FuelPumpState.pumping)
            {
                prevPumpMode = remotePumpMode;
                pumpState = FuelPumpState.pumping;
            }

            // Check pump state
            if (pumpState != FuelPumpState.pumping)
                return;

            // Distribute resources
            DistributeResources();

            // Update status display
            updatePumpStatusDisplay();
        }
        #endregion

        #region API
        /// <summary>
        /// This method will attempt to distribute any resources that the host part has to other parts in the vessel or to nearby vessels. The resources must be capable of being transferred, and they must be unlocked.
        /// Additionally, to remotely distribute the resources, remotePumpMode must be set to true, the nearby vessel must have at least one ModuleFuelPump, and the nearby vessel's fuel pump' isActivated must be set to true.
        /// </summary>
        public void DistributeResources()
        {
            int count = hostPart.Resources.Count;
            if (count <= 0)
            {
                pumpState = FuelPumpState.disabled;
                return;
            }

            PartResource resource = null;
            double transferAmount = 0f;
            bool tanksWereFilled = false;
            bool localResourcesAreEmpty = true;
            for (int index = 0; index < count; index++)
            {
                resource = hostPart.Resources[index];

                // Skip any resource that is locked or has no flow mode or has no resource to transfer.
                if (resource.info.resourceFlowMode == ResourceFlowMode.NO_FLOW ||
                    resource.info.resourceFlowMode == ResourceFlowMode.NULL ||
                    !resource.flowState ||
                    resource.flowMode == PartResource.FlowMode.None ||
                    resource.amount <= 0
                    )
                    continue;

                // We know that at least one resource isn't empty.
                localResourcesAreEmpty = false;

                // Calculate transfer amount
                transferAmount = resource.maxAmount * (pumpRate / 100f) * TimeWarp.fixedDeltaTime;
                if (transferAmount >= resource.maxAmount)
                {
                    transferAmount = resource.maxAmount;
                    resource.amount = 0;
                }
                else
                {
                    resource.amount -= transferAmount;
                }

                // Distribute the resource.
                if (!remotePumpMode)
                    tanksWereFilled = DistributeResourceLocally(resource, transferAmount);
                else
                    tanksWereFilled = distributeResourceRemotely(resource, transferAmount);
            }

            // Update the state
            // If localResourcesAreEmpty then we need to stop pumping and wait until we have more resources.
            if (localResourcesAreEmpty)
                pumpState = FuelPumpState.sourceIsEmpty;

            // If !tanksWereFilled then all the destination tanks are full and we should wait until one or more of them has room.
            else if (!tanksWereFilled)
                pumpState = FuelPumpState.destinationsAreFull;
        }

        /// <summary>
        /// Distributes the desired resource locally throughout the vessel.
        /// </summary>
        /// <param name="resource">The PartResource to distribute.</param>
        /// <param name="transferAmount">A double containing how much of the resource to distrubute.</param>
        /// <param name="isFromRemotePump">A bool indicating whether or not the source is from a remote pump. Default is false.</param>
        /// <returns>True if the distribution was successful, false if not.</returns>
        public bool DistributeResourceLocally(PartResource resource, double transferAmount, bool isFromRemotePump = false)
        {
            bool tanksWereFilled = false;

            // Make sure that we have the resource.
            if (!hostPart.Resources.Contains(resource.info.id))
            {
                // No resource was transferred so give it back to the host tank.
                resource.amount += Math.Abs(transferAmount);
                return false;
            }

            // Distribute the resource and process the result.
            double amountTransferred = resourcePartSet.RequestResource(hostPart, resource.info.id, -transferAmount, true);

            // If no tanks were filled and the source is a remote pump then we can try to fill our own tanks.
            if (amountTransferred == 0 && isFromRemotePump)
                amountTransferred = hostPart.TransferResource(resource.info.id, transferAmount);

            // Now handle the transfer result.

            // Nothing transferred? Give back the transfer amount.
            if (amountTransferred == 0)
            {
                resource.amount += Math.Abs(transferAmount);
            }

            // Not all was tranferred? Give back what we didn't use.
            else if (!Equals(transferAmount, amountTransferred))
            {
                tanksWereFilled = true;
                resource.amount += Math.Abs(transferAmount) - Math.Abs(amountTransferred);
            }

            // Everything was transferred.
            else
            {
                tanksWereFilled = true;
            }

            return tanksWereFilled;
        }
        #endregion

        #region GameEvents
        #region Events that wait for host part to no longer be empty
        private void onPartResourceEmptyNonempty(PartResource resource)
        {
            if (resource.part != hostPart)
                return;
            if (!hostPart.Resources.Contains(resource.info.id))
                return;

            // If we're waiting for the host part's tanks to fill up again, then we can resume pumping.
            if (pumpState == FuelPumpState.sourceIsEmpty && isActivated)
                pumpState = FuelPumpState.pumping;
        }

        private void onPartResourceEmptyFull(PartResource resource)
        {
            onPartResourceEmptyNonempty(resource);
        }
        #endregion

        #region Events that wait for destination parts to have room
        private void onPartResourceFullNonempty(PartResource resource)
        {
            if (resource.part == hostPart)
                return;
            if (!hostPart.Resources.Contains(resource.info.id))
                return;

            // If we're waitng for a destination part to have room then we can resume pumping.
            if (pumpState == FuelPumpState.destinationsAreFull && isActivated)
                pumpState = FuelPumpState.pumping;
        }

        private void onPartResourceNonemptyEmpty(PartResource resource)
        {
            onPartResourceFullNonempty(resource);
        }

        private void onPartResourceFullEmpty(PartResource resource)
        {
            onPartResourceFullNonempty(resource);
        }
        #endregion

        private void onVesselPartCountChanged(Vessel changedVessel)
        {
            if (changedVessel != hostPart.vessel)
                return;

            // Rebuild our parts list.
            rebuildPartSet();
        }

        private void onPartPriorityChanged(Part changedPart)
        {
            if (changedPart.vessel == hostPart.vessel)
                rebuildPartSet();
        }
        #endregion

        #region Helpers
        private bool distributeResourceRemotely(PartResource resource, double transferAmount)
        {
            // Our vessel must be landed or splashed.
            if (!hostPart.vessel.LandedOrSplashed)
                return false;

            // First, locate the pumps that are active, in range, and landed or splashed.
            List<ModuleFuelPump> activePumps = new List<ModuleFuelPump>();
            ModuleFuelPump remotePump;
            for (int index = 0; index < remoteFuelPumps.Length; index++)
            {
                remotePump = remoteFuelPumps[index];

                // The remote pump must be activated
                if (!remotePump.isActivated)
                    continue;

                // The remote pump's vessel must be landed or splashed.
                if (!remotePump.vessel.LandedOrSplashed)
                    continue;

                // The remote pump's vessel must be within our maximum pumping range.
                if (Vector3d.Distance(hostPart.vessel.GetWorldPos3D(), remotePump.vessel.GetWorldPos3D()) > maxRemotePumpRange)
                    continue;

                // Add the pump to our list
                activePumps.Add(remotePump);
            }

            // Next, distribute the resource through the remote pumps
            int count = activePumps.Count;
            double amountPerVessel = transferAmount / count;
            bool tanksWereFilled = false;
            for (int index = 0; index < count; index++)
            {
                remotePump = activePumps[index];

                if (remotePump.DistributeResourceLocally(resource, amountPerVessel, true))
                    tanksWereFilled = true;
            }

            return tanksWereFilled;
        }

        private void updateLoadedVesselsCache()
        {
            int count = FlightGlobals.VesselsLoaded.Count;
            if (count == loadedVesselsCount)
                return;
            loadedVesselsCount = count;

            List<ModuleFuelPump> remotePumps = new List<ModuleFuelPump>();
            List<ModuleFuelPump> fuelPumps = null;

            Vessel loadedVessel;
            for (int index = 0; index < count; index++)
            {
                loadedVessel = FlightGlobals.VesselsLoaded[index];
                if (loadedVessel == hostPart.vessel)
                    continue;

                // We're looking for loaded vessels with fuel pumps.
                fuelPumps = loadedVessel.FindPartModulesImplementing<ModuleFuelPump>();
                if (fuelPumps != null && fuelPumps.Count > 0)
                    remotePumps.AddRange(fuelPumps);
            }

            remoteFuelPumps = remotePumps.ToArray();
        }

        private void updatePumpStatusDisplay()
        {
            switch (pumpState)
            {
                case FuelPumpState.disabled:
                    pumpStatus = "";
                    break;
                case FuelPumpState.pumping:
                    pumpStatus = Localizer.Format("#LOC_WILDBLUECORE_fuelPumpStatusPumping");
                    break;
                case FuelPumpState.destinationsAreFull:
                    pumpStatus = Localizer.Format("#LOC_WILDBLUECORE_fuelPumpStatusDestinationsFull");
                    break;
                case FuelPumpState.sourceIsEmpty:
                    pumpStatus = Localizer.Format("#LOC_WILDBLUECORE_fuelPumpStatusSourceEmpty");
                    break;
            }
        }

        private void OnDestroy()
        {
            GameEvents.onVesselPartCountChanged.Remove(onVesselPartCountChanged);
            GameEvents.onPartResourceFullNonempty.Remove(onPartResourceFullNonempty);
            GameEvents.onPartResourceEmptyNonempty.Remove(onPartResourceEmptyNonempty);
            GameEvents.onPartResourceNonemptyEmpty.Remove(onPartResourceNonemptyEmpty);
            GameEvents.onPartResourceEmptyFull.Remove(onPartResourceEmptyFull);
            GameEvents.onPartResourceFullEmpty.Remove(onPartResourceFullEmpty);
            GameEvents.onPartPriorityChanged.Remove(onPartPriorityChanged);
        }

        private void rebuildPartSet()
        {
            resourcePartSet = new MutablePartSet(hostPart.vessel);
            resourcePartSet.BuildLists(hostPart);
            resourcePartSet.RemovePartFromLists(hostPart);
            // Should we remove all parts that have ModuleFuelPump? That might eliminate circular distributions.
        }
        #endregion
    }
}
