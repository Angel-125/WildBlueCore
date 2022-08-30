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
        Dictionary<string, string> seatAliases;
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            fetchSeatAliases();
            seatChangerView = new SeatChangerView();
            seatChangerView.part = part;
            seatChangerView.seatAliases = seatAliases;
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
        void fetchSeatAliases()
        {
            seatAliases = new Dictionary<string, string>();

            ConfigNode node = getPartConfigNode();
            if (node == null || !node.HasNode("SEAT_ALIAS"))
                return;
            ConfigNode[] nodes = node.GetNodes("SEAT_ALIAS");
            string seatName;
            string displayName;
            for (int index = 0; index < nodes.Length; index++)
            {
                node = nodes[index];
                if (!node.HasValue("name") || !node.HasValue("displayName"))
                    continue;

                seatName = node.GetValue("name");
                if (string.IsNullOrEmpty(seatName))
                    continue;

                displayName = node.GetValue("displayName");
                if (string.IsNullOrEmpty("displayName"))
                    continue;

                if (!seatAliases.ContainsKey(seatName))
                    seatAliases.Add(seatName, displayName);
            }
        }
        #endregion
    }
}
