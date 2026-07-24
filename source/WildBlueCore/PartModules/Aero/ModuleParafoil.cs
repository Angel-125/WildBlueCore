using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.UI.Screens;
using KSP.Localization;

namespace WildBlueCore.PartModules.Aero
{
    /// <summary>
    /// Acts as a controller for ModuleLiftingSurface and ModuleControlSurface
    /// As the parachute deploys, it gains lift and flight control- if the above part modules are found.
    /// </summary>
    public class ModuleParafoil: ModuleParachute
    {
        #region Fields
        /// <summary>
        /// Lift coefficient when the chute is fully retracted
        /// </summary>
        [KSPField]
        public float retractedDeflectionLiftCoeff = 0;

        /// <summary>
        /// Lift coefficient when the chute is semi-deployed (not fully open)
        /// </summary>
        [KSPField]
        public float semiDeployedDeflectionLiftCoeff = 8;

        /// <summary>
        /// Lift coefficient when the chute is deployed (fully open)
        /// </summary>
        [KSPField]
        public float deployedDeflectionLiftCoeff = 28;

        /// <summary>
        /// Control surface's lift coefficient when the chute is fully retracted
        /// </summary>
        [KSPField]
        public float retractedCtlSfcDeflectionLiftCoeff = 0;

        /// <summary>
        /// Control surface lift coefficient when the chute is semi-deployed (not fully open)
        /// </summary>
        [KSPField]
        public float semiDeployedCtlSfcDeflectionLiftCoeff = 0.25f;

        /// <summary>
        /// Control surface lift coefficient when the chute is deployed (fully open)
        /// </summary>
        [KSPField]
        public float deployedCtlSfcDeflectionLiftCoeff = 1.25f;

        /// <summary>
        /// Flag indicating whether or not the parafoil can be steered when in the semi-deployed state.
        /// </summary>
        [KSPField]
        public bool enableControlInSemiDeploy = false;
        #endregion

        #region Housekeeping
        ModuleLiftingSurface liftingSurface;
        ModuleControlSurface controlSurface;
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            // Find the lift and control surfaces
            liftingSurface = part.FindModuleImplementing<ModuleLiftingSurface>();
            controlSurface = part.FindModuleImplementing<ModuleControlSurface>();
        }

        /// <summary>
        /// After the base FixedUpdate is called, handle updates to the liftingSurface and controlSurface.
        /// </summary>
        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!HighLogic.LoadedSceneIsFlight)
                return;

            if (liftingSurface == null && controlSurface == null)
                return;

            float deploymentCurveTime = Mathf.Clamp01(Mathf.Pow(animTime, deploymentCurve));

            switch (deploymentState)
            {
                case deploymentStates.SEMIDEPLOYED:
                    SetLiftCoefficient(Mathf.Lerp(retractedDeflectionLiftCoeff, semiDeployedDeflectionLiftCoeff, deploymentCurveTime));

                    SetControlSurfaceCoefficient(enableControlInSemiDeploy ? Mathf.Lerp(retractedCtlSfcDeflectionLiftCoeff, semiDeployedCtlSfcDeflectionLiftCoeff, deploymentCurveTime) : retractedCtlSfcDeflectionLiftCoeff);
                    break;

                case deploymentStates.DEPLOYED:
                    float deployedStartControlCoeff = enableControlInSemiDeploy ? semiDeployedCtlSfcDeflectionLiftCoeff : retractedCtlSfcDeflectionLiftCoeff;

                    SetLiftCoefficient(Mathf.Lerp(semiDeployedDeflectionLiftCoeff, deployedDeflectionLiftCoeff, deploymentCurveTime));

                    SetControlSurfaceCoefficient(Mathf.Lerp(deployedStartControlCoeff, deployedCtlSfcDeflectionLiftCoeff, deploymentCurveTime));
                    break;

                case deploymentStates.STOWED:
                case deploymentStates.ACTIVE:
                case deploymentStates.CUT:
                default:
                    SetLiftCoefficient(retractedDeflectionLiftCoeff);
                    SetControlSurfaceCoefficient(retractedCtlSfcDeflectionLiftCoeff);
                    break;
            }
        }
        #endregion

        #region Helpers
        private void SetLiftCoefficient(float liftCoefficient)
        {
            if (liftingSurface != null)
                liftingSurface.deflectionLiftCoeff = liftCoefficient;
        }

        private void SetControlSurfaceCoefficient(float controlSurfaceLiftCoefficient)
        {
            if (controlSurface != null)
                controlSurface.deflectionLiftCoeff = controlSurfaceLiftCoefficient;
        }
        #endregion
    }
}
