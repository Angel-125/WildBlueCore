using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KSP.Localization;

namespace WildBlueCore.PartModules.Resources
{
    public class WBIModuleResourceConverterExtended: ModuleResourceConverter
    {
        #region constants
        private const float kminimumSuccess = 80f;
        private const float kCriticalSuccess = 95f;
        private const float kCriticalFailure = 33f;
        private const float kDefaultHoursPerCycle = 1.0f;
        private const double kHighTimewarpDelta = 0.2;

        //Summary messages for lastAttempt
        protected string attemptCriticalFail = "Critical Failure";
        protected string attemptCriticalSuccess = "Critical Success";
        protected string attemptFail = "Fail";
        protected string attemptSuccess = "Success";

        //User messages for last attempt
        public float kMessageDuration = 5.0f;
        [KSPField]
        public string criticalFailMessage = string.Empty;
        [KSPField]
        public string criticalSuccessMessage = string.Empty;
        [KSPField]
        public string failMessage = string.Empty;
        [KSPField]
        public string successMessage = string.Empty;
        #endregion

        #region FX fields
        /// <summary>
        /// Name of the effect to play when the converter starts.
        /// </summary>
        [KSPField]
        public string startEffect = string.Empty;

        /// <summary>
        /// Name of the effect to play when the converter stops.
        /// </summary>
        [KSPField]
        public string stopEffect = string.Empty;

        /// <summary>
        /// Name of the effect to play while the converter is running.
        /// </summary>
        [KSPField]
        public string runningEffect = string.Empty;
        #endregion

        #region Converter Fields
        /// <summary>
        /// This field describes how much ElectricCharge is consumed per second. A negative number indicates consumption.
        /// </summary>
        [KSPField]
        public double ecPerSec = 0;

        /// <summary>
        /// This is a threshold value to ensure that the converter will shut off if the vessel's
        /// ElectricCharge falls below the specified percentage. It is ignored if the converter doesn't
        /// use ElectricCharge.
        /// </summary>
        [KSPField]
        public int minimumVesselPercentEC = 5;

        /// <summary>
        /// This flag tells the converter to check for a connection to the homeworld if set to true.
        /// If no connection is present, then the converter operations are suspended. It requires
        /// CommNet to be enabled.
        /// </summary>
        [KSPField]
        public bool requiresHomeConnection;

        /// <summary>
        /// This field specifies the minimum number of crew required to operate the converter. If the part
        /// lacks the minimum required crew, then operations are suspended.
        /// </summary>
        [KSPField]
        public int minimumCrew = 0;

        /// <summary>
        /// This field indicates whether or not the converter can be shut down. If set to false, then the converter
        /// will remove the shutdown and toggle actions and disable the shutdown button.
        /// </summary>
        [KSPField]
        public bool canBeShutdown = true;

        /// <summary>
        /// Flag to indicate that the converter's part must be splashed in order to function.
        /// </summary>
        [KSPField]
        public bool requiresSplashed = false;

        /// <summary>
        /// Flag indicating that the converter requires an oxygenated atmosphere in order to run.
        /// </summary>
        [KSPField]
        public bool requiresOxygen = false;
        #endregion

        #region Timed Resource Fields
        /// <summary>
        /// Minimum die roll
        /// </summary>
        public int dieRollMin = 1;

        /// <summary>
        /// Maximum die roll
        /// </summary>
        public int dieRollMax = 100;

        /// <summary>
        /// On a roll of dieRollMin - dieRollMax, the minimum roll required to declare a successful resource yield. Set to 0 if you don't want to roll for success.
        /// </summary>
        [KSPField]
        public int minimumSuccess;

        /// <summary>
        /// On a roll of dieRollMin - dieRollMax, minimum roll for a resource yield to be declared a critical success.
        /// </summary>
        [KSPField]
        public int criticalSuccess;

        /// <summary>
        /// On a roll of dieRollMin - dieRollMax, the maximum roll for a resource yield to be declared a critical failure.
        /// </summary>
        [KSPField]
        public int criticalFail;

