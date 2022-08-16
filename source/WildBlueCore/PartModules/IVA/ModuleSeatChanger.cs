using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KSP.Localization;

namespace WildBlueCore.PartModules.IVA
{
    public class ModuleSeatChanger: BasePartModule
    {
        #region Events
        public static EventData<ModuleSeatChanger> onSeatsReassigned = new EventData<ModuleSeatChanger>("onSeatsReassigned");
        #endregion

        #region Fields
        #endregion

        #region Housekeeping
        SeatChangerView seatChangerView;
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            seatChangerView = new SeatChangerView();
            seatChangerView.part = part;
        }
        #endregion

        #region Events
        [KSPEvent(guiActive = true, guiName = "#LOC_WILDBLUECORE_reassignSeatsEventName")]
        public void ShowReassignSeatsGUI()
        {
            if (part.protoModuleCrew.Count == 0)
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_WILDBLUECORE_allSeatsEmpty"), 5f, ScreenMessageStyle.UPPER_CENTER);
            else if (part.protoModuleCrew.Count == part.CrewCapacity)
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_WILDBLUECORE_allSeatsOccupied"), 5f, ScreenMessageStyle.UPPER_CENTER);
            else
                seatChangerView.SetVisible(true);
        }
        #endregion

        #region Helpers
        #endregion
    }
}
