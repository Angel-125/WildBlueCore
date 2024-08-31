using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KSP.IO;
using KSP.Localization;

namespace WildBlueCore
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.EDITOR, GameScenes.FLIGHT)]
    public class WildBlueCoreScenario : ScenarioModule
    {
        #region Static Fields
        public static WildBlueCoreScenario shared;
        public static bool debugMode;
        public static bool startEventRequiresSkillCheck;
        public static bool endEventRequiresSkillCheck;
        public static bool startEventRequiresResources;
        public static bool endEventGivesResources;
        #endregion

        #region Overrides
        public override void OnAwake()
        {
            base.OnAwake();
            shared = this;
            GameEvents.OnGameSettingsApplied.Add(onGameSettingsApplied);
            onGameSettingsApplied();
        }
        #endregion

        #region Helpers
        private void onGameSettingsApplied()
        {
            debugMode = WildBlueCoreSettings.DebugModeEnabled;
            startEventRequiresSkillCheck = WildBlueCoreSettings.StartEventRequiresSkillCheck;
            endEventRequiresSkillCheck = WildBlueCoreSettings.EndEventRequiresSkillCheck;
            startEventRequiresResources = WildBlueCoreSettings.StartEventRequiresResources;
            endEventGivesResources = WildBlueCoreSettings.EndEventGivesResources;
        }
        #endregion
    }
}