        /// <summary>
        /// How many hours to wait before producing resources defined by YIELD_RESOURCE nodes.
        /// </summary>
        [KSPField]
        public double hoursPerCycle;

        /// <summary>
        /// The time at which we started a new resource production cycle.
        /// </summary>
        [KSPField(isPersistant = true)]
        public double cycleStartTime;

        /// <summary>
        /// Current progress of the production cycle
        /// </summary>
        [KSPField(guiActive = true, guiName = "Progress", isPersistant = true)]
        public string progress = string.Empty;

        /// <summary>
        /// Display field to show time remaining on the production cycle.
        /// </summary>
        [KSPField(guiActive = true, guiName = "Time Remaining")]
        public string timeRemainingDisplay = string.Empty;

        /// <summary>
        /// Results of the last production cycle attempt.
        /// </summary>
        [KSPField(guiActive = true, guiName = "Last Attempt", isPersistant = true)]
        public string lastAttempt = string.Empty;

        /// <summary>
        /// If the yield check is a critical success, multiply the units produced by this number. Default is 1.0.
        /// </summary>
        [KSPField]
        public double criticalSuccessMultiplier = 1.0;

        /// <summary>
        /// If the yield check is a failure, multiply the units produced by this number. Default is 1.0.
        /// </summary>
        [KSPField]
        public double failureMultiplier = 1.0;

        /// <summary>
        /// Flag to indicate whether or not the part explodes if the yield roll critically fails.
        /// </summary>
        [KSPField]
        public bool explodeUponCriticalFail = false;
        #endregion

        #region Housekeeping
        [KSPField(isPersistant = true)]
        public double inputEfficiency = 1f;

        [KSPField(isPersistant = true)]
        public double outputEfficiency = 1f;

        /// <summary>
        /// The amount of time that has passed since the converter was last checked if it should produce yield resources.
        /// </summary>
        public double elapsedTime;

        /// <summary>
        /// The number of seconds per yield cycle.
        /// </summary>
        public double secondsPerCycle = 0f;

        /// <summary>
        /// The list of resources to produce after the elapsedTime matches the secondsPerCycle.
        /// </summary>
        public List<ResourceRatio> yieldsList = new List<ResourceRatio>();

        /// <summary>
        /// The converter is missing resources. If set to true then the converter's operations are suspended.
        /// </summary>
        protected bool missingResources;

        /// <summary>
        /// The efficieny bonus of the crew.
        /// </summary>
        protected float crewEfficiencyBonus = 1.0f;

        protected bool debugMode;
        #endregion

