using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using WildBlueCore.Utilities;

namespace WildBlueCore.PartModules.IVA
{
    public class WBIInternalBaseModule: InternalModule
    {
        #region Events
        public static EventData<WBIInternalBaseModule, string> eventGroupUpdated = new EventData<WBIInternalBaseModule, string>("eventGroupUpdated");
        #endregion

        #region Fields
        [KSPField]
        public string triggerName = string.Empty;

        [KSPField]
        public string groupId = string.Empty;

        [KSPField]
        public string subGroupId = string.Empty;

        [KSPField]
        public bool allowSameVessel = false;
        #endregion

        #region Housekeeping
        protected TriggerClickWatcher clickWatcher = null;
        protected WBIModulePropStates propStates = null;
        public bool debugMode;
        #endregion

        #region Computed properties
        public bool ivaIsVisible
        {
            get
            {
                MeshRenderer[] meshRenderers = this.GetComponentsInChildren<MeshRenderer>();
                for (int index = 0; index < meshRenderers.Length; index++)
                {
                    if (meshRenderers[index].enabled)
                        return true;
                }

                SkinnedMeshRenderer[] skinnedMeshRenderers = this.GetComponentsInChildren<SkinnedMeshRenderer>();
                for (int index = 0; index < skinnedMeshRenderers.Length; index++)
                {
                    if (skinnedMeshRenderers[index].enabled)
                        return true;
                }

                return false;
            }
        }
        #endregion

        #region Overrides
        public void Start()
        {
            debugMode = WildBlueCoreScenario.debugMode;
            propStates = part.FindModuleImplementing<WBIModulePropStates>();

            // Get the animation trigger
            Transform trans = internalProp.FindModelTransform(triggerName);
            if (trans != null)
            {
                GameObject goTrigger = trans.gameObject;
                if (goTrigger != null)
                {
                    clickWatcher = goTrigger.GetComponent<TriggerClickWatcher>();
                    if (clickWatcher == null || clickWatcher.internalBaseModule != this)
                    {
                        clickWatcher = goTrigger.AddComponent<TriggerClickWatcher>();
                        clickWatcher.internalBaseModule = this;
                    }
                }
            }
            TriggerClickWatcher.onTriggerClicked.Add(triggerClicked);
            TriggerClickWatcher.onMouseDown.Add(triggerMouseDown);

            eventGroupUpdated.Add(groupUpdated);

            OnStart();
        }

        public virtual void OnDestroy()
        {
            TriggerClickWatcher.onTriggerClicked.Remove(triggerClicked);
            TriggerClickWatcher.onMouseDown.Remove(triggerMouseDown);

            eventGroupUpdated.Remove(groupUpdated);
        }

        public virtual void OnStart()
        {

        }

        protected virtual void onTriggerClick()
        {
        }

        protected virtual void onMouseDown()
        {

        }

        protected virtual void onGroupUpdated(WBIInternalBaseModule source)
        {

        }
        #endregion;

        #region API
        public ConfigNode GetConfigNode()
        {
            string propName = internalProp.propName;
            ConfigNode[] propNodes = GameDatabase.Instance.GetConfigNodes("PROP");

            for (int index = 0; index < propNodes.Length; index++)
            {
                if (propNodes[index].GetValue("name") == propName)
                {
                    ConfigNode[] moduleNodes = propNodes[index].GetNodes("MODULE");
                    for (int moduleIndex = 0; moduleIndex < moduleNodes.Length; moduleIndex++)
                    {
                        if (moduleNodes[moduleIndex].GetValue("name") == ClassName)
                        {
                            return moduleNodes[moduleIndex];
                        }
                    }
                }
            }

            return null;
        }
        #endregion

        #region Helpers
        protected virtual void groupUpdated(WBIInternalBaseModule source, string sourceGroupId)
        {
            if ((groupId == sourceGroupId && source.part == part) || (groupId == sourceGroupId && source.vessel == vessel && allowSameVessel))
                onGroupUpdated(source);
        }

        void triggerClicked(WBIInternalBaseModule internalBaseModule)
        {
            if (internalBaseModule == this)
                onTriggerClick();
        }

        void triggerMouseDown(WBIInternalBaseModule internalBaseModule)
        {
            if (internalBaseModule == this)
                onMouseDown();
        }
        #endregion
    }
}
