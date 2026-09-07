using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KSP.Localization;

namespace WildBlueCore.KerbalGear
{
    #region delegates
    public delegate Transform GetAttachTransformDelegate(BodyLocations bodyLocation);
    #endregion

    internal class WBIPropOffsetGUI: Dialog<WBIPropOffsetGUI>
    {
        public Dictionary<string, List<SWearableProp>> wearablePartProps;
        public GetAttachTransformDelegate getAttachTransform;
        public KerbalEVA kerbalEVA;

        #region Housekeeping
        float offsetX;
        float offsetY;
        float offsetZ;
        float offsetRoll;
        float offsetPitch;
        float offsetYaw;

        float offsetXDelta;
        float offsetYDelta;
        float offsetZDelta;
        float offsetRollDelta;
        float offsetPitchDelta;
        float offsetYawDelta;

        string offsetXString;
        string offsetYString;
        string offsetZString;
        string offsetRollString;
        string offsetPitchString;
        string offsetYawString;

        Vector3 positionOffset = Vector3.zero;
        Vector3 rotationOffset = Vector3.zero;

        int buttonGroupIndex;
        string[] buttonTexts = new string[] { "0.1", "0.01", "0.001", "0.0001", "1", "5" };
        float[] deltaOffsets = new float[] { 0.1f, 0.01f, 0.001f, 0.0001f, 1f, 5f };
        GUILayoutOption[] propPanelOptions = new GUILayoutOption[] { GUILayout.Width(165) };
        Vector2 scrollPos;

        string selectedPropName = string.Empty;
        GameObject selectedProp = null;
        SWearableProp wearableProp;
        string[] partPropNames = null;
        bool disableIdleAnimationsEnabled = true;
        bool idleAnimationsDisabled;
        bool previousAlternateIdleDisabled;
        #endregion

        #region Constructors
        public WBIPropOffsetGUI() :
        base("Prop Offsets", 635, 400)
        {
            WindowTitle = Localizer.Format("#LOC_WILDBLUECORE_propOffsetTitle");
            Resizable = false;
        }
        #endregion

        public override void SetVisible(bool newValue)
        {
            if (newValue)
            {
                if (!refreshCarriedProps())
                {
                    SetVisible(false);
                    return;
                }

                if (disableIdleAnimationsEnabled)
                    disableIdleAnimations();
            }
            else
                restoreIdleAnimations();

            base.SetVisible(newValue);
        }

        /// <summary>
        /// Refreshes the prop selector after the EVA inventory changes. If the selected prop is no
        /// longer carried, another carried prop is selected. The window closes when no wearable
        /// items remain in the inventory.
        /// </summary>
        public void RefreshCarriedProps()
        {
            if (!refreshCarriedProps() && IsVisible())
                SetVisible(false);
        }

