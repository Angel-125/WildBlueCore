/*****************************************************************************
 * The MIT License (MIT)
 * 
 * Copyright (c) 2016-2022 MOARdV
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
using MoonSharp.Interpreter;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

namespace WildBlueCore.PartModules.IVA.Camera.SupportFiles
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class WBICameraLoader : MonoBehaviour
    {
        public void Awake()
        {
            if (!GameDatabase.Instance.IsReady())
            {
                WBICameraUtility.LogError(this, "GameDatabase.IsReady is false");
                throw new Exception("WBICameraLoader: GameDatabase is not ready.  Unable to continue.");
            }

            LoadAssets();

        }

        /// <summary>
        /// Locate the requested asset bundle, load it, and return it.
        /// </summary>
        /// <param name="formatString">The format string to apply to the suffix.</param>
        /// <param name="suffix">The suffix to apply to the formatString.</param>
        /// <returns>null on error, otherwise, the asset bundle.</returns>
        private AssetBundle LoadAssetBundle(string formatString, string suffix)
        {
            string assetBundleName = string.Format(formatString, suffix);
            AssetBundle bundle = null;
            try
            {
                var uwr = UnityWebRequestAssetBundle.GetAssetBundle(assetBundleName);
                // TODO: Make this a coroutine instead of a spin wait?
                //yield return uwr.SendWebRequest();
                uwr.SendWebRequest();

                while (!uwr.isDone)
                {
                    Thread.Sleep(1);
                }

                // Get an asset from the bundle and instantiate it.
                bundle = DownloadHandlerAssetBundle.GetContent(uwr);
                if (bundle == null)
                {
                    WBICameraUtility.LogError(this, "Failed to load asset bundle {0}", assetBundleName);
                }
            }
            catch (Exception e)
            {
                WBICameraUtility.LogError(this, "Exception trying to get asset bundle {0}: {1}", assetBundleName, e.ToString());
            }
            return bundle;
        }

        /// <summary>
        /// Load our assets through the asset bundle system.
        /// </summary>
        private void LoadAssets()
        {
            StringBuilder sb = StringBuilderCache.Acquire();
            sb.Append("file://").Append(KSPUtil.ApplicationRootPath).Append("GameData/MOARdV/AvionicsSystems/mas-{0}.assetbundle");
            string assetFormat = sb.ToStringAndRelease();

            string platform = string.Empty;
            switch (Application.platform)
            {
                case RuntimePlatform.LinuxPlayer:
                    platform = "linux";
                    break;
                case RuntimePlatform.OSXPlayer:
                    platform = "osx";
                    break;
                case RuntimePlatform.WindowsPlayer:
                    platform = (SystemInfo.graphicsDeviceVersion.StartsWith("OpenGL")) ? "linux" : "windows";
                    break;
                default:
                    WBICameraUtility.LogError(this, "Unsupported/unexpected platform {0}", Application.platform);
                    return;
            }

            shaders.Clear();
            AssetBundle bundle = LoadAssetBundle(assetFormat, platform);
            if (bundle == null)
            {
                return;
            }

            string[] assetNames = bundle.GetAllAssetNames();
            int len = assetNames.Length;

            Shader shader;
            for (int i = 0; i < len; i++)
            {
                if (assetNames[i].EndsWith(".shader"))
                {
                    shader = bundle.LoadAsset<Shader>(assetNames[i]);
                    if (!shader.isSupported)
                    {
                        WBICameraUtility.LogError(this, "Shader {0} - unsupported in this configuration", shader.name);
                    }
                    shaders[shader.name] = shader;
                }
            }

            bundle.Unload(false);

            fonts.Clear();
            bundle = LoadAssetBundle(assetFormat, "font");
            if (bundle == null)
            {
                return;
            }

            assetNames = bundle.GetAllAssetNames();
            len = assetNames.Length;

            Font font;
            for (int i = 0; i < len; i++)
            {
                if (assetNames[i].EndsWith(".ttf"))
                {
                    font = bundle.LoadAsset<Font>(assetNames[i]);

                    string[] fnames = font.fontNames;
                    if (fnames.Length == 0)
                    {
                        WBICameraUtility.LogError(this, "Font {0} - did not find fontName.", font.name);
                    }
                    else
                    {
                        if (fonts.ContainsKey(fnames[0]))
                        {
                            // TODO: Do I need to keep all of the fonts in this dictionary?  Or is one
                            // adequate?
                            fonts[fnames[0]].Add(font);
                        }
                        else
                        {
                            WBICameraUtility.LogMessage(this, "Adding font \"{0}\" from asset bundle.", fnames[0]);
                            List<Font> fontList = new List<Font>();
                            fontList.Add(font);
                            fonts[fnames[0]] = fontList;
                        }
                    }
                }
            }
            bundle.Unload(false);

            WBICameraUtility.LogInfo(this, "Found {0} MAS shaders and {1} fonts.", shaders.Count, fonts.Count);

            // User fonts.  We put them here to make sure that internal
            // shaders exist already.
            ConfigNode[] masBitmapFont = GameDatabase.Instance.GetConfigNodes("MAS_BITMAP_FONT");
            for (int masFontIdx = 0; masFontIdx < masBitmapFont.Length; ++masFontIdx)
            {
                LoadBitmapFont(masBitmapFont[masFontIdx]);
            }

            // Generate our list of radio navigation beacons.
            navaids.Clear();
            ConfigNode[] navaidGroupNode = GameDatabase.Instance.GetConfigNodes("MAS_NAVAID");
            for (int navaidGroupIdx = 0; navaidGroupIdx < navaidGroupNode.Length; ++navaidGroupIdx)
            {
                ConfigNode[] navaidNode = navaidGroupNode[navaidGroupIdx].GetNodes("NAVAID");
                for (int navaidIdx = 0; navaidIdx < navaidNode.Length; ++navaidIdx)
                {
                    bool canAdd = true;
                    NavAid navaid = new NavAid();
                    navaid.maximumRange = -1.0;
                    navaid.maximumRangeDME = -1.0;

                    navaid.name = string.Empty;
                    if (!navaidNode[navaidIdx].TryGetValue("name", ref navaid.name))
                    {
                        WBICameraUtility.LogError(this, "Did not get 'name' for NAVAID");
                        canAdd = false;
                    }

                    navaid.identifier = string.Empty;
                    if (!navaidNode[navaidIdx].TryGetValue("id", ref navaid.identifier))
                    {
                        WBICameraUtility.LogError(this, "Did not get 'id' for NAVAID {0}", navaid.name);
                        canAdd = false;
                    }

                    navaid.celestialName = string.Empty;
                    if (!navaidNode[navaidIdx].TryGetValue("celestialName", ref navaid.celestialName))
                    {
                        WBICameraUtility.LogError(this, "Did not get 'celestialName' for NAVAID {0}", navaid.name);
                        canAdd = false;
                    }

                    navaid.frequency = 0.0f;
                    if (!navaidNode[navaidIdx].TryGetValue("frequency", ref navaid.frequency))
                    {
                        WBICameraUtility.LogError(this, "Did not get 'frequency' for NAVAID {0}", navaid.name);
                        canAdd = false;
                    }

                    navaid.latitude = 0.0;
                    if (!navaidNode[navaidIdx].TryGetValue("latitude", ref navaid.latitude))
                    {
                        WBICameraUtility.LogError(this, "Did not get 'latitude' for NAVAID {0}", navaid.name);
                        canAdd = false;
                    }

                    navaid.longitude = 0.0;
                    if (!navaidNode[navaidIdx].TryGetValue("longitude", ref navaid.longitude))
                    {
                        WBICameraUtility.LogError(this, "Did not get 'longitude' for NAVAID {0}", navaid.name);
                        canAdd = false;
                    }

                    navaid.altitude = 0.0;
                    if (!navaidNode[navaidIdx].TryGetValue("altitude", ref navaid.altitude))
                    {
                        WBICameraUtility.LogError(this, "Did not get 'altitude' for NAVAID {0}", navaid.name);
                        canAdd = false;
                    }

                    string type = string.Empty;
                    if (!navaidNode[navaidIdx].TryGetValue("type", ref type))
                    {
                        WBICameraUtility.LogError(this, "Did not get 'type' for NAVAID {0}", navaid.name);
                        canAdd = false;
                    }
                    switch (type)
                    {
                        case "NDB":
                            navaid.type = NavAidType.NDB;
                            break;
                        case "NDB DME":
                            navaid.type = NavAidType.NDB_DME;
                            break;
                        case "VOR":
                            navaid.type = NavAidType.VOR;
                            break;
                        case "VOR DME":
                            navaid.type = NavAidType.VOR_DME;
                            break;
                        case "ILS":
                            navaid.type = NavAidType.ILS;
                            break;
                        case "ILS DME":
                            navaid.type = NavAidType.ILS_DME;
                            break;
                        default:
                            WBICameraUtility.LogError(this, "Did not get valid 'type' for NAVAID {0}", navaid.name);
                            canAdd = false;
                            break;
                    }

                    navaid.maximumRangeLocalizer = 0.0;
                    navaid.maximumRangeGlidePath = 0.0;
                    navaid.glidePathDefault = 3.0;
                    navaid.approachHeadingILS = 0.0f;
                    navaid.localizerSectorILS = 0.0f;
                    if (navaid.type == NavAidType.ILS || navaid.type == NavAidType.ILS_DME)
                    {
                        if (!navaidNode[navaidIdx].TryGetValue("maximumRangeLocalizer", ref navaid.maximumRangeLocalizer))
                        {
                            WBICameraUtility.LogError(this, "Did not get 'maximumRangeLocalizer' for {1} {0}", navaid.name, navaid.type);
                            canAdd = false;
                        }
                        if (!navaidNode[navaidIdx].TryGetValue("maximumRangeGlidePath", ref navaid.maximumRangeGlidePath))
                        {
                            WBICameraUtility.LogError(this, "Did not get 'maximumRangeGlidePath' for {1} {0}", navaid.name, navaid.type);
                            canAdd = false;
                        }
                        if (!navaidNode[navaidIdx].TryGetValue("glidePathDefault", ref navaid.glidePathDefault))
                        {
                            WBICameraUtility.LogError(this, "Did not get 'glidePathDefault' for {1} {0}", navaid.name, navaid.type);
                            canAdd = false;
                        }
                        if (!navaidNode[navaidIdx].TryGetValue("approachHeadingILS", ref navaid.approachHeadingILS))
                        {
                            WBICameraUtility.LogError(this, "Did not get 'approachBearingILS' for {1} {0}", navaid.name, navaid.type);
                            canAdd = false;
                        }
                        if (!navaidNode[navaidIdx].TryGetValue("localizerSectorILS", ref navaid.localizerSectorILS))
                        {
                            WBICameraUtility.LogError(this, "Did not get 'horizontalSectorILS' for {1} {0}", navaid.name, navaid.type);
                            canAdd = false;
                        }
                    }

                    if (canAdd)
                    {
                        navaids.Add(navaid);
                    }
                }
            }

            morseCode.Clear();
            ConfigNode[] morseCodeNode = GameDatabase.Instance.GetConfigNodes("MAS_MORSE_CODE");
            for (int morseCodeGroupIdx = 0; morseCodeGroupIdx < morseCodeNode.Length; ++morseCodeGroupIdx)
            {
                int numEntries = morseCodeNode[morseCodeGroupIdx].CountValues;
                for (int entry = 0; entry < numEntries; ++entry)
                {
                    ConfigNode.Value val = morseCodeNode[morseCodeGroupIdx].values[entry];
                    if (val.name.Length > 1)
                    {
                        WBICameraUtility.LogError(this, "Got an invalid morse code of '{0}'", val.name);
                    }
                    else
                    {
                        AudioClip clip = GameDatabase.Instance.GetAudioClip(val.value);
                        if (clip == null)
                        {
                            WBICameraUtility.LogError(this, "Could not load audio clip {0} for morse code '{1}'", val.value, val.name);
                        }
                        else
                        {
                            morseCode[val.value[0]] = clip;
                        }
                    }
                }
            }

            pages.Clear();
            ConfigNode[] pageNode = GameDatabase.Instance.GetConfigNodes("MAS_PAGE");
            for (int pageIdx = 0; pageIdx < pageNode.Length; ++pageIdx)
            {
                string pageName = string.Empty;
                if (pageNode[pageIdx].TryGetValue("name", ref pageName))
                {
                    pageName = pageName.Trim();
                    pages.Add(pageName, pageNode[pageIdx]);
                    WBICameraUtility.LogMessage(this, "Found MAS_PAGE \"{0}\"", pageName);
                }
            }

            subPages.Clear();
            ConfigNode[] subPageNode = GameDatabase.Instance.GetConfigNodes("MAS_SUB_PAGE");
            for (int subPageIdx = 0; subPageIdx < subPageNode.Length; ++subPageIdx)
            {
                string subPageName = string.Empty;
                if (subPageNode[subPageIdx].TryGetValue("name", ref subPageName))
                {
                    subPageName = subPageName.Trim();

                    List<ConfigNode> subPageNodes = new List<ConfigNode>();
                    ConfigNode[] nodes = subPageNode[subPageIdx].GetNodes();

                    subPageNodes.AddRange(nodes);

                    subPages.Add(subPageName, subPageNodes);
                    WBICameraUtility.LogMessage(this, "Found MAS_SUB_PAGE \"{0}\"", subPageName);
                }
                else
                {
                    WBICameraUtility.LogError(this, "Found a MAS_SUB_PAGE missing 'name'.  Skipping.");
                }
            }
        }

    }
}
