/*****************************************************************************
 * The MIT License (MIT)
 * 
 * Copyright (c) 2017-2020 MOARdV
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to
 * deal in the Software without restriction, including without limitation the
 * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
 * sell copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 * DEALINGS IN THE SOFTWARE.
 * 
 ****************************************************************************/
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WildBlueCore.PartModules.IVA
{
    /// <summary>
    /// The MASCameraMode represents the modes (resolution, post-processing shader) applied to
    /// a given camera.
    /// </summary>
    internal class WBICameraMode
    {
        public readonly string name;

        /// <summary>
        /// Post-processing shader to apply.
        /// </summary>
        public Material postProcShader;

        /// <summary>
        /// The resolution of the camera mode in pixels.  Cameras render a square
        /// image.  Valid values are 64 to 2048.  Values outside that range are
        /// clamped.  The value will be adjusted
        /// to a power-of-2 if needed.  Note that large values may cause
        /// problems with lower-end machines.  Defaults to 256.
        /// </summary>
        public readonly int cameraResolution;

        /// <summary>
        /// MAS variables that update shader properties.
        /// </summary>
        private string[] propertyValue = new string[0];

        /// <summary>
        /// Map property names to shader ID numbers.
        /// </summary>
        private int[] propertyId = new int[0];

        /// <summary>
        /// The registrar that manages subscribing / unsubscribing.
        /// </summary>
        private WBIVariableRegistrar variableRegistrar = new WBIVariableRegistrar(null, null);

        /// <summary>
        /// The flight computer we registered with.
        /// </summary>
        private MASFlightComputer comp;

        public WBICameraMode(ConfigNode node, string partName)
        {
            if (!node.TryGetValue("name", ref name))
            {
                name = "(anonymous)";
            }

            string shader = string.Empty;
            if (!node.TryGetValue("shader", ref shader))
            {
                // If I simply blit the output of the camera, I have black sections in the image.  I suspect
                // they're regions where alpha = 0.  So, if the prop config doesn't select a shader, I use a
                // simple pass-through shader that drives alpha to 1.
                shader = "MOARdV/PassThrough";
            }
            else
            {
                string concatProperties = string.Empty;
                if (node.TryGetValue("properties", ref concatProperties))
                {
                    string[] propertiesList = concatProperties.Split(';');
                    int listLength = propertiesList.Length;
                    if (listLength > 0)
                    {
                        propertyId = new int[listLength];
                        propertyValue = new string[listLength];

                        for (int i = 0; i < listLength; ++i)
                        {
                            string[] pair = propertiesList[i].Split(':');
                            if (pair.Length != 2)
                            {
                                throw new ArgumentOutOfRangeException("Incorrect number of parameters for property: requires 2, found " + pair.Length + " in property " + propertiesList[i] + " for camera MODE " + name);
                            }
                            propertyId[i] = Shader.PropertyToID(pair[0].Trim());
                            propertyValue[i] = pair[1].Trim();
                        }
                    }
                }

            }

            if (!MASLoader.shaders.ContainsKey(shader))
            {
                WBICameraUtility.LogError(this, "Invalid shader \"{0}\" in MASCamera MODE {1} in {2}.", shader, name, partName);
                throw new ArgumentException("MASCameraNode: Invalid post-processing shader name.");
            }

            postProcShader = new Material(MASLoader.shaders[shader]);

            string textureName = string.Empty;
            if (node.TryGetValue("texture", ref textureName))
            {
                Texture auxTexture = GameDatabase.Instance.GetTexture(textureName, false);
                if (auxTexture == null)
                {
                    throw new ArgumentException("Unable to find 'texture' " + textureName + " for CAMERA " + name);
                }
                postProcShader.SetTexture("_AuxTex", auxTexture);
            }

            if (!node.TryGetValue("cameraResolution", ref cameraResolution))
            {
                cameraResolution = 256;
            }
            cameraResolution >>= WBICameraConfig.CameraTextureScale;

            WBICameraUtility.LastPowerOf2(ref cameraResolution, 64, 2048);
        }

        /// <summary>
        /// Unsubscribe from the property callbacks for this shader mode.
        /// </summary>
        public void UnregisterShaderProperties()
        {
            variableRegistrar.ReleaseResources();
        }

        /// <summary>
        /// Callback used to tell the mode to refresh its shader properties.
        /// </summary>
        /// <param name="comp"></param>
        public void UpdateShaderProperties(MASFlightComputer comp)
        {
            if (this.comp == comp)
            {
                return;
            }

            UnregisterShaderProperties();

            this.comp = comp;
            variableRegistrar = new WBIVariableRegistrar(comp, null);

            for (int i = 0; i < propertyValue.Length; ++i)
            {
                int id = propertyId[i];
                variableRegistrar.RegisterVariableChangeCallback(propertyValue[i], (double newValue) => postProcShader.SetFloat(id, (float)newValue));
            }
        }
    }
}
