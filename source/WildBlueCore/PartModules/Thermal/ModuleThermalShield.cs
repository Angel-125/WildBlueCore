using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WildBlueCore.Utilities;

namespace WildBlueCore.PartModules.Thermal
{
    public class WBIModuleThermalShield: WBIBasePartModule
    {
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight && !HighLogic.LoadedSceneIsEditor)
                return;

            if (part.parent == null)
                return;

            part.parent.maxTemp = part.maxTemp;
            part.parent.skinMaxTemp = part.skinMaxTemp;
        }
    }
}