        #region Overrides
        public override string GetInfo()
        {
            string moduleInfo = base.GetInfo();
            StringBuilder info = new StringBuilder();
            ConfigNode[] moduleNodes;
            ConfigNode node = null;
            ConfigNode[] yieldNodes = null;
            ConfigNode yieldNode;
            string moduleName;

            // Splashed
            if (requiresSplashed)
                moduleInfo = moduleInfo.Replace(ConverterName, ConverterName + "\n - Requires vessel to be in water");

            // Splashed
            if (requiresOxygen)
                moduleInfo = moduleInfo.Replace(ConverterName, ConverterName + "\n - Requires vessel to be on a planet with an oxygenated atmosphere");

            //Home connection
            if (requiresHomeConnection)
                moduleInfo = moduleInfo.Replace(ConverterName, ConverterName + "\n - Requires connection to homeworld");

            //Minimum crew
            if (minimumCrew > 0)
                moduleInfo = moduleInfo.Replace(ConverterName, ConverterName + "\n - Minimum Crew: " + minimumCrew);

            //Trim Outputs if we have none
            if (moduleInfo.EndsWith(Localizer.Format("#autoLOC_259594")))
                moduleInfo = moduleInfo.Replace(Localizer.Format("#autoLOC_259594"), "");

            info = new StringBuilder();
            info.AppendLine(moduleInfo);

            //See if the module has a yield list. If so, get it.
            moduleNodes = this.part.partInfo.partConfig.GetNodes("MODULE");
            for (int index = 0; index < moduleNodes.Length; index++)
            {
                node = moduleNodes[index];

                if (node.HasValue("name"))
                    moduleName = node.GetValue("name");
                else
                    continue;

                if (node.HasNode("YIELD_RESOURCE"))
                    yieldNodes = node.GetNodes("YIELD_RESOURCE");
            }

            //If we found a yield resource list then add the info.
            if (yieldNodes != null)
            {
                double processTimeHours = 0;
                if (node.HasValue("hoursPerCycle"))
                    double.TryParse(node.GetValue("hoursPerCycle"), out processTimeHours);

                info.Append(" - Skill Needed: " + ExperienceEffect + "\r\n");
                if (processTimeHours > 0)
                {
                    info.Append(" - Process Time: ");
                    info.Append(string.Format("{0:f1} hours\r\n", processTimeHours));
                }
                info.Append(" - Yield Resources\r\n");
                for (int yieldIndex = 0; yieldIndex < yieldNodes.Length; yieldIndex++)
                {
                    yieldNode = yieldNodes[yieldIndex];
                    if (yieldNode.HasValue("ResourceName") && yieldNode.HasValue("Ratio"))
                    {
                        info.AppendLine("  - " + yieldNode.GetValue("ResourceName") + ": " + yieldNode.GetValue("Ratio"));
                    }
                }
            }

            return info.ToString();
        }

        private void OnDestroy()
        {
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            debugMode = WildBlueCoreScenario.debugMode;

            //Hide action buttons if needed
            if (!canBeShutdown)
            {
                Actions["StopResourceConverterAction"].active = false;
                Actions["StopResourceConverterAction"].actionGroup = KSPActionGroup.None;
                Actions["ToggleResourceConverterAction"].active = false;
                Actions["ToggleResourceConverterAction"].actionGroup = KSPActionGroup.None;
            }

            //Load yield resources if needed
            loadYieldsList();
            if (yieldsList.Count == 0)
            {
                Fields["progress"].guiActive = false;
                Fields["lastAttempt"].guiActive = false;
                Fields["timeRemainingDisplay"].guiActive = false;
            }

            //Do a quick preprocessing to update our input/output effincies
            secondsPerCycle = hoursPerCycle * 3600;
            PreProcessing();
        }

        public override void StartResourceConverter()
        {
            base.StartResourceConverter();

            if (!string.IsNullOrEmpty(runningEffect))
                this.part.Effect(startEffect, 1.0f);

            cycleStartTime = Planetarium.GetUniversalTime();
            lastUpdateTime = cycleStartTime;
            elapsedTime = 0.0f;

            PreProcessing();

            //Slight chance to go boom upon start...
            if (explodeUponCriticalFail)
                PerformAnalysis();
        }

        public override void StopResourceConverter()
        {
            if (!canBeShutdown)
                return;
            base.StopResourceConverter();
            progress = "None";
            timeRemainingDisplay = "N/A";

            if (!string.IsNullOrEmpty(runningEffect))
                this.part.Effect(stopEffect, 1.0f);
            if (!string.IsNullOrEmpty(runningEffect))
                this.part.Effect(runningEffect, 0.0f);
        }

