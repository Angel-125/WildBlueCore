using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KSP.Localization;
using UnityEngine;
using KSP.UI;
using KSP.UI.Screens;

namespace WildBlueCore.KerbalGear
{
    /// <summary>
    /// This part module allows kerbals to change their outfits after the vessel leaves the VAB/SPH.
    /// </summary>
    /// <example>
    /// <code>
    /// MODULE
    /// {
    ///     name = WBIModuleSuitSwitcher
    /// }
    /// </code>
    /// </example>
    [KSPModule("#LOC_WILDBLUECORE_suitSwitcherTitle")]
    public class WBIModuleSuitSwitcher : WBIBasePartModule

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

            // Ideally we'd use the HelmetSuitPickerWindow to show the stock suit switcher, but that is closely tied to the editor/astronaut complex assets and they're not available during flight.
            //HelmetSuitPickerWindow pickerWindow = HelmetSuitPickerWindow();
        }
        #endregion

        #region Overrides
        public void OnDestroy()
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
