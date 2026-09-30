using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using KSP.Localization;
using UnityEngine;

namespace WildBlueCore.PartModules.Coupling
{
    /// <summary>
    /// Adds persistent PAW toggles that enable or disable each WBIModuleDockingNode on the part.
    /// </summary>
    public class WBIModuleDockingNodeController : PartModule
    {
        private const string kDockingNodeStateNode = "DOCKING_NODE_STATE";
        private const string kDisplayNameValue = "displayName";
        private const string kIsEnabledValue = "isEnabled";
        private const string kFieldNamePrefix = "dockingNodeEnabled";
        private const string kGroupName = "Docking Ports";
        private const string kGroupDisplayName = "#LOC_WILDBLUECORE_dockingPorts";
        private const string kEnabledText = "#LOC_WILDBLUECORE_dockingPortEnabled";
        private const string kDisabledText = "#LOC_WILDBLUECORE_dockingPortDisabled";

        private static readonly FieldInfo enabledField = typeof(DockingNodeToggle).GetField(nameof(DockingNodeToggle.isEnabled));
        private static readonly PropertyInfo baseFieldName = typeof(BaseField).GetProperty(nameof(BaseField.name));

        private readonly Dictionary<string, bool> dockingNodeStates = new Dictionary<string, bool>();
        private readonly List<DockingNodeToggle> dockingNodeToggles = new List<DockingNodeToggle>();

        private sealed class DockingNodeToggle
        {
            public bool isEnabled;
            public WBIModuleDockingNode dockingNode;
        }

        /// <inheritdoc />
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            dockingNodeStates.Clear();
            ConfigNode[] stateNodes = node.GetNodes(kDockingNodeStateNode);
            for (int index = 0; index < stateNodes.Length; index++)
            {
                ConfigNode stateNode = stateNodes[index];
                if (!stateNode.HasValue(kDisplayNameValue) || !stateNode.HasValue(kIsEnabledValue))
                    continue;

                bool isEnabled;
                if (bool.TryParse(stateNode.GetValue(kIsEnabledValue), out isEnabled))
                    dockingNodeStates[stateNode.GetValue(kDisplayNameValue)] = isEnabled;
            }
        }

        /// <inheritdoc />
        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            updateSavedStates();
            node.RemoveNodes(kDockingNodeStateNode);

            foreach (KeyValuePair<string, bool> state in dockingNodeStates)
            {
                ConfigNode stateNode = node.AddNode(kDockingNodeStateNode);
                stateNode.AddValue(kDisplayNameValue, state.Key);
                stateNode.AddValue(kIsEnabledValue, state.Value);
            }
        }

        /// <inheritdoc />
        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            setupDockingNodeToggles();
            StartCoroutine(applyDockingNodeStatesWhenReady());
        }

        private void setupDockingNodeToggles()
        {
            if (dockingNodeToggles.Count > 0 || part == null)
                return;

            List<WBIModuleDockingNode> dockingNodes = part.FindModulesImplementing<WBIModuleDockingNode>();
            for (int index = 0; index < dockingNodes.Count; index++)
            {
                WBIModuleDockingNode dockingNode = dockingNodes[index];
                bool isEnabled = dockingNode.isEnabled && dockingNode.enabled && dockingNode.moduleIsEnabled;
                if (dockingNodeStates.ContainsKey(dockingNode.displayName))
                    isEnabled = dockingNodeStates[dockingNode.displayName];

                DockingNodeToggle dockingNodeToggle = new DockingNodeToggle
                {
                    dockingNode = dockingNode,
                    isEnabled = isEnabled
                };
                dockingNodeToggles.Add(dockingNodeToggle);
                addToggleField(dockingNodeToggle, index);
                setDockingNodeEnabled(dockingNode, isEnabled, false);
            }
        }

        private void addToggleField(DockingNodeToggle dockingNodeToggle, int index)
        {
            KSPField fieldAttribute = new KSPField
            {
                guiActive = true,
                guiActiveEditor = true,
                isPersistant = false
            };

            BaseField toggleField = new BaseField(fieldAttribute, enabledField, dockingNodeToggle)
            {
                guiName = dockingNodeToggle.dockingNode.displayName,
                group = new BasePAWGroup(kGroupName, Localizer.Format(kGroupDisplayName), false)
            };
            baseFieldName.SetValue(toggleField, kFieldNamePrefix + index, null);

            toggleField.uiControlEditor = createToggleControl();
            toggleField.uiControlFlight = createToggleControl();
            Fields.Add(toggleField);
        }

        private UI_Toggle createToggleControl()
        {
            return new UI_Toggle
            {
                disabledText = Localizer.Format(kDisabledText),
                enabledText = Localizer.Format(kEnabledText),
                onFieldChanged = onDockingNodeToggleChanged
            };
        }

        private void onDockingNodeToggleChanged(BaseField field, object oldValue)
        {
            DockingNodeToggle dockingNodeToggle = field.host as DockingNodeToggle;
            if (dockingNodeToggle == null || dockingNodeToggle.dockingNode == null)
                return;

            setDockingNodeEnabled(dockingNodeToggle.dockingNode, dockingNodeToggle.isEnabled, true);
            dockingNodeStates[dockingNodeToggle.dockingNode.displayName] = dockingNodeToggle.isEnabled;
            MonoUtilities.RefreshContextWindows(part);
        }

        private void setDockingNodeEnabled(WBIModuleDockingNode dockingNode, bool isEnabled, bool updateFSM)
        {
            if (updateFSM)
                updateDockingNodeFSM(dockingNode, isEnabled);

            dockingNode.moduleIsEnabled = isEnabled;
            dockingNode.enabled = isEnabled;
            dockingNode.isEnabled = isEnabled;
        }

        private void updateDockingNodeFSM(WBIModuleDockingNode dockingNode, bool isEnabled)
        {
            if (dockingNode.fsm == null || !dockingNode.fsm.Started)
                return;

            if (isEnabled && dockingNode.IsDisabled)
                dockingNode.fsm.RunEvent(dockingNode.on_enable);
            else if (!isEnabled && !dockingNode.IsDisabled)
                dockingNode.fsm.RunEvent(dockingNode.on_disable);
        }

        private IEnumerator applyDockingNodeStatesWhenReady()
        {
            bool waitingForFSM;
            do
            {
                waitingForFSM = false;
                for (int index = 0; index < dockingNodeToggles.Count; index++)
                {
                    WBIModuleDockingNode dockingNode = dockingNodeToggles[index].dockingNode;
                    if (dockingNode != null && dockingNode.fsm != null && !dockingNode.fsm.Started)
                    {
                        waitingForFSM = true;
                        break;
                    }
                }

                if (waitingForFSM)
                    yield return null;
            }
            while (waitingForFSM);

            for (int index = 0; index < dockingNodeToggles.Count; index++)
            {
                DockingNodeToggle dockingNodeToggle = dockingNodeToggles[index];
                if (dockingNodeToggle.dockingNode != null)
                    updateDockingNodeFSM(dockingNodeToggle.dockingNode, dockingNodeToggle.isEnabled);
            }
        }

        private void updateSavedStates()
        {
            for (int index = 0; index < dockingNodeToggles.Count; index++)
            {
                DockingNodeToggle dockingNodeToggle = dockingNodeToggles[index];
                if (dockingNodeToggle.dockingNode != null)
                    dockingNodeStates[dockingNodeToggle.dockingNode.displayName] = dockingNodeToggle.isEnabled;
            }
        }
    }
}
