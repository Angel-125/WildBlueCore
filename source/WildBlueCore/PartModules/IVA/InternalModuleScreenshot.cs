using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using WildBlueCore.Utilities;

namespace WildBlueCore.PartModules.IVA
{
    public class WBIInternalModuleScreenshot: WBIInternalBaseModule
    {
        #region Constants
        private const float kMinimumSwitchTime = 30.0f;
        #endregion

        #region Fields
        [KSPField]
        public float screenSwitchDuration = 30f;

        [KSPField]
        public string screenTransformName = "Screen";

        [KSPField]
        public bool showScreenHideShowControl = false;

        [KSPField]
        public bool screenIsVisible = false;

        [KSPField]
        public string screeshotFolderPath = string.Empty;

        [KSPField]
        public bool setInitialRandomImage = false;
        #endregion

        #region Housekeeping
        public Renderer rendererMaterial;

        string imagePath;
        ScreenshotView screenView;
        bool enableRandomImages;
        double screenSwitchTime;
        Texture defaultTexture;
        Transform screenTransform = null;
        Texture textureShown = null;
        bool ivaVisible;
        #endregion

        #region Overrides
        public override void OnStart()
        {
            if (HighLogic.LoadedSceneIsFlight == false)
                return;
            screenView = new ScreenshotView();
            getDefaultTexture();

            //Get the screen's render material
            screenTransform = internalProp.FindModelTransform(screenTransformName);
            if (screenTransform == null)
                return;
            rendererMaterial = screenTransform.GetComponent<Renderer>();
            SetScreenVisible(false);

            //Get the prop state helper.
            setupPropStates();

            //Setup screen view
            setupScreenView();

            // Setup events
            WBIInternalModuleAnimation.onAnimationPlayed.Add(onAnimationPlayed);
            WBIInternalModuleAnimation.onAnimationFinished.Add(onAnimationFinished);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            screenView.SetVisible(false);
            screenView = null;
            textureShown = null;
            rendererMaterial = null;
            WBIInternalModuleAnimation.onAnimationPlayed.Remove(onAnimationPlayed);
            WBIInternalModuleAnimation.onAnimationFinished.Remove(onAnimationFinished);
        }

