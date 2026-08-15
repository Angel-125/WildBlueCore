using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace WildBlueCore.KerbalGear
{
    #region BodyLocations
    /// <summary>
    /// Various locations where an wearable item can be placed. This is primarily used for WBIModuleWearableItem.
    /// </summary>
    public enum BodyLocations
    {
        /// <summary>
        /// On the back of the kerbal.
        /// </summary>
        back,

        /// <summary>
        /// On the back of the kerbal, or the back of the jetpack if the kerbal has a jetpack.
        /// </summary>
        backOrJetpack,

        /// <summary>
        /// The left foot of the kerbal.
        /// </summary>
        leftFoot,

        /// <summary>
        /// The right foot of the kerbal.
        /// </summary>
        rightFoot,

        /// <summary>
        /// The left bicep of the kerbal.
        /// </summary>
        leftBicep,

        /// <summary>
        /// The right bicep of the kerbal.
        /// </summary>
        rightBicep
    }
    #endregion

    /// <summary>
    /// This module represents an equippable cargo item that appears as a 3D model on the kerbal. When equipping the item, this part module can also
    /// request one or more part modules on the kerbal that provide various abilities. For example, an item can request the WBIModuleEVAOverrides to improve the kerbal's swim speed.
    /// The requested part modules and their multiplicity are registered in KERBAL_EVA_MODULES config nodes and are created dynamically while needed.
    /// EVA_PART_MODULE child nodes request modules and may override their runtime configuration for this wearable.
    /// You can have more than one WBIModuleWearableItem part module per cargo part.
    /// </summary>
    /// <example>
    /// <code>
    ///    MODULE
    ///    {
    ///         name = WBIModuleWearableItem
    ///         moduleID = SCUBA Tank
    ///         bodyLocation = back
    ///         anchorTransform = scubaTank
    ///         meshTransform = tankMesh
    ///         positionOffset = 0.0000, 0.0200, 0.0900
    ///         positionOffsetJetpack = 0,0,0
    ///         rotationOffset = -70.0000, 0.0000, 0.0000
    ///         showChuteTransforms = false
    ///         EVA_PART_MODULE
    ///         {
    ///             name = WBIModuleEVADiveComputer
    ///         }
    ///    }
    /// </code>
    /// </example>
    public class WBIModuleWearableItem : WBIBasePartModule
    {
        #region Constants
        const string kPropNode = "PROP";
        const string kEVAPartModuleNode = "EVA_PART_MODULE";
        const string kNameField = "name";
        #endregion

        #region Housekeeping
        readonly List<ConfigNode> evaPartModuleConfigs = new List<ConfigNode>();
        #endregion

        /// <summary>
        /// ID of the module. This should be unique to the part.
        /// </summary>
        [KSPField]
        public string moduleID;

        /// <summary>
        /// Where to place the item, such as on the back of the kerbal, the end of the backpack. etc. See [[BodyLocations|KerbalGear.BodyLocations]].
        /// </summary>
        [KSPField]
        public BodyLocations bodyLocation;

        /// <summary>
        /// Name of the high-level anchor transform. This will follow the bodyLocation bone as it moves.
        /// </summary>
        [KSPField]
        public string anchorTransform;

        /// <summary>
        /// Name of the 3D model. This will be rotated and positioned relative to the anchorTransform.
        /// </summary>
        [KSPField]
        public string meshTransform;

        /// <summary>
        /// Position offsets (x,y,z).
        /// </summary>
        [KSPField]
        public Vector3 positionOffset;

        /// <summary>
        /// Position offset that is used when the kerbal has a jetpack in addition to the wearable item (x,y,z).
        /// Requires bodyLocation = backOrJetpack
        /// </summary>
        [KSPField]
        public Vector3 positionOffsetJetpack;

        /// <summary>
        /// Rotation offsets in degrees
        /// </summary>
        [KSPField]
        public Vector3 rotationOffset;

        /// <summary>
        /// Flag to indicate whether the compact stock ChuteStTransform should remain visible while this wearable item is equipped on the kerbal's back.
        /// The kerbal must also be carrying the stock evaChute inventory part.
        /// </summary>
        [KSPField]
        public bool showChuteTransforms = false;

        /// <summary>
        /// Legacy list of part modules to create on the kerbal when you equip the wearable item.
        /// Separate names with a semicolon. Prefer EVA_PART_MODULE child nodes for new configs.
        /// </summary>
        [KSPField]
        public string evaModules = null;

        /// <summary>
        /// Loads configurable EVA module requests. Each EVA_PART_MODULE node names a module
        /// registered by KERBAL_EVA_MODULES and may override that definition's runtime fields.
        /// </summary>
        /// <param name="node">The wearable item's MODULE configuration.</param>
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            evaPartModuleConfigs.Clear();
            ConfigNode[] moduleNodes = node.GetNodes(kEVAPartModuleNode);
            for (int index = 0; index < moduleNodes.Length; index++)
            {
                string moduleName = moduleNodes[index].GetValue(kNameField);
                if (string.IsNullOrEmpty(moduleName))
                {
                    Debug.LogWarning("[WildBlueCore] EVA_PART_MODULE on " +
                        (part != null ? part.name : "an unknown part") +
                        " has no name and will be ignored.");
                    continue;
                }

                evaPartModuleConfigs.Add(moduleNodes[index].CreateCopy());
            }
        }

        /// <summary>
        /// Gets copies of the explicitly configured EVA_PART_MODULE requests.
        /// </summary>
        internal ConfigNode[] GetEVAPartModuleConfigs()
        {
            ConfigNode[] configs = new ConfigNode[evaPartModuleConfigs.Count];
            for (int index = 0; index < evaPartModuleConfigs.Count; index++)
                configs[index] = evaPartModuleConfigs[index].CreateCopy();
            return configs;
        }

        /// <summary>
        /// Reports whether this wearable requests the named EVA module through either the new
        /// EVA_PART_MODULE nodes or the legacy semicolon-delimited evaModules field.
        /// </summary>
        internal bool RequestsEVAModule(string moduleName)
        {
            for (int index = 0; index < evaPartModuleConfigs.Count; index++)
            {
                if (evaPartModuleConfigs[index].GetValue(kNameField) == moduleName)
                    return true;
            }

            if (string.IsNullOrEmpty(evaModules))
                return false;

            string[] moduleNames = evaModules.Split(new char[] { ';' },
                StringSplitOptions.RemoveEmptyEntries);
            for (int index = 0; index < moduleNames.Length; index++)
            {
                if (moduleNames[index].Trim() == moduleName)
                    return true;
            }
            return false;
        }
    }
}
