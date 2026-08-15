using System;
using System.Collections.Generic;
using System.Linq;
using KSP.Localization;
using UnityEngine;

namespace WildBlueCore.KerbalGear
{
    /// <summary>
    /// Provides a stock-compatible electric engine for a KerbalGear wearable.
    /// The environmental propellant is virtual: its resource definition supplies the density
    /// used by ModuleEngines, while ElectricCharge is the only resource actually consumed.
    /// </summary>
    public class WBIModuleEVAMotor : ModuleEngines
    {
        #region Constants
        const string kGeneratedThrustTransformPrefix = "wbiEVAThrustTransform";
        const string kPAWGroupName = "EVAMotor";
        const string kPAWGroupDisplayName = "#LOC_WILDBLUECORE_evaMotor";
        const float kStoppedThreshold = 0.002f;
        const float kThrottleChangeRate = 0.5f;
        #endregion

        #region Environment fields
        /// <summary>
        /// Resource definition used as the working fluid while in an atmosphere. An empty value
        /// disables atmospheric operation.
        /// </summary>
        [KSPField]
        public string atmosphericResourceName = "IntakeAir";

        /// <summary>
        /// Resource definition used as the working fluid while underwater. An empty value disables
        /// aquatic operation.
        /// </summary>
        [KSPField]
        public string aquaticResourceName = "IntakeLqd";

        /// <summary>
        /// Resource consumed to power the motor.
        /// </summary>
        [KSPField]
        public string propellantResourceName = "ElectricCharge";

        /// <summary>
        /// Resource amount consumed per second at full throttle.
        /// </summary>
        [KSPField]
        public double propellantResourceRate = 0.1666667;

        /// <summary>
        /// Multiplies atmospheric mass flow and thrust based on static pressure in atmospheres.
        /// Aquatic operation is not affected by this curve. If no keys are configured, atmospheric
        /// flow remains at full strength for backward compatibility.
        /// </summary>
        [KSPField]
        public FloatCurve atmosphericFlowCurve = new FloatCurve();

        /// <summary>
        /// Displays the currently selected propulsion environment.
        /// </summary>
        [KSPField(guiActive = true, guiName = "#LOC_WILDBLUECORE_evaMotorEnvironment")]
        public string environmentDisplay = string.Empty;

        /// <summary>
        /// Throttle commanded for the EVA vessel. Stock FlightInputHandler does not process gradual
        /// throttle input for EVA vessels, so the motor owns and supplies this value.
        /// </summary>
        [KSPField(isPersistant = true)]
        public float evaThrottle;
        #endregion

        #region Thrust and reverse-thrust fields
        /// <summary>
        /// Model transform whose orientation defines forward thrust. Its position is ignored because
        /// the generated stock-engine thrust transform is always placed at the vessel center of mass.
        /// </summary>
        [KSPField]
        public string forwardThrustTransformName = "thrustTransform";

        /// <summary>
        /// Optional model transform whose orientation defines reverse thrust. If it is absent, the
        /// forward transform is rotated 180 degrees.
        /// </summary>
        [KSPField]
        public string reverseThrustTransformName = string.Empty;

        /// <summary>
        /// Indicates whether reverse thrust is available.
        /// </summary>
        [KSPField]
        public bool canReverseThrust = true;

        /// <summary>
        /// Indicates whether the motor is currently producing reverse thrust.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool reverseThrust;

        /// <summary>
        /// Optional animation played when changing thrust direction.
        /// </summary>
        [KSPField]
        public string reverseThrustAnimation = string.Empty;

        /// <summary>
        /// Animation layer used by the reverse-thrust animation.
        /// </summary>
        [KSPField]
        public int animationLayer = 1;
        #endregion

        #region Rotor fields
        /// <summary>
        /// Name of the transform containing the visible rotor.
        /// </summary>
        [KSPField]
        public string rotorTransformName = string.Empty;

        /// <summary>
        /// Comma-separated local axis around which the rotor turns.
        /// </summary>
        [KSPField]
        public string rotorRotationAxis = "0,0,1";

        /// <summary>
        /// Base rotor speed used by the visual animation.
        /// </summary>
        [KSPField]
        public float rotorRPM = 30f;

        /// <summary>
        /// Seconds required to spool the visible rotor up or down.
        /// </summary>
        [KSPField]
        public float rotorSpoolTime = 0.3f;

        /// <summary>
        /// Multiplier applied to the normal rotor while the blurred rotor is visible.
        /// </summary>
        [KSPField]
        public float blurredRotorFactor = 4f;

        /// <summary>
        /// Percentage of maximum thrust at which the blurred rotor becomes visible.
        /// </summary>
        [KSPField]
        public float minThrustRotorBlur = 15f;

        /// <summary>
        /// Name of the blurred rotor transform.
        /// </summary>
        [KSPField]
        public string blurredRotorName = string.Empty;

