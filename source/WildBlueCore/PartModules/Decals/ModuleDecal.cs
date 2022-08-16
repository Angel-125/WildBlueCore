using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.UI.Screens;
using KSP.Localization;

namespace WildBlueCore.PartModules.Decals
{
    /// <summary>
    /// This part module lets you change the decal using the stock flag selector. It does so independently of the mission flag.
    /// </summary>
    public class ModuleDecal : BasePartModule
    {
        /// <summary>
        /// URL to the image that's displayed by the decal.
        /// </summary>
        [KSPField(isPersistant = true)]
        public string decalURL = string.Empty;

        /// <summary>
        /// Flag to indicate whether or not the decal is visible
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool isVisible;

        /// <summary>
        /// Override flag to ensure that the decal is always visible.
        /// </summary>
        [KSPField()]
        public bool alwaysVisible;

        /// <summary>
        /// Flag to allow users to change the flag while out in the field.
        /// </summary>
        [KSPField()]
        public bool allowFieldEdit;

        /// <summary>
        /// Flag to indicate if the decal updates symmetry parts
        /// </summary>
        [KSPField()]
        public bool updateSymmetry = true;

        /// <summary>
        /// GUI name for button that toggles decal visibility
        /// </summary>
        [KSPField()]
        public string toggleDecalName = "Toggle Decal";

        /// <summary>
        ///  GUI name for button that selects the decal.
        /// </summary>
        [KSPField()]
        public string selectDecalName = "Select Decal";

        /// <summary>
        /// List of transforms that will be changed by the decal. Separate names by semicolon
        /// </summary>
        [KSPField()]
        public string decalTransforms = string.Empty;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            toggleDecalName = Localizer.Format("#LOC_WILDBLUECORE_toggleDecal");
            selectDecalName = Localizer.Format("#LOC_WILDBLUECORE_changeDecal");

            Events["ToggleDecal"].guiName = toggleDecalName;
            Events["ToggleDecal"].active = !alwaysVisible;
            Events["ToggleDecal"].guiActive = allowFieldEdit;
            Events["SelectDecal"].guiActive = allowFieldEdit;

            Events["SelectDecal"].guiName = selectDecalName;

            ChangeDecal();
            GameEvents.onEditorVariantApplied.Add(this.onEditorVariantApplied);
        }

        public void OnDestroy()
        {
            GameEvents.onEditorVariantApplied.Remove(this.onEditorVariantApplied);
        }

        /// <summary>
        /// Toggles visibility of the decal.
        /// </summary>
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Toggle Decal")]
        public void ToggleDecal()
        {
            isVisible = !isVisible;
            ChangeDecal();

            updateSymmetryParts();
        }

        /// <summary>
        /// Changes the decal
        /// </summary>
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Select Decal")]
        public void SelectDecal()
        {
            decalURL = HighLogic.CurrentGame.flagURL;
            FlagBrowser flagBrowser = (UnityEngine.Object.Instantiate((UnityEngine.Object)(new FlagBrowserGUIButton(null, null, null, null)).FlagBrowserPrefab) as GameObject).GetComponent<FlagBrowser>();
            flagBrowser.OnFlagSelected = onFlagSelected;
        }

        /// <summary>
        /// Private event handler to respond to flag selection.
        /// </summary>
        /// <param name="selected">The selected texture</param>
        private void onFlagSelected(FlagBrowser.FlagEntry selected)
        {
            decalURL = selected.textureInfo.name;
            ChangeDecal();

            updateSymmetryParts();
        }

        public void updateSymmetryParts()
        {
            if (updateSymmetry)
            {
                ModuleDecal nameTag;
                foreach (Part symmetryPart in this.part.symmetryCounterparts)
                {
                    nameTag = symmetryPart.GetComponent<ModuleDecal>();
                    nameTag.decalURL = this.decalURL;
                    nameTag.isVisible = this.isVisible;
                    nameTag.ChangeDecal();
                }
            }
        }

        private void onEditorVariantApplied(Part parModified, PartVariant partVariant)
        {
            if (parModified == this.part)
            {
                ChangeDecal();
            }
        }

        /// <summary>
        /// Changes the decal on all named transforms.
        /// </summary>
        public void ChangeDecal()
        {
            string[] tagTransforms = decalTransforms.Split(';');
            Transform[] targets;
            Texture textureForDecal;
            Renderer rendererMaterial;

            foreach (string transform in tagTransforms)
            {
                //Get the targets
                targets = part.FindModelTransforms(transform);
                if (targets == null)
                {
                    Debug.Log("No targets found for " + transform);
                    return;
                }

                foreach (Transform target in targets)
                {
                    target.gameObject.SetActive(isVisible);
                    Collider collider = target.gameObject.GetComponent<Collider>();
                    if (collider != null)
                        collider.enabled = isVisible;

                    if (string.IsNullOrEmpty(decalURL) == false)
                    {
                        rendererMaterial = target.GetComponent<Renderer>();
                        textureForDecal = GameDatabase.Instance.GetTexture(decalURL, false);
                        if (textureForDecal != null)
                            rendererMaterial.material.SetTexture("_MainTex", textureForDecal);
                    }
                }
            }
        }
    }
}
