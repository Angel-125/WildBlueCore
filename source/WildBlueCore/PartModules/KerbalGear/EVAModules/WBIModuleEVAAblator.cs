using System;
using KSP.Localization;
using UnityEngine;
using WildBlueCore.KerbalGear;

namespace WildBlueCore.KerbalGear
{
    /// <summary>
    /// Provides stock-style ablative cooling for an EVA kerbal. KerbalGear creates this module
    /// while carried equipment requests it, and WBIModuleEVAResourceTransfer exposes the
    /// equipment's stored coolant as a resource on the EVA part.
    /// </summary>
    /// <example>
    /// <code>
    /// MODULE
    /// {
    ///     name = WBIModuleWearableItem
    ///     moduleID = EVA Cooling Pack
    ///     evaModules = WBIModuleEVAResourceTransfer;WBIModuleEVAAblator
    /// }
    /// RESOURCE
    /// {
    ///     name = Ablator
    ///     amount = 10
    ///     maxAmount = 10
    /// }
    /// </code>
    /// </example>
    [KSPModule("#LOC_WILDBLUECORE_evaAblatorTitle")]
    public class WBIModuleEVAAblator : PartModule, IKerbalGearInventoryListener
    {
        #region Fields
        /// <summary>
        /// Resource consumed to remove heat from the EVA kerbal.
        /// </summary>
        [KSPField]
        public string ablativeResource = "Ablator";

        /// <summary>
        /// Constant multiplier in the stock ablation-rate equation.
        /// </summary>
        [KSPField]
        public double lossConst = 0.1;

        /// <summary>
        /// Exponent numerator in the stock ablation-rate equation. This must be negative.
        /// </summary>
        [KSPField]
        public double lossExp = -7500.0;

        /// <summary>
        /// Multiplier applied to the resource's specific heat to determine removed thermal flux.
        /// </summary>
        [KSPField]
        public double pyrolysisLossFactor = 6000.0;

        /// <summary>
        /// Skin temperature in kelvin above which cooling begins.
        /// </summary>
        [KSPField]
        public double ablationTempThresh = 500.0;

        /// <summary>
        /// Optional byproduct generated from consumed coolant.
        /// </summary>
        [KSPField]
        public string outputResource = string.Empty;

        /// <summary>
        /// Units of output generated per unit of coolant consumed.
        /// </summary>
        [KSPField]
        public double outputMult = 1.0;

        /// <summary>
        /// Current coolant mass-loss rate in kilograms per second. Visible with thermal data.
        /// </summary>
        [KSPField(guiActive = false, guiFormat = "N5", guiName = "#autoLOC_6001866",
            guiUnits = "#autoLOC_7001416")]
        public double loss;

        /// <summary>
        /// Current thermal flux removed from the EVA kerbal. Visible with thermal data.
        /// </summary>
        [KSPField(guiActive = false, guiFormat = "N2", guiName = "#autoLOC_6001867",
            guiUnits = "#autoLOC_7001417")]
        public double flux;
        #endregion

        #region Housekeeping
        PartResourceDefinition ablativeDefinition;
        PartResourceDefinition outputDefinition;
        BaseField lossField;
        BaseField fluxField;
        bool evaAblatorIsActive;
        bool invalidConfigurationLogged;
        #endregion

        #region Overrides
        /// <summary>
        /// Resolves configured resources and initializes the thermal-data fields.
        /// </summary>
        /// <param name="state">KSP's current PartModule startup state.</param>
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            lossField = Fields["loss"];
            fluxField = Fields["flux"];
            resolveResourceDefinitions();
            updateFieldVisibility();
        }

        /// <summary>
        /// Enables cooling when KerbalGear creates or activates the requested module.
        /// </summary>
        public override void OnActive()
        {
            base.OnActive();
            evaAblatorIsActive = true;
            resolveResourceDefinitions();
            updateFieldVisibility();
        }

        /// <summary>
        /// Stops cooling immediately when the last contributing inventory item is removed.
        /// No conductivity restoration is necessary because this module never changes it.
        /// </summary>
        public override void OnInactive()
        {
            base.OnInactive();
            evaAblatorIsActive = false;
            resetThermalData();
            updateFieldVisibility();
        }

        /// <summary>
        /// Updates thermal-data visibility while this dynamically created module is active.
        /// </summary>
        public override void OnUpdate()
        {
            base.OnUpdate();
            updateFieldVisibility();
        }

