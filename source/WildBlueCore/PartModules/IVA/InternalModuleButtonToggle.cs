using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace WildBlueCore.PartModules.IVA
{
    public class WBIInternalModuleButtonToggle: WBIInternalBaseModule
    {
        #region Fields
        [KSPField]
        public string buttonTransformName;

        [KSPField]
        public string buttonActiveColor = "1,1,1";

        [KSPField]
        public string soundEffectName = string.Empty;

        [KSPField]
        public bool playSoundToggleOnOnly = false;
        #endregion

        #region Housekeeping
        public bool isToggledOn = false;
        public Color toggleOnColor;

        Material buttonFaceMaterial = null;
        Color originalEmissiveColor;
        public FXGroup soundClip = null;
        WBIInternalModuleLightColorChanger colorChanger = null;
        #endregion

        #region Overrides
        public override void OnStart()
        {
            colorChanger = internalProp.FindModelComponent<WBIInternalModuleLightColorChanger>();

            // Get button face material
            getButtonFaceMaterial();

            // Get button's active color
            getButtonActiveColor();

            // Get sound
            loadSoundFX();
        }

        protected override void onTriggerClick()
        {
            // Toggle status
            isToggledOn = !isToggledOn;

            // Update the group
            eventGroupUpdated.Fire(this, groupId);

            // Play sound effect
            if (soundClip != null && soundClip.audio != null)
            {
                if (playSoundToggleOnOnly && !isToggledOn)
                    return;

                soundClip.audio.volume = GameSettings.SHIP_VOLUME;
                soundClip.audio.Play();
            }
        }

        protected override void onGroupUpdated(WBIInternalBaseModule source)
        {
            if (source is WBIInternalModuleButtonToggle)
            {
                WBIInternalModuleButtonToggle buttonToggle = (WBIInternalModuleButtonToggle)source;
                if (buttonToggle != this && buttonToggle.isToggledOn && isToggledOn)
                    isToggledOn = false;
                else if (buttonToggle.subGroupId == subGroupId)
                    isToggledOn = buttonToggle.isToggledOn;

                updateButtonColor();

                if (colorChanger != null)
                    colorChanger.ToggleColor(isToggledOn);
            }
        }
        #endregion

        #region Helpers
        void updateButtonColor()
        {
            Color color = isToggledOn ? toggleOnColor : originalEmissiveColor;
            if (buttonFaceMaterial != null)
            {
                buttonFaceMaterial.SetColor("_Color", color);
                buttonFaceMaterial.SetColor("_EmissiveColor", color);
            }
        }

        void getButtonActiveColor()
        {
            float red = -1f;
            float green = -1f;
            float blue = -1f;
            string[] rgbValues = buttonActiveColor.Split(new char[] { ',' });
            if (rgbValues.Length < 3)
                return;

            if (!float.TryParse(rgbValues[0], out red))
                return;
            if (!float.TryParse(rgbValues[1], out green))
                return;
            if (!float.TryParse(rgbValues[2], out blue))
                return;

            toggleOnColor = new Color(red, green, blue, 1);
        }

        void getButtonFaceMaterial()
        {
            Renderer renderer;
            renderer = internalProp.FindModelComponent<Renderer>(buttonTransformName);
            if (renderer != null)
            {
                buttonFaceMaterial = renderer.material;
                originalEmissiveColor = buttonFaceMaterial.GetColor("_EmissiveColor");
            }
        }

        void loadSoundFX()
        {
            if (string.IsNullOrEmpty(soundEffectName) || !GameDatabase.Instance.ExistsAudioClip(soundEffectName))
                return;

            soundClip.audio = part.gameObject.AddComponent<AudioSource>();
            soundClip.audio.volume = GameSettings.SHIP_VOLUME;
            soundClip.audio.maxDistance = 5;

            soundClip.audio.clip = GameDatabase.Instance.GetAudioClip(soundEffectName);
            soundClip.audio.loop = false;

            soundClip.audio.rolloffMode = AudioRolloffMode.Logarithmic;
            soundClip.audio.panStereo = 1f;
            soundClip.audio.spatialBlend = 1f;
            soundClip.audio.dopplerLevel = 0f;

            soundClip.audio.playOnAwake = false;
        }

        #endregion
    }
}