        /// <summary>
        /// Rotation speed of the blurred rotor transform.
        /// </summary>
        [KSPField]
        public float blurredRotorRPM;

        /// <summary>
        /// Optional standard blade transforms that are hidden when mirrored blades are selected.
        /// </summary>
//        [KSPField]
//        public string standardBladesName = string.Empty;

        /// <summary>
        /// Optional mirrored blade transforms.
        /// </summary>
//        [KSPField]
//        public string mirrorBladesName = string.Empty;

        /// <summary>
        /// Indicates whether rotor rotation and blade selection are mirrored.
        /// </summary>
//        [KSPField(isPersistant = true)]
//        public bool mirrorRotation;

        /// <summary>
        /// Indicates whether the blurred rotor is currently displayed.
        /// </summary>
        [KSPField]
        public bool isBlurred;

        /// <summary>
        /// Degrees per second used to return the rotor to its neutral orientation after shutdown.
        /// </summary>
        [KSPField]
        public float neutralSpinRate = 15f;

        /// <summary>
        /// Indicates whether the rotor returns to its neutral orientation after stopping.
        /// </summary>
        [KSPField]
        public bool restoreToNeutralRotation = true;
        #endregion

        #region Sound fields
        /// <summary>
        /// GameDatabase URL of the looping motor sound, without a file extension.
        /// </summary>
        [KSPField]
        public string runningSound = string.Empty;

        /// <summary>
        /// Maximum running-sound volume before the stock ship-volume setting is applied.
        /// </summary>
        [KSPField]
        public float runningSoundVolume = 0.6f;

        /// <summary>
        /// Running-sound pitch when the rotor first begins turning.
        /// </summary>
        [KSPField]
        public float runningSoundPitchMin = 0.7f;

        /// <summary>
        /// Running-sound pitch at full rotor spool.
        /// </summary>
        [KSPField]
        public float runningSoundPitchMax = 1.4f;
        #endregion

        #region Housekeeping
        enum PropulsionEnvironment
        {
            Unsupported,
            Atmosphere,
            Aquatic
        }

        enum RotorState
        {
            Locked,
            Spinning,
            SlowingDown,
            RotatingUp,
            RotatingDown
        }

        GameObject generatedThrustObject;
        Transform generatedThrustTransform;
        Transform forwardDirectionTransform;
        Transform reverseDirectionTransform;
        Transform rotorTransform;
        Transform blurredRotorTransform;
        AnimationState reverseThrustAnimationState;
        PartResourceDefinition propellantDefinition;
        PartResourceDefinition environmentResourceDefinition;
        PropulsionEnvironment currentEnvironment = PropulsionEnvironment.Unsupported;
        string currentEnvironmentResourceName = string.Empty;
        Vector3 rotationAxis = Vector3.forward;
        RotorState rotorState = RotorState.Locked;
        float currentRotationAngle;
        float currentSpoolRate;
        bool evaMotorStarted;
        bool flyByWireRegistered;
        bool throttleGaugeRegistered;
        KSP.UI.Screens.Flight.ThrottleGauge throttleGauge;
        AudioSource runningAudioSource;
        #endregion

        #region Events and actions
        /// <summary>
        /// Toggles forward and reverse thrust and updates the rotor direction.
        /// </summary>
        [KSPEvent(guiActive = true, guiName = "#LOC_WILDBLUECORE_setReverseThrust",
            groupName = "EVAMotor", groupDisplayName = "#LOC_WILDBLUECORE_evaMotor")]
        public void ToggleThrustDirection()
        {
            if (!canReverseThrust)
                return;

            reverseThrust = !reverseThrust;
            updateThrustTransform();
            updateReverseAnimation();
            updateReverseThrustUI();
        }

        /// <summary>
        /// Action-group wrapper for toggling forward and reverse thrust.
        /// </summary>
        /// <param name="param">KSP action parameters.</param>
        [KSPAction("#LOC_WILDBLUECORE_toggleFwdRevThrust")]
        public void ToggleThrustDirectionAction(KSPActionParam param)
        {
            ToggleThrustDirection();
        }
        #endregion

        #region PartModule overrides
        /// <summary>
        /// Creates the center-of-mass thrust transform before stock ModuleEngines locates its transforms,
        /// then resolves the wearable's visual transforms.
        /// </summary>
        /// <param name="state">Current part-module start state.</param>
        public override void OnStart(StartState state)
        {
            createThrustTransform();
            base.OnStart(state);
            setupPAWGroup();

            if (atmosphericFlowCurve == null)
                atmosphericFlowCurve = new FloatCurve();
            if (atmosphericFlowCurve.Curve.length == 0)
                atmosphericFlowCurve.Add(0f, 1f);

            // OnLoad cannot see the runtime-created transform, so repair the stock multiplier list.
            thrustTransforms.Clear();
            thrustTransforms.Add(generatedThrustTransform);
            thrustTransformMultipliers.Clear();
            thrustTransformMultipliers.Add(1f);

            stagingEnabled = false;
            overrideStagingIconIfBlank = false;
            exhaustDamage = false;
            isBlurred = false;
            setupVisualTransforms();
            updateConsumedResources();
            updateEnvironment(true);
            updateReverseThrustUI();
            setupRunningSound();
            registerFlyByWire();
            registerThrottleGauge();
            evaMotorStarted = true;
        }

