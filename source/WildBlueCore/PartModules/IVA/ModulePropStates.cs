using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace WildBlueCore.PartModules.IVA
{
    public class WBIModulePropStates : WBIBasePartModule
    {
        protected Dictionary<string, string> propModuleProperties = new Dictionary<string, string>();

        [KSPField]
        public string animationName;

        #region KSPAPI
        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            ConfigNode saveNode;

            foreach (string key in propModuleProperties.Keys)
            {
                saveNode = new ConfigNode();
                saveNode.name = "PROPVALUE";
                saveNode.AddValue("name", key);
                saveNode.AddValue("value", propModuleProperties[key]);
                node.AddNode(saveNode);
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (HighLogic.LoadedSceneIsFlight == false)
                return;

            ConfigNode[] propValues = node.GetNodes("PROPVALUE");
            foreach (ConfigNode propValueNode in propValues)
                propModuleProperties.Add(propValueNode.GetValue("name"), propValueNode.GetValue("value"));
        }
        #endregion

        #region API
        public void SaveProperty(int propID, string property, string value)
        {
            string key = propID.ToString() + property;

            if (propModuleProperties.ContainsKey(key))
                propModuleProperties[key] = value;
            else
                propModuleProperties.Add(key, value);
        }

        public string LoadProperty(int propID, string property)
        {
            string key = propID.ToString() + property;

            if (propModuleProperties.ContainsKey(key))
                return propModuleProperties[key];
            else
                return string.Empty;
        }
        #endregion
    }

}