        protected override void DrawWindowContents(int windowId)
        {
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();

            // Current prop
            GUILayout.Label(selectedPropName);

            // Selectable props to edit
            scrollPos = GUILayout.BeginScrollView(scrollPos, propPanelOptions);

            List<SWearableProp> wearableProps;
            int count;
            SWearableProp partProp;
            for (int index = 0; index < partPropNames.Length; index++)
            {
                GUILayout.Label(string.Format("<color=white>{0:s}</color>", partPropNames[index]));

                wearableProps = wearablePartProps[partPropNames[index]];
                count = wearableProps.Count;
                for (int propIndex = 0; propIndex < count; propIndex++)
                {
                    partProp = wearableProps[propIndex];
                    if (GUILayout.Button(partProp.name))
                        selectProp(partProp);
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.BeginVertical();

            // Idle animation toggle
            bool newDisableIdleAnimationsEnabled = GUILayout.Toggle(
                disableIdleAnimationsEnabled,
                Localizer.Format("#LOC_WILDBLUECORE_disableIdleAnimations"));
            if (newDisableIdleAnimationsEnabled != disableIdleAnimationsEnabled)
            {
                disableIdleAnimationsEnabled = newDisableIdleAnimationsEnabled;
                if (disableIdleAnimationsEnabled)
                    disableIdleAnimations();
                else
                    restoreIdleAnimations();
            }

            // Delta buttons
            buttonGroupIndex = GUILayout.SelectionGrid(buttonGroupIndex, buttonTexts, buttonTexts.Length);

            // Offsets
            bool updateNeeded = false;
            offsetXString = drawOffsetControls("#LOC_WILDBLUECORE_OffsetX", offsetXString, ref offsetXDelta, ref updateNeeded);
            offsetYString = drawOffsetControls("#LOC_WILDBLUECORE_OffsetY", offsetYString, ref offsetYDelta, ref updateNeeded);
            offsetZString = drawOffsetControls("#LOC_WILDBLUECORE_OffsetZ", offsetZString, ref offsetZDelta, ref updateNeeded);
            offsetRollString = drawOffsetControls("#LOC_WILDBLUECORE_OffsetRoll", offsetRollString, ref offsetRollDelta, ref updateNeeded);
            offsetPitchString = drawOffsetControls("#LOC_WILDBLUECORE_OffsetPitch", offsetPitchString, ref offsetPitchDelta, ref updateNeeded);
            offsetYawString = drawOffsetControls("#LOC_WILDBLUECORE_OffsetYaw", offsetYawString, ref offsetYawDelta, ref updateNeeded);

            // Update position offset
            positionOffset.x = offsetXDelta;
            positionOffset.y = offsetYDelta;
            positionOffset.z = offsetZDelta;

            // Update rotation offset
            rotationOffset.x = offsetRollDelta;
            rotationOffset.y = offsetPitchDelta;
            rotationOffset.z = offsetYawDelta;

            // Update the meshTransform's position and rotation.
            if (updateNeeded)
            {
                wearableProp.meshTransform.localPosition = positionOffset;
                wearableProp.meshTransform.localEulerAngles = rotationOffset;
            }

            // Copy offsets to clipboard button
            if (GUILayout.Button(Localizer.Format("#LOC_WILDBLUECORE_copyOffsetsButton")))
            {
                StringBuilder outputString = new StringBuilder();
                outputString.AppendLine(string.Format("positionOffset = {0:n4}, {1:n4}, {2:n4}", offsetXDelta, offsetYDelta, offsetZDelta));
                outputString.AppendLine(string.Format("rotationOffset = {0:n4}, {1:n4}, {2:n4}", offsetRollDelta, offsetPitchDelta, offsetYawDelta));
                string offsetString = outputString.ToString();

                if (kerbalEVA.ModuleInventoryPartReference.ContainsPart(WBIModuleWearablesController.kJetpackPartName))
                    offsetString = offsetString.Replace("positionOffset", "positionOffsetJetpack");

                GUIUtility.systemCopyBuffer = offsetString;
            }

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private string drawOffsetControls(string offsetLabel, string offsetText, ref float offsetDelta, ref bool updateNeeded)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format(offsetLabel));
            string offsetValue = GUILayout.TextField(offsetText);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<"))
            {
                offsetDelta -= deltaOffsets[buttonGroupIndex];
                offsetValue = offsetDelta.ToString();
                updateNeeded = true;
            }
            if (GUILayout.Button(">"))
            {
                offsetDelta += deltaOffsets[buttonGroupIndex];
                offsetValue = offsetDelta.ToString();
                updateNeeded = true;
            }
            GUILayout.EndHorizontal();

            float value = 0;
            if (offsetValue != offsetText && float.TryParse(offsetValue, out value))
            {
                offsetDelta = value;
                updateNeeded = true;
            }

            return offsetValue;
        }

        /// <summary>
        /// Rebuilds the selectable part list from wearable items in the live EVA inventory.
        /// </summary>
        /// <returns>True when at least one carried wearable prop is available.</returns>
        private bool refreshCarriedProps()
        {
            ModuleInventoryPart inventory = kerbalEVA != null
                ? kerbalEVA.ModuleInventoryPartReference
                : null;
            if (wearablePartProps == null || inventory == null)
            {
                partPropNames = new string[0];
                return false;
            }

            partPropNames = wearablePartProps
                .Where(entry => entry.Value != null && entry.Value.Count > 0 &&
                    inventory.ContainsPart(entry.Key))
                .Select(entry => entry.Key)
                .ToArray();
            if (partPropNames.Length == 0)
            {
                selectedPropName = string.Empty;
                selectedProp = null;
                return false;
            }

            bool selectedPartIsCarried = selectedProp != null &&
                partPropNames.Contains(wearableProp.partName);
            if (!selectedPartIsCarried)
            {
                List<SWearableProp> wearableProps = wearablePartProps[partPropNames[0]];
                selectProp(wearableProps[0]);
            }

            return true;
        }

        /// <summary>
        /// Selects a wearable prop and loads its configured offsets into the controls.
        /// </summary>
        private void selectProp(SWearableProp newWearableProp)
        {
            wearableProp = newWearableProp;
            selectedPropName = wearableProp.name;
            selectedProp = wearableProp.prop;
            setInitialOffsets();
        }

        /// <summary>
        /// Prevents stock KerbalEVA from starting alternate grounded idle animations while offsets
        /// are being adjusted. If one is already playing, return to the neutral idle immediately.
        /// </summary>
        private void disableIdleAnimations()
        {
            if (kerbalEVA == null || idleAnimationsDisabled)
                return;

            previousAlternateIdleDisabled = kerbalEVA.alternateIdleDisabled;
            kerbalEVA.alternateIdleDisabled = true;
            idleAnimationsDisabled = true;

            if (kerbalEVA.fsm != null && kerbalEVA.fsm.Started &&
                kerbalEVA.fsm.CurrentState == kerbalEVA.st_idle_b_gr)
            {
                kerbalEVA.fsm.RunEvent(kerbalEVA.On_return_idle);
            }
        }

        /// <summary>
        /// Restores the alternate-idle setting that was active before the window opened.
        /// </summary>
        private void restoreIdleAnimations()
        {
            if (!idleAnimationsDisabled)
                return;

            if (kerbalEVA != null)
                kerbalEVA.alternateIdleDisabled = previousAlternateIdleDisabled;

            idleAnimationsDisabled = false;
        }

        private void setInitialOffsets()
        {
            if (kerbalEVA.ModuleInventoryPartReference.ContainsPart(WBIModuleWearablesController.kJetpackPartName) && wearableProp.bodyLocation == BodyLocations.backOrJetpack)
            {
                offsetXString = wearableProp.positionOffsetJetpack.x.ToString();
                offsetYString = wearableProp.positionOffsetJetpack.y.ToString();
                offsetZString = wearableProp.positionOffsetJetpack.z.ToString();

                offsetXDelta = wearableProp.positionOffsetJetpack.x;
                offsetYDelta = wearableProp.positionOffsetJetpack.y;
                offsetZDelta = wearableProp.positionOffsetJetpack.z;
            }
            else
            {
                offsetXString = wearableProp.positionOffset.x.ToString();
                offsetYString = wearableProp.positionOffset.y.ToString();
                offsetZString = wearableProp.positionOffset.z.ToString();

                offsetXDelta = wearableProp.positionOffset.x;
                offsetYDelta = wearableProp.positionOffset.y;
                offsetZDelta = wearableProp.positionOffset.z;
            }

            offsetRollString = wearableProp.rotationOffset.x.ToString();
            offsetPitchString = wearableProp.rotationOffset.y.ToString();
            offsetYawString = wearableProp.rotationOffset.z.ToString();

            offsetRollDelta = wearableProp.rotationOffset.x;
            offsetPitchDelta = wearableProp.rotationOffset.y;
            offsetYawDelta = wearableProp.rotationOffset.z;
        }
    }
}