        /// <summary>
        /// Shuts the motor down and removes its generated thrust transform when KerbalGear removes
        /// the dynamic EVA module.
        /// </summary>
        public override void OnInactive()
        {
            if (EngineIgnited)
                Shutdown();

            evaMotorStarted = false;
            currentSpoolRate = 0f;
            cleanupRunningSound();
            setupBlurredState(false);
            if (generatedThrustObject != null)
                UnityEngine.Object.Destroy(generatedThrustObject);
            generatedThrustObject = null;
            generatedThrustTransform = null;
            unregisterFlyByWire();
            unregisterThrottleGauge();
            base.OnInactive();
        }

        /// <summary>
        /// Removes the vessel input callback during scene or part teardown.
        /// </summary>
        public void OnDestroy()
        {
            cleanupRunningSound();
            unregisterFlyByWire();
            unregisterThrottleGauge();
        }

        /// <summary>
        /// Supplies the throttle controls that stock FlightInputHandler skips for EVA vessels.
        /// </summary>
        public override void OnUpdate()
        {
            base.OnUpdate();
            updateEVAThrottle();
        }

        /// <summary>
        /// Reports the EVA-specific module title in the part action window.
        /// </summary>
        /// <returns>Localized EVA electric motor title.</returns>
        public override string GetModuleDisplayName()
        {
            return Localizer.Format("#LOC_WILDBLUECORE_evaMotor");
        }

        /// <summary>
        /// Treats the selected atmospheric or aquatic resource as an unlimited environmental working
        /// fluid while still requiring ElectricCharge.
        /// </summary>
        /// <param name="requiredPropellant">Stock virtual-propellant requirement.</param>
        /// <param name="propName">Name of the unavailable resource.</param>
        /// <returns>True when the environment or ElectricCharge requirement is not met.</returns>
        public override bool CheckDeprived(double requiredPropellant, out string propName)
        {
            propName = string.Empty;
            if (!environmentIsSupported())
            {
                propName = currentEnvironmentResourceName;
                return true;
            }

            if (CheatOptions.InfiniteElectricity || propellantResourceRate <= 0.0)
                return false;

            if (propellantDefinition == null)
            {
                propName = propellantResourceName;
                return true;
            }

            double available;
            double capacity;
            part.GetConnectedResourceTotals(propellantDefinition.id,
                propellantDefinition.resourceFlowMode, out available, out capacity, true);
            if (available > 0.0)
                return false;

            propName = propellantDefinition.displayName;
            return true;
        }

        /// <summary>
        /// Supplies the virtual environmental propellant and consumes ElectricCharge in proportion
        /// to the stock engine's requested mass flow.
        /// </summary>
        /// <param name="mass">Propellant mass requested by ModuleEngines for this physics tick.</param>
        /// <returns>Fraction of the request that ElectricCharge can support.</returns>
        public override double RequestPropellant(double mass)
        {
            if (!environmentIsSupported())
                return 0.0;
            if (CheatOptions.InfiniteElectricity || propellantResourceRate <= 0.0)
            {
                if (flameout)
                    UnFlameout(true);
                return 1.0;
            }
            if (propellantDefinition == null || TimeWarp.fixedDeltaTime <= 0f)
            {
                Flameout(Localizer.Format("#LOC_WILDBLUECORE_evaMotorInsufficientPower"), false, true);
                return 0.0;
            }

            double fullThrottleMass = maxFuelFlow * TimeWarp.fixedDeltaTime;
            double powerFraction = fullThrottleMass > 0.0
                ? Math.Min(1.0, Math.Max(0.0, mass / fullThrottleMass))
                : 0.0;
            double requestedCharge = propellantResourceRate * powerFraction * TimeWarp.fixedDeltaTime;
            if (requestedCharge <= 0.0)
                return 1.0;

            double availableCharge;
            double chargeCapacity;
            part.GetConnectedResourceTotals(propellantDefinition.id,
                propellantDefinition.resourceFlowMode, out availableCharge, out chargeCapacity, true);
            if (availableCharge < requestedCharge * ignitionThreshold)
            {
                Flameout(Localizer.Format("#LOC_WILDBLUECORE_evaMotorInsufficientPower"), false, true);
                return 0.0;
            }

            double consumedCharge = part.RequestResource(propellantDefinition.id,
                requestedCharge, propellantDefinition.resourceFlowMode);
            double requestRatio = Math.Min(1.0, Math.Max(0.0, consumedCharge / requestedCharge));
            if (requestRatio < ignitionThreshold)
                Flameout(Localizer.Format("#LOC_WILDBLUECORE_evaMotorInsufficientPower"), false, true);
            else if (flameout)
                UnFlameout(true);
            return requestRatio;
        }