        protected override ConversionRecipe PrepareRecipe(double deltatime)
        {
            ConversionRecipe recipe = new ConversionRecipe();
            if (!IsActivated)
                return recipe;

            try
            {
                ResourceRatio ratio;
                int count = inputList.Count;
                double adjustedDeltaTime = deltatime;

                for (int index = 0; index < count; index++)
                {
                    adjustedDeltaTime = deltatime;
                    if (inputList[index].ResourceName == "ElectricCharge")
                    {
                        ecPerSec = -inputList[index].Ratio;
                        if (deltatime >= kHighTimewarpDelta)
                            adjustedDeltaTime = kHighTimewarpDelta;
                        ratio = new ResourceRatio(inputList[index].ResourceName, inputList[index].Ratio * adjustedDeltaTime, inputList[index].DumpExcess);
                    }
                    else
                    {
                        ratio = new ResourceRatio(inputList[index].ResourceName, inputList[index].Ratio * inputEfficiency * adjustedDeltaTime, inputList[index].DumpExcess);
                    }
                    ratio.FlowMode = inputList[index].FlowMode;
                    if (ratio.FlowMode == ResourceFlowMode.NULL)
                        ratio.FlowMode = ResourceFlowMode.STAGE_PRIORITY_FLOW;
                    recipe.Inputs.Add(ratio);
                }

                count = outputList.Count;
                for (int index = 0; index < count; index++)
                {
                    ratio = new ResourceRatio(outputList[index].ResourceName, outputList[index].Ratio * outputEfficiency * deltatime, outputList[index].DumpExcess);
                    ratio.FlowMode = outputList[index].FlowMode;
                    if (ratio.FlowMode == ResourceFlowMode.NULL)
                        ratio.FlowMode = ResourceFlowMode.STAGE_PRIORITY_FLOW;
                    recipe.Outputs.Add(ratio);
                }

                recipe.Requirements.AddRange((IEnumerable<ResourceRatio>)reqList);
            }
            catch (Exception ex)
            {
                Debug.Log("[" + this.ClassName + "]" + "-  error when preparing recipe: " + ex);
            }

            return recipe;
        }

        protected override void PreProcessing()
        {
            base.PreProcessing();

            int specialistBonus = 0;
            float crewEfficiencyBonus = 1.0f;

            if (HighLogic.LoadedSceneIsFlight)
                specialistBonus = HasSpecialist(this.ExperienceEffect);

            if (specialistBonus > 0)
                crewEfficiencyBonus = 1.0f + (SpecialistBonusBase + (1.0f + (float)specialistBonus) * SpecialistEfficiencyFactor);

            //Update the inputEfficiency and outputEfficiency here.
            inputEfficiency = crewEfficiencyBonus;
            outputEfficiency = crewEfficiencyBonus;
        }

