using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KSP.Localization;
using WildBlueCore.Utilities;

namespace WildBlueCore.PartModules.Resources
{
    /// <summary>
    ///  Derived from ModuleFuelPup, this part module provides periodic refills of the resources contained in the storage tank.
    ///  The storage tank can be either the part that hosts this part module, or, if the host part has no resources, then the tank is
    ///  the part that the supply line part is attached to. It makes the assumption that the storage tank is completely full when it arrives
    ///  at the desired destination; how it gets there is up to the player. The part module allows players to specify how long, in hours, 
    ///  it takes between supply runs. It also can optionally charge for the cost of the resources upon delivery.
    ///  When a delivery is made, the part module can play an EFFECT and/or run an animation.
    /// </summary>
    public class ModuleSupplyLine: ModuleFuelPump
    {
        #region Constants
        const float kMaxTransferTime = 216000f;
        #endregion

        #region Fields

        [KSPField]
        public string deliveryAnimationName = string.Empty;

        [KSPField]
        public string deliveryEffectName = string.Empty;

        /// <summary>
        /// Flag to enable periodic transfers. Every transferPeriod, the fuel pump will immediately refill the tank and distribute the contents
        /// </summary>
        [KSPField(guiActive = true, isPersistant = true, guiActiveEditor = true, groupName = "SupplyLine", groupDisplayName = "#LOC_WILDBLUECORE_supplyLineTitle", guiName = "#LOC_WILDBLUECORE_periodicTransfer")]
        [UI_Toggle(enabledText = "#LOC_WILDBLUECORE_fuelPumpEnabled", disabledText = "#LOC_WILDBLUECORE_fuelPumpDisabled")]
        public bool transfersEnabled = false;

        /// <summary>
        /// In hours, how long to wait before magically refilling the tank and distributing the contents.
        /// </summary>
        [KSPField(guiActive = true, isPersistant = true, guiActiveEditor = true, groupName = "SupplyLine", groupDisplayName = "#LOC_WILDBLUECORE_supplyLineTitle", guiName = "#LOC_WILDBLUECORE_transferTime", guiFormat = "n0", guiUnits = "hrs")]
        [UI_FloatRange(affectSymCounterparts = UI_Scene.All, minValue = 0f, maxValue = kMaxTransferTime, stepIncrement = 0.1f)]
        public float transferTime = 0f;

        /// <summary>
        /// Last time the pump was updated.
        /// </summary>
        [KSPField(isPersistant = true)]
        public double lastUpdated = -1f;

        /// <summary>
        /// Flag indicating that the pump is recording mission time.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool isRecordingTime;

        /// <summary>
        /// Last time the pump was updated.
        /// </summary>
        [KSPField(isPersistant = true)]
        public double missionStartTime = -1f;

        /// <summary>
        /// Last time the pump was updated.
        /// </summary>
        [KSPField(isPersistant = true)]
        public double missionStopTime = -1f;

        /// <summary>
        /// In seconds, elapsed mission time.
        /// </summary>
        [KSPField(guiActive = true, isPersistant = true, guiActiveEditor = false, groupName = "SupplyLine", groupDisplayName = "#LOC_WILDBLUECORE_supplyLineTitle", guiName = "#LOC_WILDBLUECORE_supplyLineElapsedTime", guiFormat = "n1", guiUnits = "hrs")]
        public double missionElapsedTime = 0f;

        /// <summary>
        /// Flag to indicate whether or not the player should be charged for resource deliveries
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool chargeForResources;

        /// <summary>
        /// Flag to indicate whether or not the player should be charged a flat fee to deliver resources
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool payFlatFee;
        #endregion

        #region Housekeeping
        bool transfersWereEnabled;
        bool effectPlayed;
        bool animationPlayed;
        ModuleAnimateGeneric animationModule = null;
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            // Periodic Transfers
            transfersWereEnabled = transfersEnabled;

            // UI
            Fields["isActivated"].guiActive = false;
            Fields["isActivated"].guiActiveEditor = false;
            Fields["pumpRate"].guiActive = false;
            Fields["pumpRate"].guiActiveEditor = false;
            Fields["pumpStatus"].group.name = "SupplyLine";
            Fields["pumpMode"].group.name = "SupplyLine";
            Fields["pumpRate"].group.name = "SupplyLine";
            Fields["isActivated"].group.name = "SupplyLine";

            if (isRecordingTime)
            {
                Events["RecordStartTime"].active = false;
                Events["RecordEndTime"].active = true;
            }
            else
            {
                Events["RecordStartTime"].active = true;
                Events["RecordEndTime"].active = false;
            }

            wasActivated = isActivated;
            updatePumpModeUI();

