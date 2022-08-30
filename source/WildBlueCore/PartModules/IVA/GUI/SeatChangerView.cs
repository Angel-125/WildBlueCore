using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;
using KSP.IO;
using KSP.Localization;
using KSP.UI.Screens.Flight;

namespace WildBlueCore.PartModules.IVA
{
    public class SeatChangerView : Dialog<SeatChangerView>
    {
        #region Fields
        public Part part;
        public Dictionary<string, string> seatAliases;
        #endregion

        #region Housekeeping
        Vector2 scrollPosCurrentSeating;
        Vector2 scrollPosNewSeating;
        int selectedCurrentSeatIndex;
        int selectedNewSeatIndex;
        List<InternalSeat> occupiedSeats;
        List<InternalSeat> emptySeats;
        bool respawingCrew = false;
        double respawningEndTime;

        string currentSeatLabel;
        string newSeatLabel;
        string changeSeatButton;
        string respawningLabel;
        #endregion

        #region Constructors
        public SeatChangerView() :
        base(Localizer.Format("#LOC_WILDBLUECORE_reassignSeatsViewTitle"), 400, 400)
        {
            Resizable = false;
            scrollPosCurrentSeating = Vector2.zero;
            scrollPosNewSeating = Vector2.zero;
        }
        #endregion

        #region Overrides
        public override void SetVisible(bool newValue)
        {
            base.SetVisible(newValue);

            currentSeatLabel = Localizer.Format("#LOC_WILDBLUECORE_currentSeatLabel");
            newSeatLabel = Localizer.Format("#LOC_WILDBLUECORE_newSeatLabel");
            changeSeatButton = Localizer.Format("#LOC_WILDBLUECORE_changeSeatButton");
            respawningLabel = Localizer.Format("#LOC_WILDBLUECORE_respawningCrewLabel");
            fetchSeats();
        }

        protected override void DrawWindowContents(int windowId)
        {
            GUILayout.BeginVertical();
            if (respawingCrew)
            {
                GUILayout.Label(respawningLabel);
                GUILayout.EndVertical();
                if (Planetarium.GetUniversalTime() >= respawningEndTime)
                    respawnCrew();
                return;
            }

            GUILayout.BeginHorizontal();

            // CURRENT seating
            GUILayout.BeginVertical();

            GUILayout.Label(currentSeatLabel);

            scrollPosCurrentSeating = GUILayout.BeginScrollView(scrollPosCurrentSeating, new GUILayoutOption[] { GUILayout.Width(250) });
            int count = occupiedSeats.Count;
            bool isSelected = false;
            for (int index = 0; index < count; index++)
            {
                isSelected = selectedCurrentSeatIndex == index;
                isSelected = GUILayout.Toggle(isSelected, occupiedSeats[index].crew.displayName);
                if (isSelected)
                    selectedCurrentSeatIndex = index;
            }

            GUILayout.EndScrollView();

            GUILayout.EndVertical();

            // NEW seating
            GUILayout.BeginVertical();

            GUILayout.Label(newSeatLabel);

            scrollPosNewSeating = GUILayout.BeginScrollView(scrollPosNewSeating, new GUILayoutOption[] { GUILayout.Width(250) });
            count = emptySeats.Count;
            string seatName;
            for (int index = 0; index < count; index++)
            {
                seatName = emptySeats[index].seatTransformName;
                if (seatAliases.ContainsKey(seatName))
                    seatName = seatAliases[seatName];
                isSelected = selectedNewSeatIndex == index;
                isSelected = GUILayout.Toggle(isSelected, seatName);
                if (isSelected)
                    selectedNewSeatIndex = index;
            }

            GUILayout.EndScrollView();

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            // Change seat button
            if (GUILayout.Button(changeSeatButton))
            {
                ProtoCrewMember astronaut = occupiedSeats[selectedCurrentSeatIndex].crew;
                int seatIndex = findSeatIndex(emptySeats[selectedNewSeatIndex]);

                part.RemoveCrewmember(astronaut);

                part.AddCrewmemberAt(astronaut, seatIndex);

                Vessel.CrewWasModified(part.vessel);
                FlightGlobals.ActiveVessel.DespawnCrew();

                respawingCrew = true;
                respawningEndTime = Planetarium.GetUniversalTime() + 0.5f;
            }

            GUILayout.EndVertical();
        }
        #endregion

        #region Helpers
        protected void respawnCrew()
        {
            FlightGlobals.ActiveVessel.SpawnCrew();
            CameraManager.ICameras_ResetAll();
            GameEvents.onVesselChange.Fire(FlightGlobals.ActiveVessel);
            part.vessel.CrewListSetDirty();
            respawingCrew = false;
            fetchSeats();
            ModuleIVAVariants ivaVariants = part.FindModuleImplementing<ModuleIVAVariants>();
            if (ivaVariants != null)
                ivaVariants.applyVariant();
            ModuleSeatChanger.onSeatsReassigned.Fire(part.FindModuleImplementing<ModuleSeatChanger>());
        }

        int findSeatIndex(InternalSeat seat)
        {
            List<InternalSeat> seats = part.internalModel.seats;
            int count = seats.Count;
            for (int index = 0; index < count; index++)
            {
                if (seats[index] == seat)
                    return index;
            }

            return -1;
        }

        void fetchSeats()
        {
            occupiedSeats = new List<InternalSeat>();
            emptySeats = new List<InternalSeat>();

            List<InternalSeat> seats = part.internalModel.seats;
            int count = seats.Count;
            for (int index = 0; index < count; index++)
            {
                if (seats[index].taken)
                    occupiedSeats.Add(seats[index]);
                else
                    emptySeats.Add(seats[index]);
            }

            selectedCurrentSeatIndex = 0;
            selectedNewSeatIndex = 0;
        }
        #endregion
    }
}
