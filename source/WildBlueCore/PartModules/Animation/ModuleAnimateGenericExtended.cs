using System.Collections.Generic;
using System.Text;
using KSP.Localization;
using UnityEngine;
using Experience;

namespace WildBlueCore.PartModules
{
    internal struct ConsumptionResource
    {
        public Vessel vessel;
        public ModuleResource resource;
    }

    /// <summary>
    /// This part module derives from ModuleAnimatedGeneric. It adds the ability to play sound effects while animating.
    /// It also adds several options to check before playing the animation.
    /// First, it can check the active vessel for a necessary skill and skill level.
    /// Second, it can check for and consume required resources. Those resources can be pulled from the part's vessel and/or from a remote vessel.
    /// Third, it can check for and consume parts stored in an inventory. Those parts can be pulled form the parts vessel and/or from a remote vessel.
    /// Finally, you can provide a list of part modules that will be enabled after tne animation completes, and disabled when when not complete.
    /// </summary>
    public class ModuleAnimateGenericExtended: ModuleAnimateGeneric
    {
        #region Fields
        /// <summary>
        /// Debug flag
        /// </summary>
        [KSPField]
        public bool debugMode = false;

        #region Requirements
        /// <summary>
        /// Flag indicating whether or not to require a skill check before playing the start event animation. Default is false.
        /// </summary>
        [KSPField(guiName = "Start Event Needs Skill")]
        [UI_Toggle(enabledText = "On", disabledText = "Off")]
        public bool startEventRequiresSkillCheck = false;

        /// <summary>
        /// Flag indicating whether or not to require a skill check before playing the end event animation. Default is false.
        /// </summary>
        [KSPField(guiName = "End Event Needs Skill")]
        [UI_Toggle(enabledText = "On", disabledText = "Off")]
        public bool endEventRequiresSkillCheck = false;

        /// <summary>
        /// The skill required to unpack the part.
        /// </summary>
        [KSPField]
        public string skillToCheck = string.Empty;

        /// <summary>
        /// The minimum skill level required to unpack the box. Default is 0.
        /// </summary>
        [KSPField]
        public int minimumSkillLevel = 0;

        /// <summary>
        /// Flag to indicate whether or not to make a resource requirements check before playing the start event animation. Default is false.
        /// </summary>
        [KSPField(guiName = "Start Event Needs Resources")]
        [UI_Toggle(enabledText = "On", disabledText = "Off")]
        public bool startEventRequiresResources = false;

        /// <summary>
        /// Flag to indicate whether or not to make a resource requirements check before playing the end event animation. Default is false.
        /// </summary>
        [KSPField(guiName = "End Event Gives Resources")]
        [UI_Toggle(enabledText = "On", disabledText = "Off")]
        public bool endEventGivesResources = false;

        /// <summary>
        /// Flag to indicate whether or not when checking resources,
        /// resources can come from other vessels.
        /// Default is true.
        /// </summary>
        [KSPField]
        public bool canUseRemoteResources = true;
        #endregion

        #region Effects
        /// <summary>
        /// URL for the start sound played when the animation starts.
        /// </summary>
        [KSPField]
        public string startSoundURL = string.Empty;

        /// <summary>
        /// Pitch level for the start sound.
        /// </summary>
        [KSPField]
        public float startSoundPitch = 1.0f;

        /// <summary>
        /// Volume level for the start sound.
        /// </summary>
        [KSPField]
        public float startSoundVolume = 0.5f;

        /// <summary>
        /// URL for the loop sound, played while the animation is playing.
        /// </summary>
        [KSPField]
        public string loopSoundURL = string.Empty;

        /// <summary>
        /// Pitch level for the loop sound.
        /// </summary>
        [KSPField]
        public float loopSoundPitch = 1.0f;

        /// <summary>
        /// Volume level for the loop sound.
        /// </summary>
        [KSPField]
        public float loopSoundVolume = 0.5f;

        /// <summary>
        /// URL four the stop sound, played when the animation is completed.
        /// </summary>
        [KSPField]
        public string stopSoundURL = string.Empty;

        /// <summary>
        /// Pitch level for the stop sound.
        /// </summary>
        [KSPField]
        public float stopSoundPitch = 1.0f;

        /// <summary>
        /// Volume level for the stop sound.
        /// </summary>
        [KSPField]
        public float stopSoundVolume = 0.5f;
        #endregion

