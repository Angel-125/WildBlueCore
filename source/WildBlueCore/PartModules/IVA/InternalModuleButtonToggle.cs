using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace WildBlueCore.PartModules.IVA
{
    public class InternalModuleButtonToggle: InternalBaseModule
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
        Material buttonFaceMaterial = null;
        Color originalEmissiveColor;
        Color toggleOnColor;
        public FXGroup soundClip = null;
        #endregion

        #region Overrides
        public override void OnStart()
        {
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

            // Change button face color
            updateButtonColor();

            // Play sound effect
            if (soundClip != null && soundClip.audio != null)
            {
                if (playSoundToggleOnOnly && !isToggledOn)
                    return;

                soundClip.audio.volume = GameSettings.SHIP_VOLUME;
                soundClip.audio.Play();
            }

            // Update the group
            eventGroupUpdated.Fire(this, groupId);
        }

        protected override void onGroupUpdated(InternalBaseModule source)
        {
            if (source != this && source is InternalModuleButtonToggle && groupId == source.groupId)
            {
                InternalModuleButtonToggle buttonToggle = (InternalModuleButtonToggle)source;
                if (buttonToggle.isToggledOn && isToggledOn)
                {
                    isToggledOn = false;
                    updateButtonColor();
                }
            }
        }
        #endregion

        #region Helpers
        void updateButtonColor()
        {
            Color color = isToggledOn ? toggleOnColor : originalEmissiveColor;
            if (buttonFaceMaterial != null)
            {
                buttonFaceMaterial.SetColor("_MainTex", color);
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
