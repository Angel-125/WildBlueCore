using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.Localization;

namespace WildBlueCore.PartModules.Coupling
{
    public class WBIModuleDockingNode: ModuleDockingNode
    {
        #region Fields
        [KSPField]
        public string displayName = string.Empty;
        #endregion

        #region Overrides
        public override void OnStart(StartState st)
        {
            base.OnStart(st);

            if (!string.IsNullOrEmpty(displayName))
            {
                Events[nameof(Undock)].guiName = Localizer.Format("#LOC_WILDBLUECORE_undock", new string[] { displayName });
                Events[nameof(Decouple)].guiName = Localizer.Format("#LOC_WILDBLUECORE_decouple", new string[] { displayName });
                Events[nameof(MakeReferenceTransform)].guiName = Localizer.Format("#LOC_WILDBLUECORE_makeRefTransform", new string[] { displayName });

                Events[nameof(Undock)].group.name = displayName;
                Events[nameof(Decouple)].group.name = displayName;
                Events[nameof(MakeReferenceTransform)].group.name = displayName;

                string groupDisplayName = Localizer.Format("#LOC_WILDBLUECORE_groupDisplayName", new string[] { displayName });
                Events[nameof(Undock)].group.displayName = groupDisplayName;
                Events[nameof(Decouple)].group.displayName = groupDisplayName;
                Events[nameof(MakeReferenceTransform)].group.displayName = groupDisplayName;

                Fields[nameof(acquireForceTweak)].group.name = displayName;
                Fields[nameof(acquireForceTweak)].group.displayName = groupDisplayName;
            }
        }
        #endregion
    }
}
