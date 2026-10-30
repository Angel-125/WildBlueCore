using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        /// <summary>Enable torque-limited glide stabilization and coordinated steering after full deployment.</summary>
        [KSPField] public bool enableAttitudeController = false;
        /// <summary>Allow assistance alongside SAS; false avoids competing attitude controllers.</summary>
        [KSPField] public bool attitudeControllerWithSAS = false;
        /// <summary>Use the vessel control reference rather than the chute mounting transform.</summary>
        [KSPField] public bool attitudeUseVesselReference = true;
        /// <summary>Local forward direction in the selected attitude reference.</summary>
        [KSPField] public Vector3 attitudeForwardAxis = Vector3.up;
        /// <summary>Local dorsal/up direction in the selected attitude reference (KSP uses -Z).</summary>
        [KSPField] public Vector3 attitudeUpAxis = -Vector3.forward;
        /// <summary>Neutral nose elevation above the horizon, in degrees.</summary>
        [KSPField] public float attitudeGlidePitch = -5f;
        /// <summary>Maximum additional nose elevation commanded by pitch input, in degrees.</summary>
        [KSPField] public float attitudePitchRange = 15f;
        /// <summary>Maximum bank commanded by yaw/roll input, in degrees.</summary>
        [KSPField] public float attitudeMaxBank = 35f;
        /// <summary>Upper bound on coordinated heading rate, in degrees per second.</summary>
        [KSPField] public float attitudeMaxTurnRate = 12f;
        /// <summary>Upper bound on attitude-correction rate, in degrees per second.</summary>
        [KSPField] public float attitudeMaxCorrectionRate = 35f;
        /// <summary>Attitude error to desired rotation-rate gain, per second.</summary>
        [KSPField] public float attitudeResponse = 2f;
        /// <summary>Rotation-rate error to angular acceleration gain, per second.</summary>
        [KSPField] public float attitudeDamping = 4f;
        /// <summary>Integrate angular-rate error to estimate persistent aerodynamic moments.</summary>
        [KSPField] public float attitudeIntegralGain = 2f;
        /// <summary>Maximum integrated acceleration contribution, in radians per second squared.</summary>
        [KSPField] public float attitudeIntegralLimit = 20f;
        /// <summary>Total assistance torque limit in KSP torque units (kN m).</summary>
        [KSPField] public float attitudeMaxTorque = 100f;
        /// <summary>Seconds to ramp assistance after handoff or resuming from SAS.</summary>
        [KSPField] public float attitudeRampTime = 2f;
        /// <summary>Seconds to introduce primary lift once assistance has settled the vessel.</summary>
        [KSPField] public float attitudeLiftRampTime = 4f;
        /// <summary>Minimum deployed lift fraction during recovery, introduced by the normal lift ramp.</summary>
        [KSPField] public float attitudeRecoveryLift = 0.05f;
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
        // Stock exposes no public deflection accessor. Cache a read-only lookup
        // rather than changing the stock module or its visibility for diagnostics.
        static readonly FieldInfo controlDeflectionField = typeof(ModuleControlSurface).GetField(
            "deflection", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Vessel inputObservationVessel;
        Vector3 preAutopilotInput;
        float preAutopilotInputTime;
        bool hasPreAutopilotInput;
        bool attitudeInitialized;
        double attitudeStartTime;
        Vector3 attitudeHeading;
        Vector3 attitudeIntegral;
        Vector3 attitudeTorque;
        Vector3 attitudeError;
        Quaternion attitudeTargetRotation = Quaternion.identity;
        float attitudeTargetPitch;
        float attitudeTargetBank;
        float attitudeTargetTurnRate;
        string attitudeStatus = "inactive";
        float attitudeLiftAuthority;
        Vector3 attitudeWorldAngularVelocity;
        string attitudeTorquePart = "<none>";
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
            attitudeTorque = Vector3.zero;
            UpdateInputObservation();
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
                ResetAttitudeController("diagnostics-only");
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
                        SetLiftCoefficient(enableAttitudeController ? 0f : Mathf.Lerp(0f, semiDeployedDeflectionLiftCoeff, deploymentCurveTime));
                        SetControlSurfaceCoefficient(!enableAttitudeController && enableControlInSemiDeploy ? Mathf.Lerp(0f, semiDeployedCtlSfcDeflectionLiftCoeff, deploymentCurveTime) : 0f);
                        SetStabilizerAuthority(0f);
                        DisableControlSurface();
                        break;

                    case deploymentStates.DEPLOYED:
                        float deployedStartControlCoeff = enableControlInSemiDeploy ? semiDeployedCtlSfcDeflectionLiftCoeff : 0f;

                        if (enableAttitudeController)
                        {
                            // Do not introduce wing forces while stock still owns
                            // inflation. Assistance must settle the vessel first.
                            SetPrimaryLiftAuthority(parafoilFlightActive ? attitudeLiftAuthority : 0f);
                            SetControlSurfaceCoefficient(0f);
                        }
                        else
                        {
                            SetLiftCoefficientsFromSemiDeployment(deploymentCurveTime);
                            SetControlSurfaceCoefficientsFromSemiDeployment(deployedStartControlCoeff, deploymentCurveTime);
                        }

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

            UpdateAttitudeController();
            if (enableAttitudeController && parafoilFlightActive)
                SetPrimaryLiftAuthority(attitudeLiftAuthority);

            if (logDiagnostics)
                LogDiagnostics("AFTER ModuleParafoil");
        }
        #endregion

        #region Helpers
        /// <summary>Remove the observational callback before stock parachute cleanup.</summary>
        public override void OnDestroy()
        {
            if (inputObservationVessel != null)
                inputObservationVessel.OnPreAutopilotUpdate -= CapturePreAutopilotInput;
            base.OnDestroy();
        }

        private void UpdateInputObservation()
        {
            Vessel target = (debugMode || enableAttitudeController) && HighLogic.LoadedSceneIsFlight ? vessel : null;
            if (inputObservationVessel == target)
                return;
            if (inputObservationVessel != null)
                inputObservationVessel.OnPreAutopilotUpdate -= CapturePreAutopilotInput;
            inputObservationVessel = target;
            hasPreAutopilotInput = false;
            if (inputObservationVessel != null)
                inputObservationVessel.OnPreAutopilotUpdate += CapturePreAutopilotInput;
        }

        private void CapturePreAutopilotInput(FlightCtrlState state)
        {
            // Observe only: never modify the shared FlightCtrlState.
            preAutopilotInput = new Vector3(state.pitch, state.roll, state.yaw);
            preAutopilotInputTime = Time.fixedTime;
            hasPreAutopilotInput = true;
        }

        private void ResetAttitudeController(string status)
        {
            attitudeInitialized = false;
            attitudeIntegral = attitudeTorque = attitudeError = Vector3.zero;
            attitudeTargetPitch = attitudeTargetBank = attitudeTargetTurnRate = 0f;
            attitudeStatus = status;
            attitudeTorquePart = "<none>";
        }

        private void UpdateAttitudeController()
        {
            if (!enableAttitudeController || !parafoilFlightActive || deploymentState != deploymentStates.DEPLOYED ||
                vessel == null || part.Rigidbody == null || part.packed || vessel.LandedOrSplashed ||
                part.ShieldedFromAirstream || part.dynamicPressurekPa < 0.01)
            {
                ResetAttitudeController("inactive");
                attitudeLiftAuthority = 0f;
                return;
            }
            if (!attitudeControllerWithSAS && vessel.ActionGroups[KSPActionGroup.SAS])
            {
                ResetAttitudeController("SAS-active");
                attitudeLiftAuthority = Mathf.MoveTowards(attitudeLiftAuthority, 1f,
                    attitudeLiftRampTime <= 0f ? 1f : TimeWarp.fixedDeltaTime / attitudeLiftRampTime);
                return;
            }
            if (!IsAttitudeControllerLeader())
            {
                ResetAttitudeController("another-parafoil-controls");
                return;
            }
            Transform reference = vessel.ReferenceTransform;
            if (reference == null)
            {
                ResetAttitudeController("missing-reference");
                return;
            }
            Quaternion controlledRotation = attitudeUseVesselReference ? reference.rotation : part.partTransform.rotation;
            // KSP stores vessel.angularVelocity in its control-reference frame.
            // Do not feed a potentially flexing chute rigidbody's local motion
            // into a controller sized from the inertia of the entire vessel.
            attitudeWorldAngularVelocity = reference.rotation * vessel.angularVelocity;
            Vector3 up = ((Vector3)vessel.upAxis).normalized;
            Vector3 velocity = Vector3.ProjectOnPlane((Vector3)vessel.srf_velocity, up);
            float dt = TimeWarp.fixedDeltaTime;
            if (dt <= 0f || up.sqrMagnitude < 0.5f || !ParafoilAttitudeMath.IsFinite(velocity))
            {
                ResetAttitudeController("invalid-flight-frame");
                return;
            }
            if (!attitudeInitialized)
            {
                attitudeHeading = velocity.sqrMagnitude > 1f ? velocity.normalized :
                    Vector3.ProjectOnPlane(controlledRotation * attitudeForwardAxis, up).normalized;
                if (attitudeHeading.sqrMagnitude < 0.5f)
                    attitudeHeading = ((Vector3)vessel.north).normalized;
                attitudeStartTime = Planetarium.GetUniversalTime();
                attitudeInitialized = true;
            }
            // Use observed pre-autopilot controls, never SAS's correction commands.
            // Drop stale input rather than retaining a key command indefinitely.
            Vector3 input = hasPreAutopilotInput && Time.fixedTime - preAutopilotInputTime <= Mathf.Max(0.1f, 2f * dt)
                ? preAutopilotInput : Vector3.zero;
            float turn = Mathf.Clamp(input.z + input.y, -1f, 1f);
            attitudeTargetBank = turn * Mathf.Clamp(attitudeMaxBank, 0f, 60f);
            float speed = Mathf.Max(15f, velocity.magnitude);
            float gravity = ((Vector3)vessel.graviticAcceleration).magnitude;
            attitudeTargetTurnRate = Mathf.Clamp(gravity * Mathf.Tan(attitudeTargetBank * Mathf.Deg2Rad) / speed,
                -Mathf.Max(0f, attitudeMaxTurnRate) * Mathf.Deg2Rad, Mathf.Max(0f, attitudeMaxTurnRate) * Mathf.Deg2Rad);
            // If the heading-rate cap engages at low speed, reduce bank to match it.
            attitudeTargetBank = gravity > 0.0001f
                ? Mathf.Atan(speed * attitudeTargetTurnRate / gravity) * Mathf.Rad2Deg : 0f;
            float ramp = attitudeRampTime <= 0f ? 1f : Mathf.Clamp01((float)
                ((Planetarium.GetUniversalTime() - attitudeStartTime) / attitudeRampTime));
            attitudeHeading = Quaternion.AngleAxis(attitudeTargetTurnRate * Mathf.Rad2Deg * dt * ramp, up) *
                Vector3.ProjectOnPlane(attitudeHeading, up).normalized;
            attitudeTargetPitch = Mathf.Clamp(attitudeGlidePitch + Mathf.Clamp(input.x, -1f, 1f) * attitudePitchRange, -45f, 45f);
            Quaternion target;
            if (!ParafoilAttitudeMath.TryTargetRotation(attitudeHeading, up, attitudeForwardAxis,
                attitudeUpAxis, attitudeTargetPitch, attitudeTargetBank * ramp, out target))
            {
                ResetAttitudeController("invalid-configured-axes");
                return;
            }
            attitudeError = ParafoilAttitudeMath.RotationError(controlledRotation, target);
            attitudeTargetRotation = target;
            Vector3 untrimmedTorque = ParafoilAttitudeMath.CalculateTorque(attitudeError, attitudeWorldAngularVelocity,
                up * attitudeTargetTurnRate * ramp, reference.rotation, vessel.MOI, attitudeResponse, attitudeDamping,
                Mathf.Max(0f, attitudeMaxCorrectionRate) * Mathf.Deg2Rad, Vector3.zero, float.MaxValue);
            // Trim the inner rate loop, not the large outer attitude error. The
            // latter keeps accumulating while rotation is intentionally limited
            // and caused a limit cycle in the disturbance regression test.
            Vector3 rateError = ParafoilAttitudeMath.DesiredRate(attitudeError, up * attitudeTargetTurnRate * ramp,
                attitudeResponse, Mathf.Max(0f, attitudeMaxCorrectionRate) * Mathf.Deg2Rad) - attitudeWorldAngularVelocity;
            attitudeIntegral = ParafoilAttitudeMath.IntegrateTrim(attitudeIntegral, rateError, dt * ramp,
                attitudeIntegralGain, attitudeIntegralLimit, untrimmedTorque, reference.rotation, vessel.MOI, attitudeMaxTorque);
            Vector3 torque = ParafoilAttitudeMath.CalculateTorque(attitudeError, attitudeWorldAngularVelocity,
                up * attitudeTargetTurnRate * ramp, reference.rotation, vessel.MOI, attitudeResponse, attitudeDamping,
                Mathf.Max(0f, attitudeMaxCorrectionRate) * Mathf.Deg2Rad, attitudeIntegral, Mathf.Max(0f, attitudeMaxTorque));
            attitudeTorque = torque * ramp;
            Part torquePart = vessel.GetReferenceTransformPart();
            if (torquePart == null || torquePart.Rigidbody == null)
                torquePart = vessel.rootPart;
            if (torquePart == null || torquePart.Rigidbody == null)
            {
                ResetAttitudeController("missing-torque-body");
                attitudeLiftAuthority = 0f;
                return;
            }
            torquePart.AddTorque((Vector3d)attitudeTorque);
            attitudeTorquePart = torquePart.name;
            attitudeStatus = "controlling";
            attitudeLiftAuthority = ParafoilAttitudeMath.UpdateLiftAuthority(attitudeLiftAuthority,
                attitudeError.magnitude * Mathf.Rad2Deg, attitudeWorldAngularVelocity.magnitude * Mathf.Rad2Deg,
                dt, attitudeLiftRampTime, attitudeRecoveryLift);
        }

        private void SetPrimaryLiftAuthority(float authority)
        {
            if (liftingSurfaces == null || deployedLiftCoefficients == null)
                return;
            for (int i = 0; i < liftingSurfaces.Count; i++)
                liftingSurfaces[i].deflectionLiftCoeff = deployedLiftCoefficients[i] * Mathf.Clamp01(authority);
        }

        private bool IsAttitudeControllerLeader()
        {
            // Only one configured parafoil may assist a vessel at a time.
            foreach (Part vesselPart in vessel.parts)
            {
                foreach (ModuleParafoil candidate in vesselPart.FindModulesImplementing<ModuleParafoil>())
                {
                    if (!candidate.enabled || !candidate.enableAttitudeController || candidate.diagnosticsOnly || !candidate.parafoilFlightActive)
                        continue;
                    return candidate == this;
                }
            }
            return true;
        }

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
            message.Append(" fixedTime=").Append(Time.fixedTime.ToString("F4"));
            message.Append(" attitude.status=").Append(attitudeStatus);
            message.Append(" attitude.targetPitchDeg=").Append(attitudeTargetPitch.ToString("F3"));
            message.Append(" attitude.targetBankDeg=").Append(attitudeTargetBank.ToString("F3"));
            message.Append(" attitude.targetTurnRateDPS=").Append((attitudeTargetTurnRate * Mathf.Rad2Deg).ToString("F3"));
            message.Append(" attitude.errorDeg=").Append((attitudeError.magnitude * Mathf.Rad2Deg).ToString("F3"));
            message.Append(" attitude.errorWorldRad=").Append(FormatVector(attitudeError));
            message.Append(" attitude.targetWorldQuat=").Append(FormatQuaternion(attitudeTargetRotation));
            message.Append(" attitude.measuredWorldQuat=").Append(vessel != null && vessel.ReferenceTransform != null
                ? FormatQuaternion(attitudeUseVesselReference ? vessel.ReferenceTransform.rotation : part.partTransform.rotation) : "<none>");
            message.Append(" attitude.torque=").Append(FormatVector(attitudeTorque));
            message.Append(" attitude.headingWorld=").Append(FormatVector(attitudeHeading));
            message.Append(" attitude.integralAccel=").Append(FormatVector(attitudeIntegral));
            message.Append(" attitude.usesVesselReference=").Append(attitudeUseVesselReference);
            message.Append(" attitude.liftAuthority=").Append(attitudeLiftAuthority.ToString("F4"));
            message.Append(" attitude.worldAngularVelocity=").Append(FormatVector(attitudeWorldAngularVelocity));
            message.Append(" attitude.vesselMOI=").Append(vessel != null ? FormatVector(vessel.MOI) : "<none>");
            message.Append(" attitude.torquePart=").Append(attitudeTorquePart);
            message.Append(" attitude.torqueLimit=").Append(attitudeMaxTorque.ToString("F3"));
            message.Append(" attitude.integralLimit=").Append(attitudeIntegralLimit.ToString("F3"));
            message.Append(" attitude.recoveryLift=").Append(attitudeRecoveryLift.ToString("F4"));

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
            {
                message.Append(" vessel.srfVelocity=").Append(FormatVector(vessel.srf_velocity));
                AppendFlightDiagnostics(message);
            }

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
                message.Append(" controlSurface[").Append(index).Append("].deflectionDeg=")
                    .Append(controlDeflectionField != null
                        ? ((float)controlDeflectionField.GetValue(controlSurface)).ToString("F4")
                        : "<unavailable>");
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

        private void AppendFlightDiagnostics(StringBuilder message)
        {
            Vector3 up = ((Vector3)vessel.upAxis).normalized;
            Vector3 velocity = (Vector3)vessel.srf_velocity;
            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(velocity, up);
            message.Append(" vessel.verticalSpeedMps=").Append(vessel.verticalSpeed.ToString("F4"));
            message.Append(" vessel.descentSpeedMps=").Append(Math.Max(0.0, -vessel.verticalSpeed).ToString("F4"));
            message.Append(" vessel.horizontalSpeedMps=").Append(horizontalVelocity.magnitude.ToString("F4"));
            message.Append(" vessel.courseDeg=").Append(horizontalVelocity.sqrMagnitude > 0.01f
                ? FormatCompassHeading(horizontalVelocity) : "<undefined>");
            message.Append(" vessel.SAS=").Append(vessel.ActionGroups[KSPActionGroup.SAS]);
            message.Append(" control.preAutopilotPitchRollYaw=")
                .Append(hasPreAutopilotInput ? FormatVector(preAutopilotInput) : "<unavailable>");
            message.Append(" control.preAutopilotFixedTime=")
                .Append(hasPreAutopilotInput ? preAutopilotInputTime.ToString("F4") : "<unavailable>");
            if (vessel.ctrlState != null)
            {
                message.Append(" control.effectivePitchRollYaw=")
                    .Append(FormatVector(new Vector3(vessel.ctrlState.pitch, vessel.ctrlState.roll, vessel.ctrlState.yaw)));
                message.Append(" control.trimPitchRollYaw=")
                    .Append(FormatVector(new Vector3(vessel.ctrlState.pitchTrim, vessel.ctrlState.rollTrim, vessel.ctrlState.yawTrim)));
            }

            Transform reference = vessel.ReferenceTransform;
            if (reference == null)
                return;
            // KSP's reference +Y is the nose; +X is the right axis. At level
            // flight, nose cross local vertical defines the unbanked right axis.
            Vector3 nose = reference.up;
            Vector3 horizontalNose = Vector3.ProjectOnPlane(nose, up);
            Vector3 levelRight = Vector3.Cross(nose, up);
            message.Append(" vessel.referenceHeadingDeg=").Append(horizontalNose.sqrMagnitude > 0.000001f
                ? FormatCompassHeading(horizontalNose) : "<undefined>");
            message.Append(" vessel.referenceBankDeg=").Append(levelRight.sqrMagnitude > 0.000001f
                ? Vector3.SignedAngle(levelRight, reference.right, nose).ToString("F4") : "<undefined>");
        }

        private string FormatCompassHeading(Vector3 direction)
        {
            float heading = Mathf.Atan2(Vector3.Dot(direction, (Vector3)vessel.east),
                Vector3.Dot(direction, (Vector3)vessel.north)) * Mathf.Rad2Deg;
            return Mathf.Repeat(heading, 360f).ToString("F4");
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
            attitudeLiftAuthority = 0f;
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
            attitudeLiftAuthority = 0f;
            ResetAttitudeController("inactive");
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
