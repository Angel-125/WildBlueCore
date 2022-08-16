using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace WildBlueCore.PartModules.IVA
{
    public class InternalModuleLightColorChanger : InternalBaseModule
    {
        #region Fields
        [KSPField]
        public string buttonTransformName;
        #endregion

        #region Housekeeping
        public int currentColorIndex = 0;

        Light[] lights;
        List<Color> lightColors;
        List<Color> colorOptions;
        Material buttonRenderMaterial;
        #endregion

        #region Overrides
        public override void OnStart()
        {
            base.OnStart();
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            // Get the lights
            setupLights();

            // Get the color options
            getColorOptions();

            Renderer renderer = internalProp.FindModelComponent<Renderer>(buttonTransformName);
            if (renderer)
                buttonRenderMaterial = renderer.material;
        }

        protected override void groupUpdated(InternalBaseModule source, string sourceGroupId)
        {
            if (source.vessel != vessel)
                return;

            if (source is InternalModuleLightColorChanger && groupId == sourceGroupId)
            {
                InternalModuleLightColorChanger colorChanger = (InternalModuleLightColorChanger)source;
                currentColorIndex = colorChanger.currentColorIndex;
                changeLightColors();
                changeButtonMaterial();
            }
            else if (source is InternalModuleLightToggle && buttonRenderMaterial != null)
            {
                buttonRenderMaterial.SetColor("_EmissiveColor", colorOptions[currentColorIndex]);
            }
        }

        protected override void onTriggerClick()
        {
            int count = colorOptions.Count;
            if (count <= 0)
                return;

            currentColorIndex = (currentColorIndex + 1) % count;

            changeLightColors();
            changeButtonMaterial();

            eventGroupUpdated.Fire(this, groupId);
        }
        #endregion

        #region Helpers
        void changeButtonMaterial()
        {
            if (buttonRenderMaterial != null)
            {
                buttonRenderMaterial.SetColor("_MainTex", colorOptions[currentColorIndex]);
                buttonRenderMaterial.SetColor("_EmissiveColor", colorOptions[currentColorIndex]);
            }
        }

        void changeLightColors()
        {
            for (int index = 0; index < lights.Length; index++)
            {
                lights[index].color = colorOptions[currentColorIndex];
            }
        }

        void getColorOptions()
        {
            ConfigNode node = GetConfigNode();
            if (node == null || !node.HasValue("colorOption"))
                return;

            colorOptions = new List<Color>();
            string[] colorOptionValuess = node.GetValues("colorOption");
            float red = -1f;
            float green = -1f;
            float blue = -1f;
            Color color;
            for (int index = 0; index < colorOptionValuess.Length; index++)
            {
                string[] rgbValues = colorOptionValuess[index].Split(new char[] { ',' });
                if (rgbValues.Length < 3)
                    continue;

                if (!float.TryParse(rgbValues[0], out red))
                    continue;
                if (!float.TryParse(rgbValues[1], out green))
                    continue;
                if (!float.TryParse(rgbValues[2], out blue))
                    continue;

                color = new Color(red, green, blue);
                colorOptions.Add(color);
            }
        }

        void setupLights()
        {
            lights = internalModel.FindModelComponents<Light>();
            Light light;
            if (lights.Length > 0)
            {
                lightColors = new List<Color>();

                for (int index = 0; index < lights.Length; index++)
                {
                    light = lights[index];
                    lightColors.Add(light.color);
                }
            }
        }
        #endregion
    }
}