        /// <summary>
        /// Scales atmospheric mass flow and thrust with the configured pressure curve while leaving
        /// underwater operation at full strength.
        /// </summary>
        /// <returns>Environmental flow available to the stock engine simulation.</returns>
        protected override float ModifyFlow()
        {
            if (currentEnvironment == PropulsionEnvironment.Aquatic)
                return 1f;
            if (currentEnvironment != PropulsionEnvironment.Atmosphere || part == null ||
                atmosphericFlowCurve == null)
                return 0f;

            float pressure = Mathf.Max(0f, (float)part.staticPressureAtm);
            float availableFlow = atmosphericFlowCurve.Evaluate(pressure);
            if (float.IsNaN(availableFlow) || float.IsInfinity(availableFlow))
                return 0f;
            return Mathf.Clamp01(availableFlow);
        }
        #endregion

        #region Unity lifecycle
        /// <summary>
        /// Updates environmental availability and the center-of-mass transform before running the
        /// stock engine simulation, then updates the integrated propeller animation.
        /// </summary>
        public new void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || !evaMotorStarted || part == null || vessel == null)
                return;

            updateThrustTransform();
            updateEnvironment(false);

            if (!environmentIsSupported())
            {
                finalThrust = 0f;
                if (EngineIgnited)
                    Flameout(Localizer.Format("#LOC_WILDBLUECORE_evaMotorUnsupportedEnvironment"), false, false);
                updateRotor(false);
                updateRunningSound();
                return;
            }