        #endregion

        #region Housekeeping
        List<ConsumptionResource> consumptionResources;
        public bool hasStarted = false;
        AudioSource loopSound = null;
        AudioSource startSound = null;
        AudioSource stopSound = null;
        #endregion

        #region Overrides
        public override void OnAwake()
        {
            base.OnAwake();
            debugMode = WildBlueCoreScenario.debugMode;
            startEventRequiresSkillCheck = WildBlueCoreScenario.startEventRequiresSkillCheck;
            endEventRequiresSkillCheck = WildBlueCoreScenario.endEventRequiresSkillCheck;
            startEventRequiresResources = WildBlueCoreScenario.startEventRequiresResources;
            endEventGivesResources = WildBlueCoreScenario.endEventGivesResources;
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            if (debugMode)
            {
                Fields["startEventRequiresSkillCheck"].guiActive = true;
                Fields["endEventRequiresSkillCheck"].guiActive = true;

                if (resHandler.inputResources.Count > 0)
                    Fields["startEventRequiresResources"].guiActive = true;

                if (resHandler.outputResources.Count > 0)
                    Fields["endEventGivesResources"].guiActive = true;
            }

            // Disable part modules if we haven't completed the animation.
            updateManagedModules();

            setupSounds();

            OnMoving.Add(onMovingAnimation);
            OnStop.Add(onStopAnimation);
        }

        /// <summary>
        /// Toggles the animation.
        /// </summary>
        public new void Toggle()
        {
            if (!HighLogic.LoadedSceneIsFlight)
            {
                base.Toggle();
                return;
            }

            // Check for requirements
            if (!canToggleAnimation())
                return;

            // Consume resources
            consumeResources();

            PlayStartSound();

            base.Toggle();

            // Disable part modules if needed.
            if (Events["Toggle"].guiName == endEventGUIName)
                updateManagedModules();
        }

        /// <summary>
        /// Action to toggle the animation.
        /// </summary>
        /// <param name="param"></param>
        public new void ToggleAction(KSPActionParam param)
        {
            if (!HighLogic.LoadedSceneIsFlight)
            {
                base.ToggleAction(param);
                return;
            }

            // Check for requirements
            if (!canToggleAnimation())
                return;

            // Consume resources
            consumeResources();

            PlayStartSound();

            base.ToggleAction(param);

            // Disable part modules if needed.
            if (Events["Toggle"].guiName == endEventGUIName)
                updateManagedModules();
        }

        public override string GetInfo()
        {
            StringBuilder info = new StringBuilder();

            info.AppendLine(Localizer.Format("#LOC_WILDBLUECORE_moduleAGEDesc"));

            if (startEventRequiresSkillCheck)
            {
                info.AppendLine();
                info.AppendLine(Localizer.Format("#LOC_WILDBLUECORE_moduleAGESkillNeeded"));
                info.AppendLine(startEventGUIName);
            }

            if (endEventRequiresSkillCheck)
            {
                info.AppendLine();
                info.AppendLine(Localizer.Format("#LOC_WILDBLUECORE_moduleAGESkillNeeded"));
                info.AppendLine(endEventGUIName);
            }

            int count;
            if (startEventRequiresResources && resHandler.inputResources.Count > 0)
            {
                info.AppendLine();
                info.AppendLine(Localizer.Format("#LOC_WILDBLUECORE_moduleAGEResourcesNeeded"));
                info.AppendLine(startEventGUIName);
                count = resHandler.inputResources.Count;
                for (int index = 0; index < count; index++)
                    info.AppendLine(getResourceInfo(resHandler.inputResources[index]));
            }

            if (endEventGivesResources && resHandler.outputResources.Count > 0)
            {
                info.AppendLine();
                info.AppendLine(Localizer.Format("#LOC_WILDBLUECORE_moduleAGEResourcesGiven"));
                info.AppendLine(endEventGUIName);
                count = resHandler.outputResources.Count;
                for (int index = 0; index < count; index++)
                    info.AppendLine(getResourceInfo(resHandler.outputResources[index]));
            }

            return info.ToString();
        }

        public new void FixedUpdate()
        {
            base.FixedUpdate();
        }

        public void OnDestroy()
        {
            OnMoving.Remove(onMovingAnimation);
            OnStop.Remove(onStopAnimation);
        }
        #endregion

