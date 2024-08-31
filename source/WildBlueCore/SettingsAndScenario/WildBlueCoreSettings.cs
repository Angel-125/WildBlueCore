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
    public class WildBlueCoreSettings : GameParameters.CustomParameterNode
    {
        [GameParameters.CustomParameterUI("Debug Mode", toolTip = "", autoPersistance = true, gameMode = GameParameters.GameMode.ANY)]
        public bool debugMode = false;

        #region Overrides
        public override string DisplaySection
        {
            get
            {
                return Section;
            }
        }

        public override string Section
        {
            get
            {
                return "WildBlueCore";
            }
        }

        public override string Title
        {
            get
            {
                return "Wild Blue Core";
            }
        }

        public override int SectionOrder
        {
            get
            {
                return 1;
            }
        }

        public override GameParameters.GameMode GameMode
        {
            get
            {
                return GameParameters.GameMode.ANY;
            }
        }

        public override bool HasPresets
        {
            get
            {
                return false;
            }
        }
        #endregion

        public static bool DebugModeEnabled
        {
            get
            {
                WildBlueCoreSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<WildBlueCoreSettings>();
                return settings.debugMode;
            }
        }

        public static bool StartEventRequiresSkillCheck
        {
            get
            {
                WildBlueCoreAnimatedSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<WildBlueCoreAnimatedSettings>();
                return settings.startEventRequiresSkillCheck;
            }
        }

        public static bool EndEventRequiresSkillCheck
        {
            get
            {
                WildBlueCoreAnimatedSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<WildBlueCoreAnimatedSettings>();
                return settings.endEventRequiresSkillCheck;
            }
        }

        public static bool StartEventRequiresResources
        {
            get
            {
                WildBlueCoreAnimatedSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<WildBlueCoreAnimatedSettings>();
                return settings.startEventRequiresResources;
            }
        }

        public static bool EndEventGivesResources
        {
            get
            {
                WildBlueCoreAnimatedSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<WildBlueCoreAnimatedSettings>();
                return settings.endEventGivesResources;
            }
        }
    }

    public class WildBlueCoreAnimatedSettings : GameParameters.CustomParameterNode
    {
        [GameParameters.CustomParameterUI("#LOC_WILDBLUECORE_AnimStartEventSkillCheckDesc", toolTip = "#LOC_WILDBLUECORE_AnimStartEventSkillCheckTip", autoPersistance = true)]
        public bool startEventRequiresSkillCheck = true;

        [GameParameters.CustomParameterUI("#LOC_WILDBLUECORE_AnimEndEventSkillCheckDesc", toolTip = "#LOC_WILDBLUECORE_AnimEndEventSkillCheckTip", autoPersistance = true)]
        public bool endEventRequiresSkillCheck = true;

        [GameParameters.CustomParameterUI("#LOC_WILDBLUECORE_AnimStartEventResourcesDesc", toolTip = "#LOC_WILDBLUECORE_AnimStartEventResourcesTip", autoPersistance = true)]
        public bool startEventRequiresResources = true;

        [GameParameters.CustomParameterUI("#LOC_WILDBLUECORE_AnimEndEventResourcesDesc", toolTip = "#LOC_WILDBLUECORE_AnimEndEventResourcesTip", autoPersistance = true)]
        public bool endEventGivesResources = true;

        #region Overrides
        public override string DisplaySection
        {
            get
            {
                return Section;
            }
        }

        public override string Section
        {
            get
            {
                return "WildBlueCore";
            }
        }

        public override string Title
        {
            get
            {
                return "Animate Generic Extended";
            }
        }

        public override int SectionOrder
        {
            get
            {
                return 2;
            }
        }

        public override GameParameters.GameMode GameMode
        {
            get
            {
                return GameParameters.GameMode.ANY;
            }
        }

        public override bool HasPresets
        {
            get
            {
                return false;
            }
        }
        #endregion

    }
}