        /// <summary>
        /// Returns the localized title shown in the EVA kerbal's action window.
        /// </summary>
        /// <returns>The localized EVA Ablator title.</returns>
        public override string GetModuleDisplayName()
        {
            return Localizer.Format("#LOC_WILDBLUECORE_evaAblatorTitle");
        }
        #endregion

        #region KerbalGear
        /// <summary>
        /// Refreshes resource definitions after inventory changes alter the available proxies.
        /// </summary>
        /// <param name="inventory">The EVA inventory whose contents changed.</param>
        public void OnKerbalGearInventoryChanged(ModuleInventoryPart inventory)
        {
            if (!evaAblatorIsActive)
                return;

            resolveResourceDefinitions();
        }
        #endregion

        #region Unity Lifecycle
        /// <summary>
        /// Consumes coolant and applies stock-style negative exposed thermal flux to the EVA part.
        /// </summary>
        public void FixedUpdate()
        {
            resetThermalData();
            if (!HighLogic.LoadedSceneIsFlight || !evaAblatorIsActive || !moduleIsEnabled ||
                part == null || ablativeDefinition == null || lossExp >= 0.0 ||
                part.skinTemperature <= ablationTempThresh || TimeWarp.fixedDeltaTime <= 0.0f)
            {
                return;
            }

            double availableAmount;
            double maxAmount;
            part.GetConnectedResourceTotals(ablativeDefinition.id,
                ablativeDefinition.resourceFlowMode, out availableAmount, out maxAmount, true);
            if (availableAmount <= 0.0 || maxAmount <= 0.0)
                return;

            double lossRate = lossConst * Math.Exp(lossExp / part.skinTemperature) * maxAmount;
            if (lossRate <= 0.0 || double.IsNaN(lossRate) || double.IsInfinity(lossRate))
                return;

            double demand = Math.Min(availableAmount, lossRate * TimeWarp.fixedDeltaTime);
            double consumed = part.RequestResource(ablativeDefinition.id, demand,
                ablativeDefinition.resourceFlowMode);
            if (consumed <= 0.0)
                return;

            if (outputDefinition != null && outputMult > 0.0)
            {
                part.RequestResource(outputDefinition.id, -consumed * outputMult,
                    outputDefinition.resourceFlowMode);
            }

            double unitsPerSecond = consumed / TimeWarp.fixedDeltaTime;
            double massLossRate = unitsPerSecond * ablativeDefinition.density;
            flux = pyrolysisLossFactor * ablativeDefinition.specificHeatCapacity * massLossRate;
            loss = massLossRate * 1000.0;

            if (flux > 0.0 && !double.IsNaN(flux) && !double.IsInfinity(flux))
                part.AddExposedThermalFlux(-flux);
            else
                flux = 0.0;
        }

        /// <summary>
        /// Clears transient display state when Unity destroys the dynamic component.
        /// </summary>
        public void OnDestroy()
        {
            evaAblatorIsActive = false;
            resetThermalData();
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Resolves the configured coolant and optional output resource definitions.
        /// </summary>
        private void resolveResourceDefinitions()
        {
            ablativeDefinition = string.IsNullOrEmpty(ablativeResource)
                ? null
                : PartResourceLibrary.Instance.GetDefinition(ablativeResource);
            outputDefinition = string.IsNullOrEmpty(outputResource)
                ? null
                : PartResourceLibrary.Instance.GetDefinition(outputResource);

            bool configurationIsValid = ablativeDefinition != null && lossExp < 0.0 &&
                lossConst > 0.0 && pyrolysisLossFactor > 0.0;
            if (!configurationIsValid && !invalidConfigurationLogged)
            {
                Debug.LogWarning("[WBIModuleEVAAblator] Invalid cooling configuration on " +
                    (part != null ? part.partInfo.title : "EVA part") + ".");
                invalidConfigurationLogged = true;
            }
            else if (configurationIsValid)
            {
                invalidConfigurationLogged = false;
            }
        }

        /// <summary>
        /// Shows stock thermal diagnostics only while the module and thermal-data overlay are active.
        /// </summary>
        private void updateFieldVisibility()
        {
            bool showThermalData = evaAblatorIsActive && moduleIsEnabled &&
                PhysicsGlobals.ThermalDataDisplay;
            if (lossField != null)
                lossField.guiActive = showThermalData;
            if (fluxField != null)
                fluxField.guiActive = showThermalData;
        }

        /// <summary>
        /// Clears per-tick diagnostic values without modifying persistent kerbal thermal properties.
        /// </summary>
        private void resetThermalData()
        {
            loss = 0.0;
            flux = 0.0;
        }
        #endregion
    }
}
