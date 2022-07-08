using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KSP.Localization;
using UnityEngine;
using KSP.UI;

namespace WildBlueCore.KerbalGear
{

    /// <summary>
    /// This part module allows kerbals to change their outfits after the vessel leaves the VAB/SPH.
    /// </summary>
    /// <example>
    /// <code>
    /// MODULE
    /// {
    ///     name = ModuleSuitSwitcher
    /// }
    /// </code>
    /// </example>
    [KSPModule("#LOC_WILDBLUECORE_suitSwitcherTitle")]
    public class ModuleSuitSwitcher : BasePartModule

    {
        #region Constants
        #endregion

        #region Fields
        #endregion

        #region Housekeeping
        WBIWardrobeGUI wardrobeView = null;
        #endregion

        #region Events
        /// <summary>
        /// Opens the wardrobe GUI.
        /// </summary>
        [KSPEvent(guiActive = true, guiName = "#LOC_WILDBLUECORE_suitSwitcherOpenGUI")]
        public void OpenWardrobe()
        {
            if (part.protoModuleCrew.Count == 0)
            {
                ScreenMessages.PostScreenMessage("#LOC_WILDBLUECORE_noCrewForWardrobe", 3.0f, ScreenMessageStyle.UPPER_CENTER);
                return;
            }

            wardrobeView.SetVisible(true);

//            Events.Add(new BaseEvent(Events, "Test1", testEventMethod, null));
            /*
            ProtoCrewMember crew = part.protoModuleCrew[0];
            CrewListItem crewListItem = UnityEngine.Object.Instantiate<CrewListItem>(null);
            crewListItem.SetName(crew.name);
            crewListItem.SetButton(CrewListItem.ButtonTypes.V);
            crewListItem.SetStats(crew);
            crewListItem.SetXP(crew);
            crewListItem.SetCrewRef(crew);
            crewListItem.SetKerbalAsApplicableType(crew);
            crewListItem.SetTooltip(crew);
            SuitCombos component1 = GameDatabase.Instance.GetComponent<SuitCombos>();
            if (crew.name == component1.helmetSuitPickerWindow.crew.name)
                component1.helmetSuitPickerWindow.SetupSuitTypeButtons(crewListItem, crew, component1.helmetSuitPickerWindow.kerbalType);
            */
        }
        #endregion

        #region Overrides
        public void Destroy()
        {
            if (wardrobeView.IsVisible())
                wardrobeView.SetVisible(false);

            GameEvents.onVesselChange.Remove(onVesselChange);
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            if (!HighLogic.LoadedSceneIsFlight)
                return;

            wardrobeView = new WBIWardrobeGUI();
            wardrobeView.part = part;

            // Watch for game events
            GameEvents.onVesselChange.Add(onVesselChange);
        }

        public override void OnInactive()
        {
            base.OnInactive();
            if (wardrobeView.IsVisible())
                wardrobeView.SetVisible(false);
        }

        public override string GetModuleDisplayName()
        {
            return Localizer.Format("#LOC_WILDBLUECORE_suitSwitcherTitle");
        }

        public override string GetInfo()
        {
            return Localizer.Format("#LOC_WILDBLUECORE_suitSwitcherInfo");
        }
        #endregion

        #region Helpers
        private void onVesselChange(Vessel newVessel)
        {
            if (wardrobeView.IsVisible())
                wardrobeView.SetVisible(false);
        }
        #endregion
    }
}
