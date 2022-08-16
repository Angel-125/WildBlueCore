using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;
using KSP.IO;

namespace WildBlueCore.PartModules.IVA
{
    public delegate void ShowImageDelegate(Texture selectedImage, string textureFilePath);
    public delegate void SetScreenAlphaDelegate(float alpha);
    public delegate void ToggleScreenDelegate(bool isVisble);

    public class ScreenshotView : Dialog<ScreenshotView>
    {
        public Texture defaultTexture;
        public ShowImageDelegate showImageDelegate;
        public ToggleScreenDelegate toggleScreenDelegate;
        public Texture2D previewImage;
        public string screeshotFolderPath;
        public Transform cameraTransform;
        public Part part;
        public int cameraIndex;
        public bool enableRandomImages;
        public string aspectRatio;
        public bool showAlphaControl;
        public bool screenIsVisible;

        protected string[] imagePaths;
        protected string[] fileNames;
        protected string[] viewOptions = { "Screenshots" };
        protected int viewOptionIndex;
        protected int selectedIndex;
        protected int prevSelectedIndex = -1;

        private Vector2 _scrollPos;

        public ScreenshotView() :
        base("Select An Image", 900, 600)
        {
            Resizable = false;
            _scrollPos = new Vector2(0, 0);
        }

        public override void SetVisible(bool newValue)
        {
            base.SetVisible(newValue);
            fetchImages();
        }

        void fetchImages()
        {
            if (string.IsNullOrEmpty(screeshotFolderPath))
                screeshotFolderPath = KSPUtil.ApplicationRootPath.Replace("\\", "/") + "Screenshots/";

            imagePaths = Directory.GetFiles(screeshotFolderPath);
            List<string> names = new List<string>();
            names.Add("Default");
            foreach (string pictureName in imagePaths)
            {
                names.Add(pictureName.Replace(screeshotFolderPath, ""));
            }
            fileNames = names.ToArray();
        }

        public void GetRandomImage()
        {
            fetchImages();

            int imageIndex = UnityEngine.Random.Range(0, imagePaths.Length - 1);
            Texture2D randomImage = new Texture2D(1, 1);
            WWW www = new WWW("file://" + imagePaths[imageIndex]);

            www.LoadImageIntoTexture(randomImage);

            if (showImageDelegate != null)
                showImageDelegate(randomImage, imagePaths[imageIndex]);
        }

        protected override void DrawWindowContents(int windowId)
        {
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();

            //Toggle holoscreen
            if (showAlphaControl && GUILayout.Button("Show/Hide Screen") && toggleScreenDelegate != null)
            {
                screenIsVisible = !screenIsVisible;
                toggleScreenDelegate(screenIsVisible);
            }

            if (string.IsNullOrEmpty(aspectRatio) == false)
                GUILayout.Label("Aspect Ratio: " + aspectRatio);

            enableRandomImages = GUILayout.Toggle(enableRandomImages, "Enable Random Images");
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, new GUILayoutOption[] { GUILayout.Width(375) });
            if (viewOptionIndex == 0)
                selectedIndex = GUILayout.SelectionGrid(selectedIndex, fileNames, 1);
            //            else
            //                drawCameraControls();

            GUILayout.EndScrollView();

            GUILayout.EndVertical();

            GUILayout.BeginVertical();

            if (viewOptionIndex == 0)
                drawScreenshotPreview();
            //            else
            //                drawCameraView();

            drawOkCancelButtons();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        protected void drawCameraView()
        {
            if (previewImage != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(previewImage, new GUILayoutOption[] { GUILayout.Width(525), GUILayout.Height(400) });
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
        }

        protected void drawScreenshotPreview()
        {
            //Default image is always the first
            if (selectedIndex == 0)
            {
                previewImage = (Texture2D)defaultTexture;
            }

            else if (selectedIndex != prevSelectedIndex)
            {
                prevSelectedIndex = selectedIndex;
                previewImage = new Texture2D(1, 1);
                WWW www = new WWW("file://" + imagePaths[selectedIndex - 1]);

                www.LoadImageIntoTexture(previewImage);
            }

            if (previewImage != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(previewImage, new GUILayoutOption[] { GUILayout.Width(525), GUILayout.Height(400) });
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

        }

        protected void drawCameraControls()
        {
            GUILayout.Label("TODO: camera rotate and zoom controls here.");
        }

        protected void drawOkCancelButtons()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("OK"))
            {
                if (showImageDelegate != null)
                {
                    if (selectedIndex > 0)
                        showImageDelegate(previewImage, imagePaths[selectedIndex - 1]);
                    else
                        showImageDelegate(previewImage, "Default");
                }
                SetVisible(false);
            }

            if (GUILayout.Button("Cancel"))
            {
                SetVisible(false);
            }
            GUILayout.EndHorizontal();
        }
    }
}
