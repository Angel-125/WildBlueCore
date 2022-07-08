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
    /// Various locations where an wearable item can be placed. This is primarily used for ModuleWearableItem.
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
    /// activate one or more part modules on the kerbal that provide various abilities. For example, an item can activate the ModuleEVAOverrides to improve the kerbal's swim speed.
    /// The activated part modules are defined in KERBAL_EVA_MODULES config nodes.
    /// You can have more than one ModuleWearableItem part module per cargo part.
    /// </summary>
    /// <example>
    /// <code>
    ///    MODULE
    ///    {
    ///         name = ModuleWearableItem
    ///         moduleID = SCUBA Tank
    ///         bodyLocation = back
    ///         anchorTransform = scubaTank
    ///         meshTransform = tankMesh
    ///         positionOffset = 0.0000, 0.0200, 0.0900
    ///         positionOffsetJetpack = 0,0,0
    ///         rotationOffset = -70.0000, 0.0000, 0.0000
    ///         evaModules = ModuleEVADiveComputer
    ///    }
    /// </code>
    /// </example>
    public class ModuleWearableItem : BasePartModule
    {
        #region Constants
        const string kPropNode = "PROP";
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
        /// Name of the part modules to enable on the kerbal when you equip the wearable item.
        /// Separate names with a semicolon.
        /// </summary>
        [KSPField]
        public string evaModules = null;
    }
}
