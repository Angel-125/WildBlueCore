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

    internal enum PumpingMode
    {
        distributeLocally,
        sendToRemote,
        receiveFromRemote,
        addToDeepStore,
        removeFromDeepStore
    }

    /// <summary>
    /// This part module pumps one or more resources from the host part to other parts that have the same resource. The module can be directly added to a resource tank part or to a part that is
    /// radially attached to a resource tank part. When enabled, WBIModuleFuelPump will automatically pump resources until either the host part's resource is empty or when the destination parts are full. In either case,
    /// it will wait until the host part gains more resources to pump or the destination parts gain more room to store the resource.
    /// </summary>
    /// <remarks>
    /// WBIModuleFuelPump will transfer resources based on a part's Flow Priority. Higher priority parts will receive resources before lower priority parts.  
    /// </remarks>
    /// <remarks>
    /// WBIModuleFuelPump is designed to pump resources throughout the same vessel, but it can also pump resources to a nearby vessel if it is also equipped with a part that has a WBIModuleFuelPump.  
    /// </remarks>
    /// <remarks>
    /// To pump a resource throughout the same vessel, the following conditions must be met:  
    /// </remarks>
    /// <li>The fuel pump providing resources must be set to Distribute Localy.</li>
    /// <li>The resource must not be empty.</li>
    /// <li>The resource must be transferrable and unlocked.</li>
    /// <li>The destination parts must have space available to receive the pumped resource.</li>
    /// <li>The destination parts' resource storage must be unlocked.</li>
    /// <remarks>
    /// To pump a resource to another nearby vessel, the following conditions must be met:  
    /// </remarks>
    /// <li>The resource must not be empty.</li>
    /// <li>The resource must be transferrable and unlocked.</li>
    /// <li>The destination parts must have space available to receive the pumped resource.</li>
    /// <li>The destination parts' resource storage must be unlocked.</li>
    /// <li>All vessels must be either landed or splashed.</li>
    /// <li>The nearby vessels must be within the provider's pump range.</li>
    /// <li>The pump providing resources must have its pump mode set to Send to remote.</li>
    /// <li>The pumps that receive resources must be set to Receive from remote.</li>
    /// <example>
    /// <code>
    /// MODULE
    /// {
    ///     name = WBIModuleFuelPump
    ///     maxRemotePumpRange = 200
    /// }
    /// </code>
    /// </example>
    public class WBIModuleFuelPump: WBIBasePartModule
    {
        #region Constants
        const double kAmountThreshold = 1e-8;
        const float kMaxTransferTime = 216000f;
        const double kPauseDuration = 3.0f;
        #endregion

        #region Custom GameEvents
        /// <summary>
        /// Signals when the isActivated and/or remotePumpMode changes.
        /// </summary>
        public static EventData<WBIModuleFuelPump> onPumpStateChanged = new EventData<WBIModuleFuelPump>("onPumpStateChanged");

        public static EventData<WBIModuleFuelPump> onReloadPumpVessels = new EventData<WBIModuleFuelPump>("onReloadPumpVessels");
        #endregion

        #region Fields
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, groupName = "FuelPump", groupDisplayName = "#LOC_WILDBLUECORE_fuelPumpTitle", guiName = "#LOC_WILDBLUECORE_fuelPumpTitle")]
        [UI_Toggle(enabledText = "#LOC_WILDBLUECORE_fuelPumpEnabled", disabledText = "#LOC_WILDBLUECORE_fuelPumpDisabled")]
        public bool isActivated = false;
        
        [KSPField(guiActive = true, groupName = "FuelPump", groupDisplayName = "#LOC_WILDBLUECORE_fuelPumpTitle", guiName = "#LOC_WILDBLUECORE_fuelPumpStatus")]
        public string pumpStatus = string.Empty;

        [KSPField(isPersistant = true)]
        internal PumpingMode pumpMode;

        [KSPField(guiActive = true, isPersistant = true, guiActiveEditor = true, groupName = "FuelPump", groupDisplayName = "#LOC_WILDBLUECORE_fuelPumpTitle", guiName = "#LOC_WILDBLUECORE_fuelPumpRate", guiFormat = "n0", guiUnits = "%")]
        [UI_FloatRange(affectSymCounterparts = UI_Scene.All, minValue = 1f, maxValue = 20f, stepIncrement = 1f)]
        public float pumpRate = 10f;

        /// <summary>
        /// In meters, the maximum range that the fuel pump can reach when remote pumping resources. Default is 2000 meters.
        /// </summary>
        [KSPField()]
        public float maxRemotePumpRange = 2000f;

        /// <summary>
        /// Flag to indicate that the part that has the WBIModuleFuelPump is the host part.
        /// </summary>
        [KSPField]
        public bool selfIsHostPart = true;
        #endregion

        #region Housekeeping
        internal Part hostPart = null;
        internal MutablePartSet resourcePartSet = null;
        internal FuelPumpState pumpState = FuelPumpState.disabled;
        internal int loadedVesselsCount = -1;
        internal WBIModuleFuelPump[] remoteFuelPumps;
        internal PumpingMode prevPumpMode;
        internal bool wasActivated;
        internal bool isPaused = false;
        internal double resumeOpsTimestamp;

        string cacheStringStatusPumpOff;
        string cacheStringStatusPumping;
        string cacheStringStatusDestinationsFull;
        string cacheStringStatusSourceEmpty;
        string cacheStringStatusReceiveReady;
        string cacheStringStatusSendReady;
        string cacheStringStatusDistributeReady;
        string cacheStringModeLocal;
        string cacheStringModeRemoteSend;
        string cacheStringModeRemoteReceive;
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            cacheStringStatusPumpOff = Localizer.Format("#LOC_WILDBLUECORE_fuelPumpDisabled");
            cacheStringStatusPumping = Localizer.Format("#LOC_WILDBLUECORE_fuelPumpStatusPumping");
            cacheStringStatusDestinationsFull = Localizer.Format("#LOC_WILDBLUECORE_fuelPumpStatusDestinationsFull");
            cacheStringStatusSourceEmpty = Localizer.Format("#LOC_WILDBLUECORE_fuelPumpStatusSourceEmpty");
            cacheStringModeLocal = Localizer.Format("#LOC_WILDBLUECORE_fuelPumpModeLocal");
            cacheStringModeRemoteSend = Localizer.Format("#LOC_WILDBLUECORE_fuelPumpModeRemoteSend");
            cacheStringModeRemoteReceive = Localizer.Format("#LOC_WILDBLUECORE_fuelPumpModeRemoteReceive");
            cacheStringStatusReceiveReady = Localizer.Format("#LOC_WILDBLUECORE_fuelPumpStatusReceiveReady");
            cacheStringStatusSendReady = Localizer.Format("#LOC_WILDBLUECORE_fuelPumpStatusSendReady");
            cacheStringStatusDistributeReady = Localizer.Format("#LOC_WILDBLUECORE_fuelPumpStatusDistrubuteReady");

            wasActivated = isActivated;
            updatePumpModeUI();

            if (!HighLogic.LoadedSceneIsFlight)
                return;

            // Get host part
            findHostPart();

            // Init previous pump mode.
            prevPumpMode = pumpMode;

            // Rebuild the part set
            rebuildPartSet();

            // Update our loaded vessels cache
            updateLoadedVesselsCache();

            // Add events
            GameEvents.onVesselPartCountChanged.Add(onVesselPartCountChanged);
            GameEvents.onPartResourceFullNonempty.Add(onPartResourceFullNonempty);
            GameEvents.onPartResourceEmptyNonempty.Add(onPartResourceEmptyNonempty);
            GameEvents.onPartResourceNonemptyEmpty.Add(onPartResourceNonemptyEmpty);
            GameEvents.onPartResourceEmptyFull.Add(onPartResourceEmptyFull);
            GameEvents.onPartResourceFullEmpty.Add(onPartResourceFullEmpty);
            GameEvents.onPartPriorityChanged.Add(onPartPriorityChanged);
            onPumpStateChanged.Add(pumpStateChanged);
            onReloadPumpVessels.Add(reloadPumpVessels);
        }

        public override string GetInfo()
        {
            return Localizer.Format("#LOC_WILDBLUECORE_fuelPumpModuleInfo");
        }
        #endregion

        #region Actions
        /// <summary>
        /// Turns off the fuel pump.
        /// </summary>
        /// <param name="param">A KSPActionParam containing the action parameters.</param>
        [KSPAction("#LOC_WILDBLUECORE_fuelPumpModeOn")]
        public void ActionFuelPumpOn(KSPActionParam param)
        {
            isActivated = true;
            updatePumpActivationState();
        }

        /// <summary>
        /// Turns off the fuel pump.
        /// </summary>
        /// <param name="param">A KSPActionParam containing the action parameters.</param>
        [KSPAction("#LOC_WILDBLUECORE_fuelPumpModeOff")]
        public void ActionFuelPumpOff(KSPActionParam param)
        {
            isActivated = false;
            updatePumpActivationState();
        }

        /// <summary>
        /// Sets pump mode to local distribution.
        /// </summary>
        /// <param name="param">A KSPActionParam containing the action parameters.</param>
        [KSPAction("#LOC_WILDBLUECORE_fuelPumpModeLocal")]
        public void ActionFuelPumpLocal(KSPActionParam param)
        {
            pumpMode = PumpingMode.distributeLocally;
            updatePumpActivationState();
        }

        /// <summary>
        /// Sets the pump mode to send to remote pumps.
        /// </summary>
        /// <param name="param">A KSPActionParam containing the action parameters.</param>
        [KSPAction("#LOC_WILDBLUECORE_fuelPumpModeRemoteSend")]
        public void ActionFuelPumpRemoteSend(KSPActionParam param)
        {
            pumpMode = PumpingMode.sendToRemote;
            updatePumpActivationState();
        }

        /// <summary>
        /// Sets the pump mode to receive from remote pumps.
        /// </summary>
        /// <param name="param">A KSPActionParam containing the action parameters.</param>
        [KSPAction("#LOC_WILDBLUECORE_fuelPumpModeRemoteReceive")]
        public void ActionFuelPumpModeReceive(KSPActionParam param)
        {
            pumpMode = PumpingMode.receiveFromRemote;
            updatePumpActivationState();
        }

        private void updatePumpActivationState()
        {
            loadedVesselsCount = -1;
            rebuildPartSet();
            updateLoadedVesselsCache();
            onPumpStateChanged.Fire(this);
            onReloadPumpVessels.Fire(this);
        }
        #endregion

        #region Events
        [KSPEvent(guiName = "#LOC_WILDBLUECORE_fuelPumpModeOff", guiActive = true, guiActiveEditor = true, guiActiveUnfocused = true, unfocusedRange = 5.0f, groupName = "FuelPump", groupDisplayName = "#LOC_WILDBLUECORE_fuelPumpTitle")]
        public void CyclePumpMode()
        {
            switch (pumpMode)
            {
                case PumpingMode.distributeLocally:
                    pumpMode = PumpingMode.sendToRemote;
                    break;

                case PumpingMode.sendToRemote:
                    pumpMode = PumpingMode.receiveFromRemote;
                    break;

                case PumpingMode.receiveFromRemote:
                    pumpMode = PumpingMode.distributeLocally;
                    break;
            }

            if (HighLogic.LoadedSceneIsFlight)
                updatePumpActivationState();
            updatePumpModeUI();
        }
        #endregion

        #region Update Loops
        internal virtual void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            // If we're not activated then we're done.
            if (!isActivated)
            {
                pumpState = FuelPumpState.disabled;
                pumpStatus = cacheStringStatusPumpOff;
                return;
            }

            if (isPaused && Planetarium.GetUniversalTime() > resumeOpsTimestamp)
            {
                isPaused = false;
                updatePumpActivationState();
                updatePumpModeUI();
                pumpState = FuelPumpState.pumping;
                prevPumpMode = pumpMode;
                onPumpStateChanged.Fire(this);
            }

            // Check activation
            if (isActivated != wasActivated)
            {
                wasActivated = isActivated;
                if (isActivated)
                    isPaused = false;
                updatePumpActivationState();
                updatePumpModeUI();
            }

            // Update our loaded vessels cache
            updateLoadedVesselsCache();

            // Check previous pump mode. If it's changed then reset the pump state since we might be stuck.
            if (prevPumpMode != pumpMode)
            {
                prevPumpMode = pumpMode;
                pumpState = FuelPumpState.pumping;
                onPumpStateChanged.Fire(this);
            }

            // Check pump state
            if (pumpState != FuelPumpState.pumping)
            {
                checkForResources();
                updatePumpStatusDisplay();
                return;
            }

            // Distribute resources
            DistributeResources();

            // Update status display
            updatePumpStatusDisplay();
        }
        #endregion

        #region API
        /// <summary>
        /// This method will attempt to distribute any resources that the host part has to other parts in the vessel or to nearby vessels. The resources must be capable of being transferred, and they must be unlocked.
        /// Additionally, to remotely distribute the resources, remotePumpMode must be set to true, the nearby vessel must have at least one WBIModuleFuelPump, and the nearby vessel's fuel pump' isActivated must be set to true.
        /// </summary>
        public void DistributeResources(float pumpRateOverride = -1f)
        {
            if (!isActivated || pumpMode == PumpingMode.receiveFromRemote)
                return;

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
                    resource.amount <= kAmountThreshold
                    )
                    continue;

                // We know that at least one resource isn't empty.
                localResourcesAreEmpty = false;

                // Calculate pump rate
                float adjustedPumpRate = (pumpRate / 100) * TimeWarp.fixedDeltaTime;
                if (pumpRateOverride > 0)
                    adjustedPumpRate = pumpRateOverride / 100f;

                // Calculate transfer amount
                transferAmount = resource.maxAmount * adjustedPumpRate;
                if (transferAmount >= resource.amount)
                {
                    transferAmount = resource.amount;
                    resource.amount = 0;
                }
                else
                {
                    resource.amount -= transferAmount;
                    if (resource.amount < 0)
                        resource.amount = 0;
                }

                // Distribute the resource.
                switch (pumpMode)
                {
                    case PumpingMode.distributeLocally:
                        tanksWereFilled = DistributeResourceLocally(resource, transferAmount);
                        break;

                    case PumpingMode.sendToRemote:
                        tanksWereFilled = distributeResourceRemotely(resource, transferAmount);
                        break;

                    default:
                        break;
                }
            }

            // Update the state
            // If localResourcesAreEmpty then we need to stop pumping and wait until we have more resources.
            if (localResourcesAreEmpty)
            {
                pumpState = FuelPumpState.sourceIsEmpty;
                isPaused = true;
                resumeOpsTimestamp = Planetarium.GetUniversalTime() + kPauseDuration;
            }

            // If !tanksWereFilled then all the destination tanks are full and we should wait until one or more of them has room.
            else if (!tanksWereFilled)
            {
                pumpState = FuelPumpState.destinationsAreFull;
                isPaused = true;
                resumeOpsTimestamp = Planetarium.GetUniversalTime() + kPauseDuration;
            }
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
            // As a courtesy, if we are receiving from a remote pump we can skip this check.
            if (!hostPart.Resources.Contains(resource.info.id) && resource.resourceName != "ElectricCharge")
            {
                // No resource was transferred so give it back to the host tank.
                resource.amount += Math.Abs(transferAmount);

                return false;
            }

            // Distribute the resource and process the result.
            double amountTransferred = hostPart.RequestResource(resource.info.id, -transferAmount, resource.info.resourceFlowMode, false);

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
            else if (!Equals(Math.Abs(transferAmount), Math.Abs(amountTransferred)))
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
            {
                pumpState = FuelPumpState.pumping;
            }
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
            {
                pumpState = FuelPumpState.pumping;
            }
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

        private void pumpStateChanged(WBIModuleFuelPump fuelPump)
        {
            if (fuelPump.hostPart.vessel != hostPart.vessel || fuelPump == this)
                return;

            rebuildPartSet();
        }

        private void reloadPumpVessels(WBIModuleFuelPump fuelPump)
        {
            if (fuelPump == this)
                return;

            loadedVesselsCount = -1;
            rebuildPartSet();
            updateLoadedVesselsCache();
        }
        #endregion

        #region Helpers
        private void findHostPart()
        {
            if (selfIsHostPart)
            {
                hostPart = part;
                return;
            }

            if (part.parent != null)
                hostPart = part.parent;
            else
                hostPart = part;
        }

        private bool distributeResourceRemotely(PartResource resource, double transferAmount)
        {
            // Our vessel must be landed or splashed.
            if (!hostPart.vessel.LandedOrSplashed)
                return false;

            // First, locate the pumps that are active, in range, and landed or splashed.
            List<WBIModuleFuelPump> activePumps = new List<WBIModuleFuelPump>();
            WBIModuleFuelPump remotePump;
            for (int index = 0; index < remoteFuelPumps.Length; index++)
            {
                remotePump = remoteFuelPumps[index];

                // Remote pump must be activated.
                if (!remotePump.isActivated)
                    continue;

                // The remote pump must be set to receive resources
                if (remotePump.pumpMode != PumpingMode.receiveFromRemote)
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

            List<WBIModuleFuelPump> remotePumps = new List<WBIModuleFuelPump>();
            List<WBIModuleFuelPump> fuelPumps = null;

            Vessel loadedVessel;
            for (int index = 0; index < count; index++)
            {
                loadedVessel = FlightGlobals.VesselsLoaded[index];
                if (loadedVessel == hostPart.vessel)
                    continue;

                // We're looking for loaded vessels with fuel pumps.
                fuelPumps = loadedVessel.FindPartModulesImplementing<WBIModuleFuelPump>();
                if (fuelPumps != null && fuelPumps.Count > 0)
                    remotePumps.AddRange(fuelPumps);
            }

            remoteFuelPumps = remotePumps.ToArray();
        }

        internal void updatePumpStatusDisplay()
        {
            switch (pumpState)
            {
                case FuelPumpState.disabled:
                    pumpStatus = cacheStringStatusPumpOff;
                    break;
                case FuelPumpState.pumping:
                    if (pumpMode == PumpingMode.receiveFromRemote)
                        pumpStatus = cacheStringStatusReceiveReady;
                    else
                        pumpStatus = cacheStringStatusPumping;
                    break;
                case FuelPumpState.destinationsAreFull:
                    pumpStatus = cacheStringStatusDestinationsFull;
                    break;
                case FuelPumpState.sourceIsEmpty:
                    pumpStatus = cacheStringStatusSourceEmpty;
                    break;
            }
        }

        internal void updatePumpModeUI()
        {
            switch (pumpMode)
            {
                default:
                case PumpingMode.distributeLocally:
                    Events["CyclePumpMode"].guiName = cacheStringModeLocal;
                    pumpStatus = cacheStringStatusDistributeReady;
                    break;

                case PumpingMode.sendToRemote:
                    Events["CyclePumpMode"].guiName = cacheStringModeRemoteSend;
                    pumpStatus = cacheStringStatusSendReady;
                    break;

                case PumpingMode.receiveFromRemote:
                    Events["CyclePumpMode"].guiName = cacheStringModeRemoteReceive;
                    pumpStatus = cacheStringStatusReceiveReady;
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
            onPumpStateChanged.Remove(pumpStateChanged);
            onReloadPumpVessels.Remove(reloadPumpVessels);
        }

        private void rebuildPartSet()
        {
            resourcePartSet = new MutablePartSet(hostPart.vessel);
            resourcePartSet.BuildLists(hostPart);
            resourcePartSet.RemovePartFromLists(hostPart);

            List<WBIModuleFuelPump> fuelPumps = hostPart.vessel.FindPartModulesImplementing<WBIModuleFuelPump>();
            WBIModuleFuelPump fuelPump;
            int count = fuelPumps.Count;
            for (int index = 0; index < count; index++)
            {
                fuelPump = fuelPumps[index];
                if (fuelPump == this || fuelPump.pumpMode != PumpingMode.sendToRemote)
                    continue;

                resourcePartSet.RemovePartFromLists(fuelPump.hostPart);
            }
        }

        private void checkForResources()
        {
            // If we're activated and our pump state is disabled, enable the pump state.
            if (isActivated && pumpState == FuelPumpState.disabled)
            {
                pumpState = FuelPumpState.pumping;
                loadedVesselsCount = -1;
                updateLoadedVesselsCache();
                return;
            }

            // If we're not activated or we aren't waiting for our host tank to acquire resources then we're done.
            else if (!isActivated || pumpState != FuelPumpState.sourceIsEmpty)
            {
                return;
            }

            // Fallback: if we don't receive an event that we have resources again then check to see if we're non-empty.
            int count = hostPart.Resources.Count;
            PartResource resource;
            for (int index = 0; index < count; index++)
            {
                resource = hostPart.Resources[index];

                if (resource.info.resourceFlowMode == ResourceFlowMode.NO_FLOW ||
                    resource.info.resourceFlowMode == ResourceFlowMode.NULL ||
                    !resource.flowState ||
                    resource.flowMode == PartResource.FlowMode.None ||
                    resource.amount <= kAmountThreshold
                    )
                    continue;

                pumpState = FuelPumpState.pumping;
                return;
            }
        }
        #endregion
    }
}