            if (flameout)
                UnFlameout(false);
            base.FixedUpdate();
            updateRotor(EngineIgnited && isOperational && finalThrust > 0.0001f);
            updateRunningSound();
        }
        #endregion

        #region Environment helpers
        /// <summary>
        /// Places this module's fields and events, including the PAW entries inherited from
        /// ModuleEngines, in a single EVA Motor group.
        /// </summary>
        void setupPAWGroup()
        {
            foreach (BaseField field in Fields)
            {
                field.group = new BasePAWGroup(kPAWGroupName, kPAWGroupDisplayName, false);
            }

            foreach (BaseEvent moduleEvent in Events)
            {
                moduleEvent.group = new BasePAWGroup(kPAWGroupName, kPAWGroupDisplayName, false);
            }
        }

        /// <summary>
        /// Applies the standard throttle-up, throttle-down, full-throttle, and cutoff bindings to
        /// the active EVA vessel.
        /// </summary>
        void updateEVAThrottle()
        {
            if (!HighLogic.LoadedSceneIsFlight || FlightDriver.Pause || vessel == null ||
                FlightGlobals.ActiveVessel != vessel || !vessel.isEVA)
                return;

            float throttle = Mathf.Clamp01(evaThrottle);
            if (InputLockManager.IsUnlocked(ControlTypes.THROTTLE_CUT_MAX) &&
                !GameSettings.MODIFIER_KEY.GetKey(false))
            {
                if (GameSettings.THROTTLE_CUTOFF.GetKeyDown(false))
                {
                    throttle = 0f;
                }
                else if (GameSettings.THROTTLE_FULL.GetKeyDown(false))
                {
                    throttle = 1f;
                }
            }

            if (InputLockManager.IsUnlocked(ControlTypes.THROTTLE))
            {
                float throttleDelta = 0f;
                if (GameSettings.THROTTLE_UP.GetKey(false))
                    throttleDelta += kThrottleChangeRate * Time.deltaTime;
                if (GameSettings.THROTTLE_DOWN.GetKey(false))
                    throttleDelta -= kThrottleChangeRate * Time.deltaTime;
                throttleDelta += GameSettings.AXIS_THROTTLE_INC.GetAxis() *
                    kThrottleChangeRate * Time.deltaTime;
                if (!Mathf.Approximately(throttleDelta, 0f))
                {
                    throttle = Mathf.Clamp01(throttle + throttleDelta);
                }
            }

            evaThrottle = throttle;
            if (FlightInputHandler.state != null)
                FlightInputHandler.state.mainThrottle = throttle;
            vessel.ctrlState.mainThrottle = throttle;
        }

        /// <summary>
        /// Registers the callback that supplies EVA throttle after stock input processing.
        /// </summary>
        void registerFlyByWire()
        {
            if (flyByWireRegistered || vessel == null)
                return;

            vessel.OnFlyByWire += onFlyByWire;
            flyByWireRegistered = true;
        }

        /// <summary>
        /// Stops supplying EVA throttle to the vessel.
        /// </summary>
        void unregisterFlyByWire()
        {
            if (!flyByWireRegistered || vessel == null)
                return;

            vessel.OnFlyByWire -= onFlyByWire;
            flyByWireRegistered = false;
        }

        /// <summary>
        /// Keeps the vessel control state, engine, and stock throttle display synchronized.
        /// </summary>
        void onFlyByWire(FlightCtrlState controlState)
        {
            if (controlState != null)
                controlState.mainThrottle = Mathf.Clamp01(evaThrottle);
        }

        /// <summary>
        /// Registers a pre-render callback for the stock throttle gauge. Stock KSP displays EVA
        /// throttle as the binary KerbalEVA.JetpackIsThrusting state instead of mainThrottle.
        /// </summary>
        void registerThrottleGauge()
        {
            if (throttleGaugeRegistered || !HighLogic.LoadedSceneIsFlight)
                return;

            Canvas.willRenderCanvases += updateThrottleGauge;
            throttleGaugeRegistered = true;
        }

        /// <summary>
        /// Stops overriding the stock EVA throttle gauge.
        /// </summary>
        void unregisterThrottleGauge()
        {
            if (!throttleGaugeRegistered)
                return;

            Canvas.willRenderCanvases -= updateThrottleGauge;
            throttleGaugeRegistered = false;
            throttleGauge = null;
        }

        /// <summary>
        /// Displays the motor's continuous throttle after the stock EVA gauge applies its binary
        /// jetpack indication, but before the canvas is rendered.
        /// </summary>
        void updateThrottleGauge()
        {
            if (vessel == null || FlightGlobals.ActiveVessel != vessel || !vessel.isEVA)
                return;

            if (throttleGauge == null)
                throttleGauge = UnityEngine.Object.FindObjectOfType<KSP.UI.Screens.Flight.ThrottleGauge>();
            if (throttleGauge != null && throttleGauge.gauge != null)
                throttleGauge.gauge.SetValue(Mathf.Clamp01(evaThrottle));
        }

        /// <summary>
        /// Creates the spatial looping sound used by the integrated rotor.
        /// </summary>
        void setupRunningSound()
        {
            cleanupRunningSound();
            if (string.IsNullOrEmpty(runningSound) || GameDatabase.Instance == null)
                return;

            AudioClip clip = GameDatabase.Instance.GetAudioClip(runningSound);
            if (clip == null)
            {
                Debug.LogWarning("[WBIModuleEVAMotor] Unable to load running sound: " + runningSound);
                return;
            }

            runningAudioSource = part.gameObject.AddComponent<AudioSource>();
            runningAudioSource.clip = clip;
            runningAudioSource.playOnAwake = false;
            runningAudioSource.loop = true;
            runningAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            runningAudioSource.spatialBlend = 1f;
            runningAudioSource.dopplerLevel = 0f;
            runningAudioSource.volume = 0f;
            runningAudioSource.pitch = Mathf.Max(0.01f, runningSoundPitchMin);
        }

        /// <summary>
        /// Fades and pitches the motor loop using the same spool value as the visible rotor.
        /// </summary>
        void updateRunningSound()
        {
            if (runningAudioSource == null)
                return;

            float spool = Mathf.Clamp01(currentSpoolRate);
            if (spool > kStoppedThreshold)
            {
                runningAudioSource.volume = GameSettings.SHIP_VOLUME *
                    Mathf.Max(0f, runningSoundVolume) * spool;
                runningAudioSource.pitch = Mathf.Lerp(
                    Mathf.Max(0.01f, runningSoundPitchMin),
                    Mathf.Max(0.01f, runningSoundPitchMax), spool);
                if (!runningAudioSource.isPlaying)
                    runningAudioSource.Play();
            }
            else
            {
                runningAudioSource.volume = 0f;
                if (runningAudioSource.isPlaying)
                    runningAudioSource.Stop();
            }
        }

        /// <summary>
        /// Stops and destroys the module-owned motor sound.
        /// </summary>
        void cleanupRunningSound()
        {
            if (runningAudioSource == null)
                return;

            runningAudioSource.Stop();
            UnityEngine.Object.Destroy(runningAudioSource);
            runningAudioSource = null;
        }

        /// <summary>
        /// Resolves the resource definition used to power the motor.
        /// </summary>
        void updateConsumedResources()
        {
            propellantDefinition = string.IsNullOrEmpty(propellantResourceName)
                ? null
                : PartResourceLibrary.Instance.GetDefinition(propellantResourceName);

            // ModuleEngines exposes this list through IResourceConsumer. Report the resource that
            // is actually consumed rather than the unlimited environmental working fluid.
            if (consumedResources == null)
                consumedResources = new List<PartResourceDefinition>();
            consumedResources.Clear();
            if (propellantDefinition != null)
                consumedResources.Add(propellantDefinition);
        }

        /// <summary>
        /// Detects atmosphere versus water and selects the matching virtual propellant definition.
        /// </summary>
        /// <param name="forceRefresh">Whether to rebuild the virtual propellant even if the environment has not changed.</param>
        void updateEnvironment(bool forceRefresh)
        {
            PropulsionEnvironment nextEnvironment = PropulsionEnvironment.Unsupported;
            string nextResourceName = string.Empty;

            bool underwater = vessel != null && vessel.mainBody != null && vessel.mainBody.ocean &&
                FlightGlobals.getAltitudeAtPos(vessel.CoM, vessel.mainBody) < 0.0;
            if (underwater)
            {
                nextEnvironment = PropulsionEnvironment.Aquatic;
                nextResourceName = aquaticResourceName;
            }
            else if (part != null && part.atmDensity > 0.0)
            {
                nextEnvironment = PropulsionEnvironment.Atmosphere;
                nextResourceName = atmosphericResourceName;
            }

            if (string.IsNullOrEmpty(nextResourceName))
                nextEnvironment = PropulsionEnvironment.Unsupported;

            if (!forceRefresh && nextEnvironment == currentEnvironment &&
                nextResourceName == currentEnvironmentResourceName)
                return;

            currentEnvironment = nextEnvironment;
            currentEnvironmentResourceName = nextResourceName;
            environmentResourceDefinition = string.IsNullOrEmpty(nextResourceName)
                ? null
                : PartResourceLibrary.Instance.GetDefinition(nextResourceName);
            if (environmentResourceDefinition == null)
                currentEnvironment = PropulsionEnvironment.Unsupported;

            configureVirtualPropellant();
            updateEnvironmentDisplay();
        }

        /// <summary>
        /// Replaces the stock propellant list with one virtual working fluid. No corresponding
        /// PartResource is added to the EVA part.
        /// </summary>
        void configureVirtualPropellant()
        {
            if (propellants == null)
                propellants = new List<Propellant>();
            propellants.Clear();

            if (environmentResourceDefinition == null)
                return;

            ConfigNode propellantNode = new ConfigNode("PROPELLANT");
            propellantNode.AddValue("name", environmentResourceDefinition.name);
            propellantNode.AddValue("ratio", 1.0f);
            propellantNode.AddValue("DrawGauge", false);
            Propellant virtualPropellant = new Propellant();
            virtualPropellant.Load(propellantNode);
            propellants.Add(virtualPropellant);
            SetupPropellant();
        }

        /// <summary>
        /// Determines whether the selected environment has a valid, non-massless working-fluid definition.
        /// </summary>
        /// <returns>True when thrust can be produced in the current environment.</returns>
        bool environmentIsSupported()
        {
            return currentEnvironment != PropulsionEnvironment.Unsupported &&
                environmentResourceDefinition != null && environmentResourceDefinition.density > 0f;
        }

        /// <summary>
        /// Updates the localized environment shown in the part action window.
        /// </summary>
        void updateEnvironmentDisplay()
        {
            switch (currentEnvironment)
            {
                case PropulsionEnvironment.Atmosphere:
                    environmentDisplay = Localizer.Format("#LOC_WILDBLUECORE_evaMotorAtmosphere");
                    break;
                case PropulsionEnvironment.Aquatic:
                    environmentDisplay = Localizer.Format("#LOC_WILDBLUECORE_evaMotorAquatic");
                    break;
                default:
                    environmentDisplay = Localizer.Format("#LOC_WILDBLUECORE_evaMotorUnsupported");
                    break;
            }
        }
        #endregion

        #region Thrust-transform helpers
        /// <summary>
        /// Creates the transform consumed by ModuleEngines and gives it a unique name.
        /// </summary>
        void createThrustTransform()
        {
            generatedThrustObject = new GameObject(kGeneratedThrustTransformPrefix + GetInstanceID());
            generatedThrustTransform = generatedThrustObject.transform;
            generatedThrustTransform.SetParent(part.transform, false);
            thrustVectorTransformName = generatedThrustObject.name;
            updateThrustTransform();
        }

        /// <summary>
        /// Keeps the engine force application point at vessel center of mass while copying orientation
        /// from the wearable's configured forward or reverse direction transform.
        /// </summary>
        void updateThrustTransform()
        {
            if (generatedThrustTransform == null || vessel == null)
                return;

            Transform direction = reverseThrust && reverseDirectionTransform != null
                ? reverseDirectionTransform
                : forwardDirectionTransform;
            generatedThrustTransform.position = vessel.CoM;
            if (direction != null)
                generatedThrustTransform.rotation = direction.rotation;
            else
                generatedThrustTransform.rotation = part.transform.rotation;

            // ModuleEngines applies force opposite thrustTransform.forward. The wearable's named
            // transforms point in the desired travel direction, so invert the engine transform.
            generatedThrustTransform.Rotate(0f, 180f, 0f, Space.Self);

            if (reverseThrust && reverseDirectionTransform == null)
                generatedThrustTransform.Rotate(0f, 180f, 0f, Space.Self);
        }

        /// <summary>
        /// Updates the reverse-thrust event label and visibility.
        /// </summary>
        void updateReverseThrustUI()
        {
            BaseEvent reverseEvent = Events["ToggleThrustDirection"];
            reverseEvent.active = canReverseThrust;
            reverseEvent.guiName = reverseThrust
                ? Localizer.Format("#LOC_WILDBLUECORE_setForwardThrust")
                : Localizer.Format("#LOC_WILDBLUECORE_setReverseThrust");
            Actions["ToggleThrustDirectionAction"].active = canReverseThrust;
            MonoUtilities.RefreshContextWindows(part);
        }

        /// <summary>
        /// Initializes and updates the optional reverse-thrust animation.
        /// </summary>
        void updateReverseAnimation()
        {
            if (reverseThrustAnimationState == null)
                return;
            reverseThrustAnimationState.speed = reverseThrust ? 1f : -1f;
            reverseThrustAnimationState.normalizedTime = reverseThrust ? 0f : 1f;
            reverseThrustAnimationState.enabled = true;
        }
        #endregion

        #region Rotor helpers
        /// <summary>
        /// Resolves all wearable-model transforms used by thrust direction and propeller animation.
        /// </summary>
        void setupVisualTransforms()
        {
            forwardDirectionTransform = string.IsNullOrEmpty(forwardThrustTransformName)
                ? null
                : findPartTransform(forwardThrustTransformName);
            reverseDirectionTransform = string.IsNullOrEmpty(reverseThrustTransformName)
                ? null
                : findPartTransform(reverseThrustTransformName);
            rotorTransform = string.IsNullOrEmpty(rotorTransformName)
                ? null
                : findPartTransform(rotorTransformName);
            blurredRotorTransform = string.IsNullOrEmpty(blurredRotorName)
                ? null
                : findPartTransform(blurredRotorName);
            rotationAxis = parseVector(rotorRotationAxis, Vector3.forward);

            if (!string.IsNullOrEmpty(reverseThrustAnimation))
            {
                Animation animation = part.FindModelAnimator(reverseThrustAnimation);
                if (animation != null)
                {
                    reverseThrustAnimationState = animation[reverseThrustAnimation];
                    if (reverseThrustAnimationState != null)
                    {
                        reverseThrustAnimationState.layer = animationLayer;
                        reverseThrustAnimationState.wrapMode = WrapMode.ClampForever;
                    }
                }
            }

            setupRotorVisibility();
            updateThrustTransform();
            updateReverseAnimation();
        }

        /// <summary>
        /// Advances the rotor spool, spin, blur, and neutral-return states.
        /// </summary>
        /// <param name="running">True when the stock engine is producing thrust.</param>
        void updateRotor(bool running)
        {
            if (rotorTransform == null)
                return;

            if (running)
                rotateRotorRunning();
            else
                rotateRotorShutdown();
        }

        /// <summary>
        /// Spins and spools the rotor while selecting the blurred meshes at sufficient thrust.
        /// </summary>
        void rotateRotorRunning()
        {
            rotorState = RotorState.Spinning;
            float targetSpool = maxThrust > 0f ? Mathf.Clamp01(finalThrust / maxThrust) : 0f;
            float spoolDuration = Mathf.Max(0.01f, rotorSpoolTime);
            currentSpoolRate = Mathf.MoveTowards(currentSpoolRate, targetSpool,
                TimeWarp.fixedDeltaTime / spoolDuration);

            bool shouldBlur = targetSpool >= minThrustRotorBlur / 100f && blurredRotorTransform != null;
            setupBlurredState(shouldBlur);
            float rotationRate = rotorRPM * 6f * currentSpoolRate;
            if (shouldBlur)
                rotationRate *= blurredRotorFactor;
            rotateTransform(rotorTransform, rotationRate);
            if (shouldBlur)
                rotateTransform(blurredRotorTransform, blurredRotorRPM * 6f, false);
        }

        /// <summary>
        /// Spools the rotor down and optionally returns it to its neutral local orientation.
        /// </summary>
        void rotateRotorShutdown()
        {
            setupBlurredState(false);
            float spoolDuration = Mathf.Max(0.01f, rotorSpoolTime);
            currentSpoolRate = Mathf.MoveTowards(currentSpoolRate, 0f,
                TimeWarp.fixedDeltaTime / spoolDuration);
            if (currentSpoolRate > kStoppedThreshold)
            {
                rotorState = RotorState.SlowingDown;
                rotateTransform(rotorTransform, rotorRPM * 6f * currentSpoolRate);
                return;
            }

            currentSpoolRate = 0f;
            if (!restoreToNeutralRotation)
            {
                rotorState = RotorState.Locked;
                return;
            }

            float signedAngle = Mathf.DeltaAngle(currentRotationAngle, 0f);
            if (Mathf.Abs(signedAngle) <= neutralSpinRate * TimeWarp.fixedDeltaTime)
            {
                currentRotationAngle = 0f;
                rotorState = RotorState.Locked;
                return;
            }

            rotorState = signedAngle > 0f ? RotorState.RotatingUp : RotorState.RotatingDown;
            float step = Mathf.Sign(signedAngle) * neutralSpinRate * TimeWarp.fixedDeltaTime;
            rotateTransform(rotorTransform, step / Mathf.Max(TimeWarp.fixedDeltaTime, 0.0001f));
        }

        /// <summary>
        /// Rotates a propeller transform and records its accumulated angle.
        /// </summary>
        /// <param name="target">Transform to rotate.</param>
        /// <param name="degreesPerSecond">Requested angular speed.</param>
        /// <param name="trackRotation">Whether this transform contributes to the normal rotor's neutral-return angle.</param>
        void rotateTransform(Transform target, float degreesPerSecond, bool trackRotation = true)
        {
            if (target == null)
                return;
            float direction = 1f;
            if (reverseThrust)
                direction *= -1f;
            float angle = degreesPerSecond * TimeWarp.fixedDeltaTime * direction;
            if (trackRotation)
                currentRotationAngle = Mathf.Repeat(currentRotationAngle + angle, 360f);
            target.Rotate(rotationAxis * angle, Space.Self);
        }

        /// <summary>
        /// Shows the appropriate normal, mirrored, and blurred blade meshes.
        /// </summary>
        void setupRotorVisibility()
        {
            if (rotorTransform != null)
                rotorTransform.gameObject.SetActive(!isBlurred);
            if (blurredRotorTransform != null)
                blurredRotorTransform.gameObject.SetActive(isBlurred);
        }

        /// <summary>
        /// Changes blurred-rotor state only when necessary.
        /// </summary>
        /// <param name="newBlurredState">True to display the blurred rotor.</param>
        void setupBlurredState(bool newBlurredState)
        {
            if (isBlurred == newBlurredState)
                return;
            isBlurred = newBlurredState;
            setupRotorVisibility();
        }

        /// <summary>
        /// Finds all named transforms in a comma-separated list.
        /// </summary>
        /// <param name="transformNames">Comma-separated transform names.</param>
        /// <returns>Transforms found on the EVA part hierarchy.</returns>
        Transform[] getTransforms(string transformNames)
        {
            if (string.IsNullOrEmpty(transformNames))
                return new Transform[0];

            List<Transform> transforms = new List<Transform>();
            string[] names = transformNames.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int index = 0; index < names.Length; index++)
            {
                Transform target = findPartTransform(names[index].Trim());
                if (target != null)
                    transforms.Add(target);
            }
            return transforms.ToArray();
        }

        /// <summary>
        /// Finds a transform anywhere below the EVA part. Wearable props are instantiated beside
        /// the stock model hierarchy, so Part.FindModelTransform cannot resolve their transforms.
        /// </summary>
        Transform findPartTransform(string transformName)
        {
            if (part == null || string.IsNullOrEmpty(transformName))
                return null;

            return part.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(candidate => candidate.name == transformName);
        }

        /// <summary>
        /// Shows or hides an array of blade transforms and their colliders.
        /// </summary>
        /// <param name="transforms">Transforms to update.</param>
        /// <param name="isVisible">Desired visibility.</param>
        void setTransformsVisible(Transform[] transforms, bool isVisible)
        {
            if (transforms == null)
                return;
            for (int index = 0; index < transforms.Length; index++)
            {
                Transform target = transforms[index];
                if (target == null)
                    continue;
                target.gameObject.SetActive(isVisible);
                Collider targetCollider = target.GetComponent<Collider>();
                if (targetCollider != null)
                    targetCollider.enabled = isVisible;
            }
        }

        /// <summary>
        /// Parses a comma-separated Vector3 field.
        /// </summary>
        /// <param name="value">Serialized vector.</param>
        /// <param name="fallback">Value returned when parsing fails.</param>
        /// <returns>Parsed vector or the supplied fallback.</returns>
        Vector3 parseVector(string value, Vector3 fallback)
        {
            if (string.IsNullOrEmpty(value))
                return fallback;
            string[] values = value.Split(',');
            float x;
            float y;
            float z;
            if (values.Length != 3 || !float.TryParse(values[0], out x) ||
                !float.TryParse(values[1], out y) || !float.TryParse(values[2], out z))
                return fallback;
            return new Vector3(x, y, z);
        }
        #endregion
    }
}
