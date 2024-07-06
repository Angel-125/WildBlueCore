using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.Localization;
using WildBlueCore;

namespace WildBlueCore.PartModules.Experimental
{
    public class ModuleMassTest: BasePartModule
    {
        public override void OnUpdate()
        {
            base.OnUpdate();
            float prefabMass = part.partInfo.partPrefab.mass;
            if (part.prefabMass.Equals(prefabMass))
            {
                return;
            }
            Debug.Log("[ModuleMassTest] - Resetting mass on " + part.partInfo.name);
            part.needPrefabMass = true;
            part.UpdateMass();

            Debug.Log("[ModuleMassTest OnUpdate] - " + part.partInfo.name + " has current mass: " + part.mass);
            Debug.Log("[ModuleMassTest OnUpdate] - " + part.partInfo.name + " has vessel mass: " + part.vessel.GetTotalMass());
            Debug.Log("[ModuleMassTest OnUpdate] - " + part.partInfo.name + " has current rb mass: " + part.rb.mass);
            Debug.Log("[ModuleMassTest OnUpdate] - " + part.partInfo.name + " has current prefabMass: " + part.prefabMass);
            Debug.Log("[ModuleMassTest OnUpdate] - " + part.partInfo.name + " has new mass: " + part.mass);
            Debug.Log("[ModuleMassTest OnUpdate] - " + part.partInfo.name + " has prefab mass: " + prefabMass);
            Debug.Log("[ModuleMassTest OnUpdate] - " + part.partInfo.name + " has module mass: " + part.GetModuleMass(prefabMass));
            Debug.Log("[ModuleMassTest OnUpdate] - " + part.partInfo.name + " has resource mass: " + part.GetResourceMass());
            Debug.Log("[ModuleMassTest OnUpdate] - " + part.partInfo.name + " has vessel mass after fix: " + part.vessel.GetTotalMass());
            Debug.Log("[ModuleMassTest OnUpdate] - " + part.partInfo.name + " has new rb mass: " + part.rb.mass);
            Debug.Log("[ModuleMassTest OnUpdate] - " + part.partInfo.name + " has new mass post UpdateMass: " + part.mass);
            Debug.Log("[ModuleMassTest OnUpdate] - " + part.partInfo.name + " has new prefabMass: " + part.prefabMass);
        }
    }
}