        #region API
        /// <summary>
        /// Sets the Progress level. Call this instead of setting deployPercent directly.
        /// </summary>
        /// <param name="toValue">A float between 0 and 100.</param>
        public void SetProgress(float toValue)
        {
            float value = toValue;
            if (value < 0)
                value = 0;
            else if (value > 100)
                value = 100;

            deployPercent = value;

            // Update managed modules if the deployPercent < 100;
            if (deployPercent < 100)
            {
                FixedUpdate();
                updateManagedModules();
            }
        }

        /// <summary>
        /// Plays the start sound.
        /// </summary>
        public void PlayStartSound()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            if (startSound != null)
                startSound.Play();

            if (loopSound != null)
                loopSound.Play();
        }

        /// <summary>
        /// Plays the end sound.
        /// </summary>
        public void PlayEndSound()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            if (stopSound != null)
                stopSound.Play();

            if (loopSound != null)
                loopSound.Stop();
        }
        #endregion

        #region Helpers
        bool canToggleAnimation()
        {
            // Check for skill requirements
            if (!hasRequiredSkill())
                return false;

            // Check for resource requirements
            if (!hasRequiredResources())
                return false;

            // Check for part requirements

            return true;
        }

        bool hasRequiredResources()
        {
            consumptionResources = new List<ConsumptionResource>();

            if ((Events["Toggle"].guiName == startEventGUIName && !startEventRequiresResources) || resHandler.inputResources.Count <= 0)
                return true;
            else if (Events["Toggle"].guiName == endEventGUIName)
                return true;

            int count = resHandler.inputResources.Count;
            double amount, maxAmount;
            ModuleResource moduleResource;
            Vessel supplyVessel;
            int loadedVesselCount;
            bool resourceIsAvailable;
            ConsumptionResource consumptionResource;
            for (int index = 0; index < count; index++)
            {
                moduleResource = resHandler.inputResources[index];
                resourceIsAvailable = false;

                // Check locally first.
                vessel.GetConnectedResourceTotals(moduleResource.id, out amount, out maxAmount);
                if (amount >= moduleResource.amount)
                {
                    // Add the resource and vessel to our resource consumption list.
                    consumptionResource = new ConsumptionResource();
                    consumptionResource.vessel = vessel;
                    consumptionResource.resource = moduleResource;
                    consumptionResources.Add(consumptionResource);
                    resourceIsAvailable = true;
                }

                // Not locally found. Check remote vessels
                else if (canUseRemoteResources)
                {
                    // If the supplyVessel has enough of the resource then add the vessel and resource to our consumption list.
                    loadedVesselCount = FlightGlobals.VesselsLoaded.Count;
                    for (int loadedVesselIndex = 0; loadedVesselIndex < loadedVesselCount; loadedVesselIndex++)
                    {
                        supplyVessel = FlightGlobals.VesselsLoaded[loadedVesselIndex];
                        if (supplyVessel == part.vessel)
                            continue;

                        supplyVessel.GetConnectedResourceTotals(moduleResource.id, out amount, out maxAmount);
                        if (amount >= moduleResource.amount)
                        {
                            // Add the resource and vessel to our resource consumption list.
                            consumptionResource = new ConsumptionResource();
                            consumptionResource.vessel = supplyVessel;
                            consumptionResource.resource = moduleResource;
                            consumptionResources.Add(consumptionResource);
                            resourceIsAvailable = true;
                            break;
                        }
                    }
                }

                // Final check
                if (!resourceIsAvailable)
                {
                    string[] messages = { string.Format("{0:n1}", moduleResource.amount), moduleResource.resourceDef.displayName, Events["Toggle"].guiName };
                    string errorMessage = Localizer.Format("#LOC_WILDBLUECORE_missingResource", messages);
                    ScreenMessages.PostScreenMessage(errorMessage, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    return false;
                }
            }

            return true;
        }

        bool hasRequiredSkill()
        {
            if ((Events["Toggle"].guiName == startEventGUIName && !startEventRequiresSkillCheck) || string.IsNullOrEmpty(skillToCheck))
                return true;
            else if ((Events["Toggle"].guiName == endEventGUIName && !endEventRequiresSkillCheck) || string.IsNullOrEmpty(skillToCheck))
                return true;

            ProtoCrewMember astronaut;
            int highestSkill;

            // Make sure that we have sufficient skill.
            if (vessel.isEVA)
                highestSkill = getHighestRank(vessel, out astronaut);
            else
                highestSkill = getHighestRank(vessel, out astronaut);

            if (astronaut == null || highestSkill < minimumSkillLevel)
            {
                string[] messages = { skillToCheck, Events["Toggle"].guiName };
                string errorMessage = Localizer.Format("#LOC_WILDBLUECORE_missingSkill", messages);
                ScreenMessages.PostScreenMessage(errorMessage, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                return false;
            }

            return true;
        }

        int getHighestRank(Vessel vessel, out ProtoCrewMember astronaut)
        {
            astronaut = null;
            if (string.IsNullOrEmpty(skillToCheck))
                return 0;
            try
            {
                if (vessel.GetCrewCount() == 0)
                    return 0;
            }
            catch
            {
                return 0;
            }

            string[] skillsToCheck = skillToCheck.Split(new char[] { ';' });
            string checkSkill;
            int highestRank = 0;
            int crewRank = 0;
            bool hasABadass = false;
            bool hasAVeteran = false;
            bool hasAHero = false;
            for (int skillIndex = 0; skillIndex < skillsToCheck.Length; skillIndex++)
            {
                checkSkill = skillsToCheck[skillIndex];

                //Find the highest racking kerbal with the desired skill (if any)
                ProtoCrewMember[] vesselCrew = vessel.GetVesselCrew().ToArray();
                for (int index = 0; index < vesselCrew.Length; index++)
                {
                    if (vesselCrew[index].HasEffect(checkSkill))
                    {
                        if (vesselCrew[index].isBadass)
                            hasABadass = true;
                        if (vesselCrew[index].veteran)
                            hasAVeteran = true;
                        if (vesselCrew[index].isHero)
                            hasAHero = true;
                        crewRank = vesselCrew[index].experienceTrait.CrewMemberExperienceLevel();
                        if (crewRank > highestRank)
                        {
                            highestRank = crewRank;
                            astronaut = vesselCrew[index];
                        }
                    }
                }
            }

            if (hasABadass)
                highestRank += 1;
            if (hasAVeteran)
                highestRank += 1;
            if (hasAHero)
                highestRank += 1;

            return highestRank;
        }

        void consumeResources()
        {
            // Give resources back
            if (endEventGivesResources && Events["Toggle"].guiName == endEventGUIName && endEventGivesResources && resHandler.outputResources.Count > 0)
            {
                int resCount = resHandler.outputResources.Count;
                for (int index = 0; index < resCount; index++)
                {
                    part.RequestResource(resHandler.outputResources[index].resourceDef.name, -resHandler.outputResources[index].amount, ResourceFlowMode.ALL_VESSEL, false);
                }
                return;
            }

            // Consume resournces
            int count = consumptionResources.Count;
            if (count <= 0)
                return;

            ConsumptionResource consumptionResource;
            for (int index = 0; index < count; index++)
            {
                consumptionResource = consumptionResources[index];
                consumptionResource.vessel.RequestResource(consumptionResource.vessel.rootPart, consumptionResource.resource.id, consumptionResource.resource.amount, true);
            }
        }

        void updateManagedModules()
        {
            ConfigNode node = getPartConfigNode();
            if (node == null)
                return;
            List<PartModule> managedModules = getManagedModules(node);
            int count = managedModules.Count;
            if (count <= 0)
                return;
            bool isDeployed = Events["Toggle"].guiName == endEventGUIName;
            if (debugMode)
            {
                Debug.Log("[ModuleAnimateGenericExtended] - animTime: " + animTime);
                Debug.Log("[ModuleAnimateGenericExtended] - isDeployed: " + isDeployed);
            }

            PartModule module;
            for (int index = 0; index < count; index++)
            {
                module = managedModules[index];

                if (isDeployed)
                {
                    module.moduleIsEnabled = true;
                    module.enabled = true;
                    module.isEnabled = true;
                    module.OnActive();
                    if (module is BaseConverter)
                    {
                        BaseConverter converter = (BaseConverter)module;
                        converter.EnableModule();
                    }
                }
                else
                {
                    module.OnInactive();
                    module.isEnabled = false;
                    module.enabled = false;
                    module.moduleIsEnabled = false;
                    if (module is BaseConverter)
                    {
                        BaseConverter converter = (BaseConverter)module;
                        converter.DisableModule();
                    }
                }
            }

            //Dirty the GUI
            MonoUtilities.RefreshContextWindows(part);
        }

        List<PartModule> getManagedModules(ConfigNode node)
        {
            List<PartModule> managedModules = new List<PartModule>();
            if (!node.HasNode("MANAGED_MODULES"))
            {
                if (debugMode)
                    Debug.Log("[ModuleAnimateGenericExtended] - MANAGED_MODULES not found");
                return managedModules;
            }
            ConfigNode modulesNode = node.GetNode("MANAGED_MODULES");

            string[] moduleNames = modulesNode.GetValues("moduleName");
            string managedModuleName;
            for (int nameIndex = 0; nameIndex < moduleNames.Length; nameIndex++)
            {
                managedModuleName = moduleNames[nameIndex];

                if (part.Modules.Contains(managedModuleName))
                    managedModules.Add(part.Modules[managedModuleName]);
            }

            if (debugMode)
                Debug.Log("[ModuleAnimateGenericExtended] - found " + managedModules.Count + " modules to manage.");
            return managedModules;
        }

        ConfigNode getPartConfigNode(string className = "")
        {
            if (!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight)
                return null;
            if (part.partInfo.partConfig == null)
            {
                if (debugMode)
                    Debug.Log("[ModuleAnimateGenericExtended] - part.partInfo.partConfig == null");
                return null;
            }
            ConfigNode[] nodes = this.part.partInfo.partConfig.GetNodes("MODULE");
            ConfigNode partConfigNode = null;
            ConfigNode node = null;
            string moduleName;
            string nodeModuleID;

            //Get the config node.
            for (int index = 0; index < nodes.Length; index++)
            {
                node = nodes[index];
                if (node.HasValue("name"))
                {
                    moduleName = node.GetValue("name");
                    if (moduleName == this.ClassName || moduleName == className)
                    {
                        if (!string.IsNullOrEmpty(moduleID) && node.HasValue("moduleID"))
                        {
                            nodeModuleID = node.GetValue("moduleID");
                            if (moduleID == nodeModuleID)
                            {
                                partConfigNode = node;
                                break;
                            }
                        }
                        else
                        {
                            partConfigNode = node;
                            break;
                        }
                    }
                }
            }

            if (debugMode)
                Debug.Log("[ModuleAnimateGenericExtended] - found partConfigNode");
            return partConfigNode;
        }

        protected virtual void setupSounds()
        {
            if (!string.IsNullOrEmpty(startSoundURL))
            {
                startSound = gameObject.AddComponent<AudioSource>();
                startSound.clip = GameDatabase.Instance.GetAudioClip(startSoundURL);
                startSound.pitch = startSoundPitch;
                startSound.volume = GameSettings.SHIP_VOLUME * startSoundVolume;
            }

            if (!string.IsNullOrEmpty(loopSoundURL))
            {
                loopSound = gameObject.AddComponent<AudioSource>();
                loopSound.clip = GameDatabase.Instance.GetAudioClip(loopSoundURL);
                loopSound.loop = true;
                loopSound.pitch = loopSoundPitch;
                loopSound.volume = GameSettings.SHIP_VOLUME * loopSoundVolume;
            }

            if (!string.IsNullOrEmpty(stopSoundURL))
            {
                stopSound = gameObject.AddComponent<AudioSource>();
                stopSound.clip = GameDatabase.Instance.GetAudioClip(stopSoundURL);
                stopSound.pitch = stopSoundPitch;
                stopSound.volume = GameSettings.SHIP_VOLUME * stopSoundVolume;
            }
        }

        void onMovingAnimation(float time, float speed)
        {
            if (hasStarted)
            {
                hasStarted = true;
                PlayStartSound();
            }
        }

        void onStopAnimation(float deployedPercentage)
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            hasStarted = false;
            PlayEndSound();
            updateManagedModules();
        }

        string getResourceInfo(ModuleResource resource)
        {
            PartResourceDefinition definition = PartResourceLibrary.Instance.GetDefinition(resource.name.GetHashCode());
            string resourceName = definition == null ? resource.title : definition.displayName;

            return Localizer.Format("#LOC_WILDBLUECORE_resourceInfo", new string[2] { resourceName, string.Format("{0:n2}", resource.amount) });
        }
        #endregion
    }
}