        public override void FixedUpdate()
        {
            double deltaTime = GetDeltaTime();
            lastUpdateTime = Planetarium.GetUniversalTime();

            //Update status
            UpdateConverterStatus();

            //Make sure we're activated if we're always active
            if (this.AlwaysActive)
                this.IsActivated = true;

            //Run the converter
            if (IsActivated)
            {
                ConverterResults results = new ConverterResults();
                ConversionRecipe recipe;
                ResourceRatio resourceRatio;
                int count;
                PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
                PartResourceDefinition resourceDef;
                double amount = 0;
                double maxAmount = 0;
                double amountObtained = 0;
                bool infinitePropellantsEnabled = CheatOptions.InfinitePropellant;
                bool infiniteElectricity = CheatOptions.InfiniteElectricity;

                //Do the pre-processing
                status = "A-OK";
                PreProcessing();
                recipe = PrepareRecipe(deltaTime);
                count = recipe.Inputs.Count;

                // Requires spashed
                if (requiresSplashed && !part.vessel.Splashed)
                {
                    status = "Vessel must be in water";
                    return;
                }

                if (requiresOxygen && !part.vessel.mainBody.atmosphereContainsOxygen)
                {
                    status = "Requires oxygenated atmosphere";
                    return;
                }

                //Handle required resources
                if (requiresHomeConnection && CommNet.CommNetScenario.CommNetEnabled && !this.part.vessel.connection.IsConnectedHome)
                {
                    status = "Requires home connection";
                    return;
                }

                if (minimumCrew > 0 && this.part.protoModuleCrew.Count < minimumCrew)
                {
                    status = "Needs more crew (" + this.part.protoModuleCrew.Count + "/" + minimumCrew + ")";
                    return;
                }

                //Make sure we have room for the outputs
                count = recipe.Outputs.Count;
                for (int index = 0; index < count; index++)
                {
                    resourceRatio = recipe.Outputs[index];

                    resourceDef = definitions[resourceRatio.ResourceName];
                    this.part.GetConnectedResourceTotals(resourceDef.id, out amount, out maxAmount, true);
                    if (amount >= maxAmount)
                    {
                        status = resourceDef.displayName + " is full";
                        if (AutoShutdown)
                            StopResourceConverter();
                        return;
                    }
                }

                //Make sure we have enough of the inputs
                count = recipe.Inputs.Count;
                for (int index = 0; index < count; index++)
                {
                    resourceRatio = recipe.Inputs[index];

                    //Skip resource if the appropriate cheat is on
                    if ((resourceRatio.ResourceName == "ElectricCharge" && infiniteElectricity) || (infinitePropellantsEnabled))
                        continue;

                    //Make sure we have enough of the resource
                    resourceDef = definitions[resourceRatio.ResourceName];
                    this.part.GetConnectedResourceTotals(resourceDef.id, out amount, out maxAmount, true);
                    if (amount < resourceRatio.Ratio)
                    {
                        status = "Missing " + resourceDef.displayName;
                        if (AutoShutdown)
                            StopResourceConverter();
                        return;
                    }

                    //Check for mininum EC
                    else if (resourceRatio.ResourceName == "ElectricCharge" && (amount / maxAmount) <= (minimumVesselPercentEC / 100.0f))
                    {
                        status = "Needs more " + resourceDef.displayName;
                        return;
                    }
                }

                //Now process the inputs.
                for (int index = 0; index < count; index++)
                {
                    resourceRatio = recipe.Inputs[index];
                    resourceDef = definitions[resourceRatio.ResourceName];

                    //Skip resource if the appropriate cheat is on
                    if ((resourceRatio.ResourceName == "ElectricCharge" && infiniteElectricity) || (infinitePropellantsEnabled))
                        continue;

                    this.part.RequestResource(resourceDef.id, resourceRatio.Ratio, resourceRatio.FlowMode);
                }

                //Process the outputs
                count = recipe.Outputs.Count;
                for (int index = 0; index < count; index++)
                {
                    resourceRatio = recipe.Outputs[index];
                    resourceDef = definitions[resourceRatio.ResourceName];

                    amountObtained = this.part.RequestResource(resourceDef.id, -resourceRatio.Ratio, resourceRatio.FlowMode);
                    if (amountObtained >= maxAmount)
                    {
                        status = resourceDef.displayName + " is full";
                    }
                }

                //Post process the results
                results.Status = status;
                results.TimeFactor = deltaTime;
                PostProcess(results, deltaTime);

                //Explosion check. Special case for when we have no yield resources.
                if (explodeUponCriticalFail && yieldsList.Count == 0)
                {
                    elapsedTime = Planetarium.GetUniversalTime() - cycleStartTime;
                    if (elapsedTime >= secondsPerCycle)
                    {
                        float completionRatio = (float)(elapsedTime / secondsPerCycle);
                        elapsedTime = 0;
                        cycleStartTime = Planetarium.GetUniversalTime();

                        if (completionRatio > 1.0f && !missingResources)
                        {
                            int cyclesSinceLastUpdate = Mathf.RoundToInt(completionRatio);
                            for (int currentCycle = 0; currentCycle < cyclesSinceLastUpdate; currentCycle++)
                            {
                                if (performAnalysisRoll() <= criticalFail)
                                {
                                    onCriticalFailure();
                                    return;
                                }
                            }
                        }
                    }
                }
            }

            //Cleanup
            CheckForShutdown();
            PostUpdateCleanup();
        }

        protected override void UpdateConverterStatus()
        {
            if (DirtyFlag == IsActivated)
                return;
            DirtyFlag = IsActivated;

            if (IsActivated)
                status = Localizer.Format("#autoLOC_257237");

            stopEvt.active = this.IsActivated;
            startEvt.active = !this.IsActivated;

            //if we can't shut down then hide the stop button.
            if (!canBeShutdown)
                stopEvt.active = false;

            MonoUtilities.RefreshContextWindows(this.part);
        }

