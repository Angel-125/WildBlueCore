using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using WildBlueCore.Utilities;

namespace WildBlueCore.PartModules.IVA
{
    public class InternalModuleLightToggle: InternalBaseModule
    {
        #region Fields
        [KSPField]
        public string buttonTransformName;

        [KSPField]
        public float dimmerLevel = 0.5f;

        [KSPField]
        public string excludedProps = "WBI_Monitor;wbiDigitalPictureFrame;wbiAlertLight";
        #endregion

        #region Housekeeping
        public bool lightsOn = false;

        Light[] lights;
        List<float> lightLevels;
        List<Color> lightColors;
        List<Color> emissiveColors;
        List<Material> emissiveMaterials;
        InternalModuleScreenshot screenshotModule;
        ModuleColorChanger colorChanger = null;
        #endregion

        #region Overrides
        public override void OnStart()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            // Get the lights
            setupLights();

            // Get the emissive textures
            setupEmissives();

            findScreenshotModule();

            colorChanger = part.FindModuleImplementing<ModuleColorChanger>();
        }

        protected override void onGroupUpdated(InternalBaseModule source)
        {
            if (source is InternalModuleLightToggle)
            {
                InternalModuleLightToggle lightToggle = (InternalModuleLightToggle)source;
                lightsOn = lightToggle.lightsOn;
            }
        }

        protected override void onTriggerClick()
        {
            toggleLights();
        }
        #endregion

        #region Helpers
        void toggleLights()
        {
            lightsOn = !lightsOn;

            for (int index = 0; index < lights.Length; index++)
            {
                lights[index].intensity = lightsOn ? lightLevels[index] : dimmerLevel;
            }

            int count = emissiveMaterials.Count;
            for (int index = 0; index < count; index++)
            {
                if (screenshotModule != null && screenshotModule.rendererMaterial.material == emissiveMaterials[index])
                    continue;

                emissiveMaterials[index].SetColor("_EmissiveColor", lightsOn ? emissiveColors[index] : Color.black);
            }

            if (colorChanger != null)
            {
                colorChanger.ToggleEvent();
            }

            eventGroupUpdated.Fire(this, groupId);
        }

        void setupLights()
        {
            lights = internalModel.FindModelComponents<Light>();
            Light light;
            if (lights.Length > 0)
            {
                lightLevels = new List<float>();
                lightColors = new List<Color>();

                for (int index = 0; index < lights.Length; index++)
                {
                    light = lights[index];
                    lightLevels.Add(light.intensity);
                    lightColors.Add(light.color);

                    if (light.intensity != 0)
                        lightsOn = true;
                }
            }
        }

        void setupEmissives()
        {
            emissiveColors = new List<Color>();
            emissiveMaterials = new List<Material>();

            // Exclude the button materials of other props with InternalModuleLightToggle as well as excluded props.
            int count = internalModel.props.Count;
            InternalProp prop;
            InternalModuleLightToggle lightToggle;
            InternalModuleButtonToggle buttonToggle;
            List<Material> excludedMaterials = new List<Material>();
            Renderer[] renderers;
            Renderer renderer;

            for (int index = 0; index < count; index++)
            {
                prop = internalModel.props[index];
                buttonToggle = prop.FindModelComponent<InternalModuleButtonToggle>();
                if (buttonToggle != null)
                {
                    renderer = prop.FindModelComponent<Renderer>(buttonToggle.buttonTransformName);
                    if (renderer != null)
                        excludedMaterials.Add(renderer.material);
                }
                lightToggle = prop.FindModelComponent<InternalModuleLightToggle>();
                if (lightToggle != null)
                {
                    renderer = prop.FindModelComponent<Renderer>(lightToggle.buttonTransformName);
                    if (renderer != null)
                        excludedMaterials.Add(renderer.material);
                }
                else if (!string.IsNullOrEmpty(excludedProps) && !string.IsNullOrEmpty(prop.propName) && excludedProps.Contains(prop.propName))
                {
                    renderers = prop.FindModelComponents<Renderer>();
                    for (int renderIndex = 0; renderIndex < renderers.Length; renderIndex++)
                        excludedMaterials.Add(renderers[renderIndex].material);
                }
            }

            // Exclude materials in any IVA Variants that are hidden
            ModuleIVAVariants iVAVariants = part.FindModuleImplementing<ModuleIVAVariants>();
            if (iVAVariants != null)
                excludedMaterials.AddRange(iVAVariants.FetchHiddenMaterials());

            // Now find all the materials in the model that have emissives
            renderers = internalModel.FindModelComponents<Renderer>();
            for (int index = 0; index < renderers.Length; index++)
            {
                if (excludedMaterials.Contains(renderers[index].material))
                    continue;

                if (renderers[index].material.HasProperty("_Emissive") && renderers[index].material.GetTexture("_Emissive"))
                {
                    emissiveColors.Add(renderers[index].material.color);
                    emissiveMaterials.Add(renderers[index].material);
                }
            }
        }

        void findScreenshotModule()
        {
            int count = internalProp.internalModules.Count;
            for (int index = 0; index < count; index++)
            {
                if (internalProp.internalModules[index] is InternalModuleScreenshot)
                {
                    screenshotModule = (InternalModuleScreenshot)internalProp.internalModules[index];
                    break;
                }
            }
        }
        #endregion
    }
}
