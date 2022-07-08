using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WildBlueCore
{
    class ModuleConstructionRotationSnap: BasePartModule
    {
        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            // Debug settings
            Events["SnapLeft"].guiActive = debugMode;
        }
        #endregion

        #region Events
        [KSPEvent(guiActiveUnfocused = true, unfocusedRange = 5)]
        public void SnapLeft()
        {
            AttachNode oppositeNode = part.attachNodes[0].FindOpposingNode();
            part.attachNodes[0].attachedPart.transform.rotation = oppositeNode.nodeTransform.rotation;
//            part.Rigidbody.rotation = part.attachNodes[0].attachedPart.Rigidbody.rotation;
        }
        #endregion
    }
}