        protected override void PostProcess(ConverterResults result, double deltaTime)
        {
            base.PostProcess(result, deltaTime);

            if (FlightGlobals.ready == false)
                return;
            if (HighLogic.LoadedSceneIsFlight == false)
                return;
            if (ModuleIsActive() == false)
                return;
            if (hoursPerCycle == 0f)
                return;
            if (yieldsList.Count == 0)
                return;
            if (!IsActivated)
                return;

            //Play the runningEffect
            if (!string.IsNullOrEmpty(runningEffect))
                this.part.Effect(runningEffect, 1.0f);

            //Check cycle start time
            if (cycleStartTime == 0f)
            {
                cycleStartTime = Planetarium.GetUniversalTime();
                lastUpdateTime = cycleStartTime;
                elapsedTime = 0.0f;
                return;
            }

            //If we're missing resources then we're done.
            if (!string.IsNullOrEmpty(result.Status))
            {
                if (result.Status.ToLower().Contains("missing"))
                {
                    status = result.Status;
                    missingResources = true;
                    return;
                }
            }

            //Calculate elapsed time
            elapsedTime = Planetarium.GetUniversalTime() - cycleStartTime;
            timeRemainingDisplay = formatTime(secondsPerCycle - elapsedTime, true);

            //Calculate progress
            CalculateProgress();

            //If we've elapsed time cycle then perform the analyis.
            float completionRatio = (float)(elapsedTime / secondsPerCycle);
            if (completionRatio > 1.0f && !missingResources)
            {
                int cyclesSinceLastUpdate = Mathf.RoundToInt(completionRatio);
                int currentCycle;
                for (currentCycle = 0; currentCycle < cyclesSinceLastUpdate; currentCycle++)
                {
                    PerformAnalysis();

                    //Reset start time
                    cycleStartTime = Planetarium.GetUniversalTime();
                }
            }

            //Update status
            if (yieldsList.Count > 0)
                status = "Progress: " + progress;
            else if (string.IsNullOrEmpty(status))
                status = "Running";
        }
        #endregion

        #region Yield Resources
        protected void loadYieldsList()
        {
            if (this.part.partInfo.partConfig == null)
                return;
            ConfigNode[] nodes = this.part.partInfo.partConfig.GetNodes("MODULE");
            ConfigNode converterNode = null;
            ConfigNode node = null;
            string moduleName;

            //Get the switcher config node.
            for (int index = 0; index < nodes.Length; index++)
            {
                node = nodes[index];
                if (node.HasValue("name"))
                {
                    moduleName = nodes[index].GetValue("name");
                    if (moduleName == this.ClassName)
                    {
                        converterNode = nodes[index];
                        if (converterNode.HasValue("ConverterName") && converterNode.GetValue("ConverterName") == ConverterName)
                            break;
                    }
                }
            }
            if (converterNode == null)
                return;

            //Get the nodes we're interested in
            nodes = converterNode.GetNodes("YIELD_RESOURCE");
            if (nodes.Length == 0)
                return;

            //Ok, start processing the yield resources
            yieldsList.Clear();
            ResourceRatio yieldResource;
            string resourceName;
            double amount;
            for (int index = 0; index < nodes.Length; index++)
            {
                node = nodes[index];
                if (!node.HasValue("ResourceName"))
                    continue;
                resourceName = node.GetValue("ResourceName");

                if (!node.HasValue("Ratio"))
                    continue;
                if (!double.TryParse(node.GetValue("Ratio"), out amount))
                    continue;

                yieldResource = new ResourceRatio(resourceName, amount, true);
                yieldResource.FlowMode = ResourceFlowMode.ALL_VESSEL;

                yieldsList.Add(yieldResource);
            }
        }

