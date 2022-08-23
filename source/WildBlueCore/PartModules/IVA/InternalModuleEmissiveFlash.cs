using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace WildBlueCore.PartModules.IVA
{
    public class InternalModuleEmissiveFlash: InternalBaseModule
    {
        #region Fields
        [KSPField]
        public string emissiveTransformName;

        [KSPField]
        public float flashDuration = 1f;
        #endregion

        #region Housekeeping
        Material emissiveMaterial = null;
        Color originalEmissiveColor;
        Color originalColor;
        Color flashColor;
        bool isFlashing = false;
        bool flashOn = false;
        double flashTime = -1f;
        #endregion

        #region Overrides
        public override void OnStart()
        {
            Renderer renderer;
            renderer = internalProp.FindModelComponent<Renderer>(emissiveTransformName);
            if (renderer != null)
            {
                emissiveMaterial = renderer.material;
                originalEmissiveColor = emissiveMaterial.GetColor("_EmissiveColor");
                originalColor = emissiveMaterial.GetColor("_Color");
            }
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || !isFlashing)
                return;

            if (Planetarium.GetUniversalTime() >= flashTime)
            {
                flashTime = Planetarium.GetUniversalTime() + flashDuration;
                flashOn = !flashOn;
                updateEmissiveColor();
            }
        }

        protected override void onGroupUpdated(InternalBaseModule source)
        {
            if (source is InternalModuleButtonToggle)
            {
                InternalModuleButtonToggle buttonToggle = (InternalModuleButtonToggle)source;

                isFlashing = buttonToggle.isToggledOn;
                if (isFlashing)
                {
                    flashTime = Planetarium.GetUniversalTime() + flashDuration;
                    flashColor = buttonToggle.toggleOnColor;
                    flashOn = true;
                }
                else
                {
                    flashOn = false;
                }
                updateEmissiveColor();
            }
        }
        #endregion

        #region Helpers
        void updateEmissiveColor()
        {
            if (emissiveMaterial != null)
            {
                emissiveMaterial.SetColor("_Color", flashOn ? flashColor : originalColor);
                emissiveMaterial.SetColor("_EmissiveColor", flashOn ? flashColor : originalEmissiveColor);
            }
        }
        #endregion
    }
}