        public void FixedUpdate()
        {
            ivaVisible = ivaIsVisible;

            if (enableRandomImages == false || ivaVisible == false || TimeWarp.CurrentRateIndex > 0)
            {
                screenSwitchTime = Planetarium.GetUniversalTime() + screenSwitchDuration;
                return;
            }

            if (screenSwitchDuration <= 0)
                screenSwitchDuration = kMinimumSwitchTime;
            if (screenSwitchTime <= 0)
            {
                screenSwitchTime = Planetarium.GetUniversalTime() + screenSwitchDuration;
            }

            if (Planetarium.GetUniversalTime() >= screenSwitchTime)
            {
                screenSwitchTime = Planetarium.GetUniversalTime() + screenSwitchDuration;
                screenView.GetRandomImage();
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (!HighLogic.LoadedSceneIsFlight || rendererMaterial == null)
                return;

            if (rendererMaterial.enabled != screenIsVisible && ivaVisible)
                SetScreenVisible(screenIsVisible);
        }
        #endregion

        #region Callbacks
        public void SetScreenVisible(bool isVisible)
        {
            if (rendererMaterial == null)
                return;

            screenIsVisible = isVisible;
            if (propStates != null)
                propStates.SaveProperty(internalProp.propID, "screenIsVisible", screenIsVisible.ToString());

            Color clearColor = new Color(1, 1, 1, 0);
            Color color = isVisible ? Color.white : clearColor;
            rendererMaterial.material.SetColor("_MainTex", color);
            rendererMaterial.material.SetColor("_EmissiveColor", color);
            rendererMaterial.enabled = isVisible;
        }

        public void ShowImage(Texture texture, string textureFilePath)
        {
            //Save the image path
            imagePath = textureFilePath;
            if (propStates != null)
                propStates.SaveProperty(internalProp.propID, "imagePath", imagePath);

            //Now, replace the textures in each target
            textureShown = texture;
            rendererMaterial.material.SetTexture("_Emissive", texture);
            rendererMaterial.material.SetTexture("_MainTex", texture);
            SetScreenVisible(true);

            //Finally, record the random screen switch state
            enableRandomImages = screenView.enableRandomImages;
            if (propStates != null)
            {
                if (screenSwitchDuration < kMinimumSwitchTime)
                    screenSwitchDuration = kMinimumSwitchTime;

                propStates.SaveProperty(internalProp.propID, "enableRandomImages", enableRandomImages.ToString());
                propStates.SaveProperty(internalProp.propID, "screenSwitchDuration", screenSwitchDuration.ToString());
            }

        }
        #endregion

        #region Helpers
        void setupScreenView()
        {
            string filePath = string.Empty;
            if (!string.IsNullOrEmpty(screeshotFolderPath))
                filePath = KSPUtil.ApplicationRootPath.Replace("\\", "/") + "GameData/" + screeshotFolderPath;

            screenView.showImageDelegate = ShowImage;
            screenView.toggleScreenDelegate = SetScreenVisible;
            screenView.showAlphaControl = this.showScreenHideShowControl;
            screenView.screeshotFolderPath = filePath;
            if (setInitialRandomImage)
            {
                enableRandomImages = true;
                if (propStates != null)
                    propStates.SaveProperty(internalProp.propID, "enableRandomImages", enableRandomImages.ToString());
                screenView.enableRandomImages = enableRandomImages;
                screenView.GetRandomImage();
            }
        }

        void setupPropStates()
        {
            if (propStates != null)
            {
                imagePath = propStates.LoadProperty(internalProp.propID, "imagePath");
                if (string.IsNullOrEmpty(imagePath) == false && System.IO.File.Exists(imagePath))
                {
                    Texture2D image = new Texture2D(1, 1);
                    WWW www = new WWW("file://" + imagePath);

                    www.LoadImageIntoTexture(image);

                    ShowImage(image, imagePath);
                }

                string value = propStates.LoadProperty(internalProp.propID, "enableRandomScreens");
                if (string.IsNullOrEmpty(value) == false)
                    enableRandomImages = bool.Parse(value);

                value = propStates.LoadProperty(internalProp.propID, "screenSwitchDuration");
                if (string.IsNullOrEmpty(value) == false)
                    screenSwitchDuration = float.Parse(value);

                value = propStates.LoadProperty(internalProp.propID, "screenIsVisible");
                if (string.IsNullOrEmpty(value) == false)
                    screenIsVisible = bool.Parse(value);
            }
        }

        void getDefaultTexture()
        {
            Transform target;
            Renderer rendererMaterial;

            //Get the target
            target = internalProp.FindModelTransform(screenTransformName);
            if (target == null)
            {
                return;
            }

            rendererMaterial = target.GetComponent<Renderer>();
            defaultTexture = rendererMaterial.material.GetTexture("_MainTex");
            screenView.defaultTexture = defaultTexture;
        }

        protected override void onTriggerClick()
        {
            screenView.part = this.part;
            screenView.enableRandomImages = enableRandomImages;
            screenView.screenIsVisible = this.screenIsVisible;
            screenView.SetVisible(true);
        }

        void onAnimationPlayed(InternalProp eventProp, bool playInReverse)
        {
            if (eventProp != internalProp)
                return;

            if (playInReverse)
            {
                SetScreenVisible(false);
            }
        }

        void onAnimationFinished(InternalProp eventProp, bool playInReverse)
        {
            if (eventProp != internalProp)
                return;

            if (!playInReverse && textureShown != null)
            {
                SetScreenVisible(true);
            }
        }
        #endregion
    }
}