        /// <summary>
        /// Performs the analysis roll to determine how many yield resources to produce.
        /// The roll must meet or exceed the minimumSuccess required in order to produce a nominal
        /// yield (the amount specified in a YIELD_RESOURCE's Ratio entry). If the roll fails,
        /// then a lower than normal yield is produced. If the roll exceeds the criticalSuccess number,
        /// then a higher than normal yield is produced. If the roll falls below the criticalFailure number,
        /// then no yield is produced, and the part will explode if the explodeUponCriticalFailure flag is set.
        /// </summary>
        public virtual void PerformAnalysis()
        {
            //If we have no minimum success then just produce the yield resources.
            if (minimumSuccess <= 0.0f)
            {
                produceyieldsList(1.0);
                return;
            }

            //Ok, go through the analysis.
            int analysisRoll = performAnalysisRoll();

            if (analysisRoll <= criticalFail)
                onCriticalFailure();

            else if (analysisRoll >= criticalSuccess)
                onCriticalSuccess();

            else if (analysisRoll >= minimumSuccess)
                onSuccess();

            else
                onFailure();

        }

        protected virtual int performAnalysisRoll()
        {
            return UnityEngine.Random.Range(dieRollMin, dieRollMax);
        }

        protected virtual void onCriticalFailure()
        {
            lastAttempt = attemptCriticalFail;

            StopResourceConverter();

            //Show user message
            if (!string.IsNullOrEmpty(criticalFailMessage))
                ScreenMessages.PostScreenMessage(ConverterName + ": " + criticalFailMessage, kMessageDuration);

            //Explode if required.
            if (explodeUponCriticalFail)
            {
                //Now go boom.
                this.part.explode();
            }
        }

        protected virtual void onCriticalSuccess()
        {
            lastAttempt = attemptCriticalSuccess;
            produceyieldsList(criticalSuccessMultiplier);

            //Show user message
            if (!string.IsNullOrEmpty(criticalSuccessMessage))
                ScreenMessages.PostScreenMessage(ConverterName + ": " + criticalSuccessMessage, kMessageDuration);
        }

        protected virtual void onFailure()
        {
            lastAttempt = attemptFail;
            produceyieldsList(failureMultiplier);

            //Show user message
            if (!string.IsNullOrEmpty(failMessage))
                ScreenMessages.PostScreenMessage(ConverterName + ": " + failMessage, kMessageDuration);
        }

        protected virtual void onSuccess()
        {
            lastAttempt = attemptSuccess;
            produceyieldsList(1.0);

            //Show user message
            if (!string.IsNullOrEmpty(successMessage))
                ScreenMessages.PostScreenMessage(successMessage, kMessageDuration);
        }

        protected virtual void produceyieldsList(double yieldMultiplier)
        {
            int count = yieldsList.Count;
            ResourceRatio resourceRatio;
            double yieldAmount = 0;
            string resourceName;
            double highestSkill = 0;

            //Find highest skill bonus
            if (UseSpecialistBonus && !string.IsNullOrEmpty(ExperienceEffect))
            {
                List<ProtoCrewMember> crewMembers = this.part.vessel.GetVesselCrew();

                int crewCount = crewMembers.Count;
                for (int index = 0; index < crewCount; index++)
                {
                    if (crewMembers[index].HasEffect(ExperienceEffect))
                    {
                        if (crewMembers[index].experienceLevel > highestSkill)
                            highestSkill = crewMembers[index].experienceTrait.CrewMemberExperienceLevel();
                    }
                }
            }

            //Produce the yield resources
            for (int index = 0; index < count; index++)
            {
                yieldAmount = 0;
                resourceRatio = yieldsList[index];

                resourceName = resourceRatio.ResourceName;
                yieldAmount = resourceRatio.Ratio * (1.0 + (highestSkill * SpecialistEfficiencyFactor)) * yieldMultiplier;

                this.part.RequestResource(resourceName, -yieldAmount, resourceRatio.FlowMode);
            }
        }

