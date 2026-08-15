using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

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
        /// Enables FixedUpdate diagnostics in KSP.log.
        /// </summary>
        [KSPField]
        public bool debugMode = false;

        /// <summary>
        /// Minimum time, in seconds, between groups of FixedUpdate diagnostic messages.
        /// Set to 0 to log every physics update.
        /// </summary>
        [KSPField]
        public float debugLogInterval = 0.25f;

        /// <summary>
        /// When enabled, ModuleParafoil only emits diagnostics and leaves all
        /// parachute, drag, rotation, lift, and control behavior to the base module.
        /// </summary>
        [KSPField]
        public bool diagnosticsOnly = false;

        /// <summary>
        /// Lift coefficient when the chute is semi-deployed (not fully open)
        /// </summary>
        [KSPField]
        public float semiDeployedDeflectionLiftCoeff = 8;

        /// <summary>
        /// Control surface lift coefficient when the chute is semi-deployed (not fully open)
        /// </summary>
        [KSPField]
        public float semiDeployedCtlSfcDeflectionLiftCoeff = 0.25f;

        /// <summary>
        /// Flag indicating whether or not the parafoil can be steered when in the semi-deployed state.
        /// </summary>
        [KSPField]
        public bool enableControlInSemiDeploy = false;

        /// <summary>
        /// Time, in seconds, to ramp the control surface and passive stabilizers
        /// from zero to their configured authority after the parafoil takes control.
        /// </summary>
        [KSPField]
        public float controlAuthorityRampTime = 1f;
        #endregion

        #region Housekeeping
        const float disabledControlSurfaceRange = 0.0001f;

        List<ModuleLiftingSurface> liftingSurfaces;
        List<float> deployedLiftCoefficients;
        List<ModuleControlSurface> controlSurfaces;
        List<float> deployedControlSurfaceLiftCoefficients;
        List<float> maximumControlSurfaceRanges;
        List<bool> originalIgnorePitch;
        List<bool> originalIgnoreRoll;
        List<bool> originalIgnoreYaw;
        List<ModuleParafoilStabilizer> stabilizerSurfaces;
        List<float> deployedStabilizerLiftCoefficients;
        float originalMaximumDrag;
        Quaternion originalCanopyLocalRotation;
        bool hasOriginalCanopyLocalRotation;
        bool parafoilFlightActive;
        double controlAuthorityRampStartTime;
        float nextDebugLogTime;
        #endregion

        #region Overrides
        /// <summary>
        /// Overrides base OnStart to provide custom functionality.
        /// </summary>
        /// <param name="state"></param>
        public override void OnStart(StartState state)
        {
            // Capture the part's unmodified values before ModuleParachute initializes its drag.
            originalMaximumDrag = part.maximum_drag;

            // ModuleControlSurface and ModuleParafoilStabilizer derive from
            // ModuleLiftingSurface, so exclude them from the primary lift collection.
            liftingSurfaces = part.FindModulesImplementing<ModuleLiftingSurface>()
                .Where(module =>
                    !(module is ModuleControlSurface) &&
                    !(module is ModuleParafoilStabilizer))
                .ToList();
            deployedLiftCoefficients = liftingSurfaces
                .Select(module => module.deflectionLiftCoeff)
                .ToList();

            controlSurfaces = part.FindModulesImplementing<ModuleControlSurface>();
            deployedControlSurfaceLiftCoefficients = controlSurfaces
                .Select(module => module.deflectionLiftCoeff)
                .ToList();
            maximumControlSurfaceRanges = controlSurfaces
                .Select(module => module.ctrlSurfaceRange)
                .ToList();
            originalIgnorePitch = controlSurfaces
                .Select(module => module.ignorePitch)
                .ToList();
            originalIgnoreRoll = controlSurfaces
                .Select(module => module.ignoreRoll)
                .ToList();
            originalIgnoreYaw = controlSurfaces
                .Select(module => module.ignoreYaw)
                .ToList();

            stabilizerSurfaces = part.FindModulesImplementing<ModuleParafoilStabilizer>();
            deployedStabilizerLiftCoefficients = stabilizerSurfaces
                .Select(module => module.deflectionLiftCoeff)
                .ToList();

            // The surface modules can generate forces as soon as physics starts,
            // regardless of whether the parachute canopy is visible. Neutralize
            // them before ModuleParachute initialization leaves OnStart.
            if (!diagnosticsOnly && HighLogic.LoadedSceneIsFlight)
                DisableAerodynamicSurfaces();

            base.OnStart(state);

            if (canopy != null)
            {
                originalCanopyLocalRotation = canopy.localRotation;
                hasOriginalCanopyLocalRotation = true;
            }

            if (!diagnosticsOnly && HighLogic.LoadedSceneIsFlight)
                DisableAerodynamicSurfaces();
        }

        /// <summary>
        /// Lets ModuleParachute control deployment, then hands fully deployed flight
        /// to the parafoil lifting and control surfaces.
        /// </summary>
        protected override void FixedUpdate()
        {
            bool logDiagnostics = ShouldLogDiagnostics();
            if (logDiagnostics)
                LogDiagnostics("BEFORE ModuleParachute");

            bool bypassModuleParachute = !diagnosticsOnly &&
                parafoilFlightActive &&
                deploymentState == deploymentStates.DEPLOYED;

            if (bypassModuleParachute)
                MaintainDeployedParachuteSafety();
            else
                base.FixedUpdate();

            if (logDiagnostics)
                LogDiagnostics(bypassModuleParachute
                    ? "AFTER ModuleParachute (BYPASSED)"
                    : "AFTER ModuleParachute");

            if (!HighLogic.LoadedSceneIsFlight)
                return;

            if (diagnosticsOnly)
            {
                if (logDiagnostics)
                    LogDiagnostics("AFTER ModuleParafoil");

                return;
            }

            if (parafoilFlightActive && deploymentState != deploymentStates.DEPLOYED)
                EndParafoilFlight();

            if (!parafoilFlightActive && IsFullyDeployed())
                BeginParafoilFlight();

            if (parafoilFlightActive)
                RestoreCanopyRotation();

            if ((liftingSurfaces != null && liftingSurfaces.Count > 0) ||
                (controlSurfaces != null && controlSurfaces.Count > 0) ||
                (stabilizerSurfaces != null && stabilizerSurfaces.Count > 0))
            {
                float deploymentCurveTime = Mathf.Clamp01(Mathf.Pow(animTime, deploymentCurve));

                switch (deploymentState)
                {
                    case deploymentStates.SEMIDEPLOYED:
                        SetLiftCoefficient(Mathf.Lerp(0f, semiDeployedDeflectionLiftCoeff, deploymentCurveTime));
                        SetControlSurfaceCoefficient(enableControlInSemiDeploy ? Mathf.Lerp(0f, semiDeployedCtlSfcDeflectionLiftCoeff, deploymentCurveTime) : 0f);
                        SetStabilizerAuthority(0f);
                        DisableControlSurface();
                        break;

                    case deploymentStates.DEPLOYED:
                        float deployedStartControlCoeff = enableControlInSemiDeploy ? semiDeployedCtlSfcDeflectionLiftCoeff : 0f;

                        SetLiftCoefficientsFromSemiDeployment(deploymentCurveTime);
                        SetControlSurfaceCoefficientsFromSemiDeployment(deployedStartControlCoeff, deploymentCurveTime);

                        if (parafoilFlightActive)
                        {
                            UpdateControlAuthority();
                            UpdateStabilizerAuthority();
                        }
                        else
                        {
                            SetStabilizerAuthority(0f);
                            DisableControlSurface();
                        }
                        break;

                    case deploymentStates.STOWED:
                    case deploymentStates.ACTIVE:
                    case deploymentStates.CUT:
                    default:
                        DisableAerodynamicSurfaces();
                        break;
                }
            }

            if (logDiagnostics)
                LogDiagnostics("AFTER ModuleParafoil");
        }
        #endregion

        #region Helpers
        private bool ShouldLogDiagnostics()
        {
            if (!debugMode || !HighLogic.LoadedSceneIsFlight)
                return false;

            float currentTime = Time.realtimeSinceStartup;
            if (debugLogInterval > 0f && currentTime < nextDebugLogTime)
                return false;

            nextDebugLogTime = currentTime + Mathf.Max(0f, debugLogInterval);
            return true;
        }

        private void LogDiagnostics(string phase)
        {
            StringBuilder message = new StringBuilder(768);
            message.Append("[ModuleParafoil] - frame=").Append(Time.frameCount);
            message.Append(" phase=").Append(phase);
            message.Append(" part=").Append(part != null ? part.name : "<null>");
            message.Append(" state=").Append(deploymentState);
            message.Append(" animTime=").Append(animTime.ToString("F4"));
            message.Append(" rotationSpeedDPS=").Append(rotationSpeedDPS.ToString("F3"));
            message.Append(" fullyDeployedDrag=").Append(fullyDeployedDrag.ToString("F3"));
            message.Append(" maximum_drag=").Append(part != null ? part.maximum_drag.ToString("F3") : "<null>");
            message.Append(" parafoilFlightActive=").Append(parafoilFlightActive);
            message.Append(" diagnosticsOnly=").Append(diagnosticsOnly);

            if (canopy != null)
            {
                message.Append(" canopy.worldEuler=").Append(FormatVector(canopy.rotation.eulerAngles));
                message.Append(" canopy.localEuler=").Append(FormatVector(canopy.localRotation.eulerAngles));
                message.Append(" canopy.worldQuat=").Append(FormatQuaternion(canopy.rotation));
                message.Append(" canopy.localQuat=").Append(FormatQuaternion(canopy.localRotation));
                message.Append(" canopy.forward=").Append(FormatVector(canopy.forward));
                message.Append(" canopy.up=").Append(FormatVector(canopy.up));
                message.Append(" canopy.originalLocalQuat=")
                    .Append(hasOriginalCanopyLocalRotation
                        ? FormatQuaternion(originalCanopyLocalRotation)
                        : "<not-captured>");
                message.Append(" canopy.rotationErrorDeg=")
                    .Append(hasOriginalCanopyLocalRotation
                        ? Quaternion.Angle(canopy.localRotation, originalCanopyLocalRotation).ToString("F4")
                        : "<not-captured>");
            }
            else
            {
                message.Append(" canopy=<null>");
            }

            if (part != null)
            {
                message.Append(" part.worldEuler=").Append(FormatVector(part.transform.rotation.eulerAngles));
                message.Append(" dragVectorDir=").Append(FormatVector(part.dragVectorDir));

                if (part.Rigidbody != null)
                {
                    message.Append(" rigidbody.velocity=").Append(FormatVector(part.Rigidbody.velocity));
                    message.Append(" rigidbody.angularVelocity=").Append(FormatVector(part.Rigidbody.angularVelocity));
                }
            }

            if (vessel != null)
                message.Append(" vessel.srfVelocity=").Append(FormatVector(vessel.srf_velocity));

            int liftingSurfaceCount = liftingSurfaces != null ? liftingSurfaces.Count : 0;
            message.Append(" liftingSurface.count=").Append(liftingSurfaceCount);
            for (int index = 0; index < liftingSurfaceCount; index++)
            {
                ModuleLiftingSurface liftingSurface = liftingSurfaces[index];
                message.Append(" liftingSurface[").Append(index).Append("].transformName=")
                    .Append(string.IsNullOrEmpty(liftingSurface.transformName) ? "<part-root>" : liftingSurface.transformName);
                message.Append(" liftingSurface[").Append(index).Append("].targetCoeff=")
                    .Append(deployedLiftCoefficients[index].ToString("F4"));
                message.Append(" liftingSurface[").Append(index).Append("].coeff=")
                    .Append(liftingSurface.deflectionLiftCoeff.ToString("F4"));
                message.Append(" liftingSurface[").Append(index).Append("].liftScalar=")
                    .Append(liftingSurface.liftScalar.ToString("F4"));
                message.Append(" liftingSurface[").Append(index).Append("].dragScalar=")
                    .Append(liftingSurface.dragScalar.ToString("F4"));
            }

            int controlSurfaceCount = controlSurfaces != null ? controlSurfaces.Count : 0;
            message.Append(" controlSurface.count=").Append(controlSurfaceCount);
            for (int index = 0; index < controlSurfaceCount; index++)
            {
                ModuleControlSurface controlSurface = controlSurfaces[index];
                message.Append(" controlSurface[").Append(index).Append("].transformName=")
                    .Append(string.IsNullOrEmpty(controlSurface.transformName) ? "<part-root>" : controlSurface.transformName);
                message.Append(" controlSurface[").Append(index).Append("].targetCoeff=")
                    .Append(deployedControlSurfaceLiftCoefficients[index].ToString("F4"));
                message.Append(" controlSurface[").Append(index).Append("].coeff=")
                    .Append(controlSurface.deflectionLiftCoeff.ToString("F4"));
                message.Append(" controlSurface[").Append(index).Append("].liftScalar=")
                    .Append(controlSurface.liftScalar.ToString("F4"));
                message.Append(" controlSurface[").Append(index).Append("].dragScalar=")
                    .Append(controlSurface.dragScalar.ToString("F4"));
                message.Append(" controlSurface[").Append(index).Append("].range=")
                    .Append(controlSurface.ctrlSurfaceRange.ToString("F4"));
                message.Append(" controlSurface[").Append(index).Append("].area=")
                    .Append(controlSurface.ctrlSurfaceArea.ToString("F4"));
                message.Append(" controlSurface[").Append(index).Append("].ignorePitch=")
                    .Append(controlSurface.ignorePitch);
                message.Append(" controlSurface[").Append(index).Append("].ignoreRoll=")
                    .Append(controlSurface.ignoreRoll);
                message.Append(" controlSurface[").Append(index).Append("].ignoreYaw=")
                    .Append(controlSurface.ignoreYaw);
            }

            int stabilizerCount = stabilizerSurfaces != null ? stabilizerSurfaces.Count : 0;
            message.Append(" stabilizer.count=").Append(stabilizerCount);
            for (int index = 0; index < stabilizerCount; index++)
            {
                ModuleParafoilStabilizer stabilizer = stabilizerSurfaces[index];
                message.Append(" stabilizer[").Append(index).Append("].transformName=")
                    .Append(string.IsNullOrEmpty(stabilizer.transformName) ? "<part-root>" : stabilizer.transformName);
                message.Append(" stabilizer[").Append(index).Append("].targetCoeff=")
                    .Append(deployedStabilizerLiftCoefficients[index].ToString("F4"));
                message.Append(" stabilizer[").Append(index).Append("].coeff=")
                    .Append(stabilizer.deflectionLiftCoeff.ToString("F4"));
                message.Append(" stabilizer[").Append(index).Append("].liftScalar=")
                    .Append(stabilizer.liftScalar.ToString("F4"));
                message.Append(" stabilizer[").Append(index).Append("].dragScalar=")
                    .Append(stabilizer.dragScalar.ToString("F4"));
            }

            Debug.Log(message.ToString());
        }

        private string FormatVector(Vector3 value)
        {
            return string.Format("({0:F4},{1:F4},{2:F4})", value.x, value.y, value.z);
        }

        private string FormatVector(Vector3d value)
        {
            return string.Format("({0:F4},{1:F4},{2:F4})", value.x, value.y, value.z);
        }

        private string FormatQuaternion(Quaternion value)
        {
            return string.Format("({0:F5},{1:F5},{2:F5},{3:F5})", value.x, value.y, value.z, value.w);
        }

        private bool IsFullyDeployed()
        {
            return deploymentState == deploymentStates.DEPLOYED && animTime >= 0.999f;
        }

        private void BeginParafoilFlight()
        {
            parafoilFlightActive = true;
            controlAuthorityRampStartTime = Planetarium.GetUniversalTime();

            // ModuleLiftingSurface, ModuleParafoilStabilizer, and ModuleControlSurface replace the deployed
            // parachute aerodynamics after the deployment animation completes.
            part.maximum_drag = originalMaximumDrag;
            part.DragCubes.SetCubeWeight("PACKED", 0f);
            part.DragCubes.SetCubeWeight("SEMIDEPLOYED", 0f);
            part.DragCubes.SetCubeWeight("DEPLOYED", 0f);
            part.DragCubes.SetOcclusionMultiplier(1f);

            RestoreCanopyRotation();
            DisableControlSurface();
        }

        private void EndParafoilFlight()
        {
            parafoilFlightActive = false;
            DisableAerodynamicSurfaces();
        }

        private void MaintainDeployedParachuteSafety()
        {
            if (part == null || part.packed)
                return;

            // A splashed vessel may continue sinking or drifting faster than
            // autoCutSpeed indefinitely. Cut immediately on splashdown rather
            // than waiting for the stock landing speed check to succeed.
            if (vessel != null && vessel.situation == Vessel.Situations.SPLASHED)
            {
                CutParachute();
                return;
            }

            if (vessel != null)
            {
                SetConvectiveStats(
                    vessel.atmDensity,
                    vessel.externalTemperature,
                    vessel.mach,
                    vessel.convectiveCoefficient);
            }
            else
            {
                SetConvectiveStats(0.0, 4.0, 0.0, 0.0);
            }

            // Preserve stock landing/splash auto-cut, vacuum cut, and canopy
            // thermal failure while bypassing ModuleParachute's rotation and velocity updates.
            UpdateCut();
        }

        private void DisableControlSurface()
        {
            if (controlSurfaces == null)
                return;

            for (int index = 0; index < controlSurfaces.Count; index++)
            {
                ModuleControlSurface controlSurface = controlSurfaces[index];
                controlSurface.ignorePitch = true;
                controlSurface.ignoreRoll = true;
                controlSurface.ignoreYaw = true;
                controlSurface.ctrlSurfaceRange = disabledControlSurfaceRange;
            }
        }

        private void DisableAerodynamicSurfaces()
        {
            SetLiftCoefficient(0f);
            SetControlSurfaceCoefficient(0f);
            SetStabilizerAuthority(0f);
            DisableControlSurface();
        }

        private void RestoreCanopyRotation()
        {
            if (canopy == null || !hasOriginalCanopyLocalRotation)
                return;

            canopy.localRotation = originalCanopyLocalRotation;
        }

        private void UpdateControlAuthority()
        {
            if (controlSurfaces == null ||
                maximumControlSurfaceRanges == null ||
                originalIgnorePitch == null ||
                originalIgnoreRoll == null ||
                originalIgnoreYaw == null)
                return;

            float rampProgress = controlAuthorityRampTime <= 0f
                ? 1f
                : Mathf.Clamp01((float)((Planetarium.GetUniversalTime() - controlAuthorityRampStartTime) / controlAuthorityRampTime));

            int controlSurfaceCount = Math.Min(
                controlSurfaces.Count,
                Math.Min(
                    maximumControlSurfaceRanges.Count,
                    Math.Min(
                        originalIgnorePitch.Count,
                        Math.Min(originalIgnoreRoll.Count, originalIgnoreYaw.Count))));

            for (int index = 0; index < controlSurfaceCount; index++)
            {
                ModuleControlSurface controlSurface = controlSurfaces[index];
                controlSurface.ignorePitch = originalIgnorePitch[index];
                controlSurface.ignoreRoll = originalIgnoreRoll[index];
                controlSurface.ignoreYaw = originalIgnoreYaw[index];
                controlSurface.ctrlSurfaceRange = Mathf.Lerp(
                    disabledControlSurfaceRange,
                    maximumControlSurfaceRanges[index],
                    rampProgress);
            }
        }

        private void UpdateStabilizerAuthority()
        {
            float rampProgress = controlAuthorityRampTime <= 0f
                ? 1f
                : Mathf.Clamp01((float)((Planetarium.GetUniversalTime() - controlAuthorityRampStartTime) / controlAuthorityRampTime));

            SetStabilizerAuthority(rampProgress);
        }

        private void SetStabilizerAuthority(float authority)
        {
            if (stabilizerSurfaces == null || deployedStabilizerLiftCoefficients == null)
                return;

            float clampedAuthority = Mathf.Clamp01(authority);
            int stabilizerCount = Mathf.Min(stabilizerSurfaces.Count, deployedStabilizerLiftCoefficients.Count);
            for (int index = 0; index < stabilizerCount; index++)
                stabilizerSurfaces[index].deflectionLiftCoeff =
                    deployedStabilizerLiftCoefficients[index] * clampedAuthority;
        }

        private void SetLiftCoefficient(float liftCoefficient)
        {
            if (liftingSurfaces == null)
                return;

            for (int index = 0; index < liftingSurfaces.Count; index++)
                liftingSurfaces[index].deflectionLiftCoeff = liftCoefficient;
        }

        private void SetControlSurfaceCoefficient(float controlSurfaceLiftCoefficient)
        {
            if (controlSurfaces == null)
                return;

            for (int index = 0; index < controlSurfaces.Count; index++)
                controlSurfaces[index].deflectionLiftCoeff = controlSurfaceLiftCoefficient;
        }

        private void SetLiftCoefficientsFromSemiDeployment(float deploymentCurveTime)
        {
            if (liftingSurfaces == null || deployedLiftCoefficients == null)
                return;

            int liftingSurfaceCount = Math.Min(liftingSurfaces.Count, deployedLiftCoefficients.Count);
            for (int index = 0; index < liftingSurfaceCount; index++)
            {
                liftingSurfaces[index].deflectionLiftCoeff = Mathf.Lerp(
                    semiDeployedDeflectionLiftCoeff,
                    deployedLiftCoefficients[index],
                    deploymentCurveTime);
            }
        }

        private void SetControlSurfaceCoefficientsFromSemiDeployment(float deployedStartControlCoeff, float deploymentCurveTime)
        {
            if (controlSurfaces == null || deployedControlSurfaceLiftCoefficients == null)
                return;

            int controlSurfaceCount = Math.Min(controlSurfaces.Count, deployedControlSurfaceLiftCoefficients.Count);
            for (int index = 0; index < controlSurfaceCount; index++)
            {
                controlSurfaces[index].deflectionLiftCoeff = Mathf.Lerp(
                    deployedStartControlCoeff,
                    deployedControlSurfaceLiftCoefficients[index],
                    deploymentCurveTime);
            }
        }
        #endregion
    }
}