            // Animation
            List<ModuleAnimateGeneric> animationModules = part.FindModulesImplementing<ModuleAnimateGeneric>();
            int count = animationModules.Count;
            for (int index = 0; index < count; index++)
            {
                if (animationModules[index].animationName == deliveryAnimationName)
                {
                    animationModule = animationModules[index];
                    break;
                }
            }
        }

        public override string GetInfo()
        {
            return Localizer.Format("#LOC_WILDBLUECORE_supplyLineInfo");
        }
        #endregion

        #region Events
        [KSPEvent(guiName = "#LOC_WILDBLUECORE_supplyLineStartTime", guiActive = true, guiActiveEditor = false, guiActiveUnfocused = true, unfocusedRange = 5.0f, groupName = "SupplyLine", groupDisplayName = "#LOC_WILDBLUECORE_supplyLineTitle")]
        public void RecordStartTime()
        {
            isRecordingTime = true;
            Events["RecordStartTime"].active = false;
            Events["RecordEndTime"].active = true;
            missionStartTime = Planetarium.GetUniversalTime();
            missionStopTime = 0;
            missionElapsedTime = 0;
        }

        [KSPEvent(guiName = "#LOC_WILDBLUECORE_supplyLineStopTime", guiActive = true, guiActiveEditor = false, guiActiveUnfocused = true, unfocusedRange = 5.0f, groupName = "SupplyLine", groupDisplayName = "#LOC_WILDBLUECORE_supplyLineTitle")]
        public void RecordEndTime()
        {
            isRecordingTime = false;
            Events["RecordStartTime"].active = true;
            Events["RecordEndTime"].active = false;
            missionStopTime = Planetarium.GetUniversalTime();
            missionElapsedTime = (missionStopTime - missionStartTime) / 3600f;
            if (missionElapsedTime > kMaxTransferTime)
                transferTime = kMaxTransferTime;
            else
                transferTime = (float)missionElapsedTime;
        }
        #endregion

        #region Update Loops
        internal override void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            isActivated = true;

            base.FixedUpdate();

            // Check resource transfer state
            if (transfersWereEnabled != transfersEnabled)
            {
                transfersWereEnabled = transfersEnabled;
                lastUpdated = 0f;
            }

            // Record mission time
            if (isRecordingTime)
            {
                missionElapsedTime = (Planetarium.GetUniversalTime() - missionStartTime) / 3600f;
                Fields["missionElapsedTime"].guiName = Localizer.Format("#LOC_WILDBLUECORE_supplyLineElapsedTime");
            }

            // If transfers are enabled, then make sure the pump is activated
            if (transfersEnabled)
            {
                // Perform the resource transfer if needed.
                performResourceTransferIfNeeded();
            }

            part.Effect(deliveryEffectName, 0f);
        }
        #endregion


        #region Helpers
        private void performResourceTransferIfNeeded()
        {
            if (isRecordingTime || transferTime <= 0 || !transfersEnabled || hostPart == null)
                return;
            if (lastUpdated <= 0)
                lastUpdated = Planetarium.GetUniversalTime();

            double elapsedTime = Planetarium.GetUniversalTime() - lastUpdated;
            double transferTimeSeconds = transferTime * 3600;

            // Show time remaining until next transfer
            if (elapsedTime < transferTimeSeconds)
            {
                missionElapsedTime = (transferTimeSeconds - elapsedTime) / 3600f;
                Fields["missionElapsedTime"].guiName = Localizer.Format("#LOC_WILDBLUECORE_supplyLineNextTransferTime");
            }

            // Run through the completed transfer cycles
            while (elapsedTime > transferTimeSeconds)
            {
                // Fill the tank
                fillTankResources();

                // Distribute resources
                isActivated = true;
                wasActivated = isActivated;
                DistributeResources(100f);

                // Update elapsedTime
                elapsedTime -= transferTimeSeconds;

                if (elapsedTime <= transferTimeSeconds)
                    lastUpdated = Planetarium.GetUniversalTime() + Math.Abs(elapsedTime);

                // Play effect
                if (!effectPlayed)
                {
                    effectPlayed = true;
                    part.Effect(deliveryEffectName, 1f);
                }

                // Play animation
                if (!animationPlayed && animationModule != null)
                {
                    animationPlayed = true;
                    animationModule.Toggle();
                }
            }

            effectPlayed = false;
            animationPlayed = false;
        }

        private void fillTankResources()
        {
            int count = hostPart.Resources.Count;
            PartResource resource = null;
            for (int index = 0; index < count; index++)
            {
                resource = hostPart.Resources[index];
                resource.amount = resource.maxAmount;
            }
        }
        #endregion
    }
}