        /// <summary>
        /// Calculates and updates the progress of the yield production cycle.
        /// </summary>
        public virtual void CalculateProgress()
        {
            //Get elapsed time (seconds)
            progress = string.Format("{0:f1}%", ((elapsedTime / secondsPerCycle) * 100));
        }
        #endregion

        #region Helpers

        private static double secondsPerDay = 0;
        private static double secondsPerYear = 0;

        /// <summary>
        /// Formats the supplied seconds into a string.
        /// </summary>
        /// <param name="secondsToFormat">The number of seconds to format.</param>
        /// <param name="showCompact">A flag to indicate whether or not to show the compact form.</param>
        /// <returns></returns>
        private static string formatTime(double secondsToFormat, bool showCompact = false)
        {
            StringBuilder timeBuilder = new StringBuilder();
            double seconds = secondsToFormat;
            double years = 0;
            double days = 0;
            double hours = 0;
            double minutes = 0;

            //Make sure we have calculated our seconds per day
            GetSecondsPerDay();

            //Years
            years = Math.Floor(seconds / secondsPerYear);
            if (years >= 1.0)
            {
                seconds -= years * secondsPerYear;
            }
            else
            {
                years = 0;
            }

            //Days
            days = Math.Floor(seconds / secondsPerDay);
            if (days >= 1.0)
            {
                seconds -= days * secondsPerDay;
            }
            else
            {
                days = 0;
            }

            //Hours
            hours = Math.Floor(seconds / 3600);
            if (hours >= 1.0)
            {
                seconds -= hours * 3600;
            }
            else
            {
                seconds = 0;
            }

            //Minutes
            minutes = Math.Floor(seconds / 60);
            if (minutes >= 1.0)
            {
                seconds -= minutes * 60;
            }
            else
            {
                minutes = 0;
            }

            if (showCompact)
            {
                if (years > 0)
                    timeBuilder.Append(string.Format("Y{0:n2}:", years));
                if (days > 0)
                    timeBuilder.Append(string.Format("D{0:n2}:", days));
                if (hours > 0)
                    timeBuilder.Append(string.Format("H{0:n2}:", hours));
                if (minutes > 0)
                    timeBuilder.Append(string.Format("M{0:n2}:", minutes));
                if (seconds > 0.0001)
                    timeBuilder.Append(string.Format("S{0:n2}", seconds));
            }
            else
            {
                if (years > 0)
                    timeBuilder.Append(string.Format(" {0:n2} Years,", years));
                if (days > 0)
                    timeBuilder.Append(string.Format(" {0:n2} Days,", days));
                if (hours > 0)
                    timeBuilder.Append(string.Format(" {0:n2} Hours,", hours));
                if (minutes > 0)
                    timeBuilder.Append(string.Format(" {0:n2} Minutes,", minutes));
                if (seconds > 0.0001)
                    timeBuilder.Append(string.Format(" {0:n2} Seconds", seconds));
            }

            string timeDisplay = timeBuilder.ToString();
            char[] trimChars = { ',' };
            timeDisplay = timeDisplay.TrimEnd(trimChars);
            return timeDisplay;
        }

        /// <summary>
        /// Gets the number of seconds per day on the homeworld.
        /// </summary>
        /// <returns>The lenght of the solar day in seconds of the homeworld.</returns>
        private static double GetSecondsPerDay()
        {
            if (secondsPerDay > 0)
                return secondsPerDay;

            //Find homeworld
            int count = FlightGlobals.Bodies.Count;
            CelestialBody body = null;
            for (int index = 0; index < count; index++)
            {
                body = FlightGlobals.Bodies[index];
                if (body.isHomeWorld)
                    break;
                else
                    body = null;
            }
            if (body == null)
            {
                secondsPerYear = 21600 * 426.08;
                secondsPerDay = 21600;
                return secondsPerDay;
            }

            //Also get seconds per year
            secondsPerYear = body.orbit.period;

            //Return solar day length
            secondsPerDay = body.solarDayLength;
            return secondsPerDay;
        }
        #endregion
    }
}
