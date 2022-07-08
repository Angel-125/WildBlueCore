﻿using System;
using System.Linq;
using System.Reflection;

/*
Source code copyright 2021, by Michael Billard (Angel-125)
License: GPLV3

Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueCore.Wrappers
{
    public class ModuleWaterfallFXWrapper
    {
        static Type typeModuleWaterfallFX;
        static MethodInfo miSetControllerOverride;
        static MethodInfo miSetControllerOverrideValue;
        static MethodInfo miSetControllerValue;

        public PartModule partModule;

        public static void InitClass(Assembly assembly)
        {
            typeModuleWaterfallFX = assembly.GetTypes().First(t => t.Name.Equals("ModuleWaterfallFX"));

            miSetControllerOverride = typeModuleWaterfallFX.GetMethod("SetControllerOverride", new[] { typeof(string), typeof(bool) });
            miSetControllerOverrideValue = typeModuleWaterfallFX.GetMethod("SetControllerOverrideValue", new[] { typeof(string), typeof(float) });
            miSetControllerValue = typeModuleWaterfallFX.GetMethod("SetControllerValue", new[] { typeof(string), typeof(float) });
        }

        /// <summary>
        /// Attempts to obtain the Waterfall FX module from the supplied part.
        /// </summary>
        /// <param name="part">A Part that might contain a waterfall fx module</param>
        /// <returns>A WFModuleWaterfallFX if the part has a waterfall module, or null if not.</returns>
        public static ModuleWaterfallFXWrapper GetWaterfallModule(Part part)
        {
            {
                int count = part.Modules.Count;
                PartModule module;

                for (int index = 0; index < count; index++)
                {
                    module = part.Modules[index];
                    if (module.moduleName == "ModuleWaterfallFX")
                    {
                        ModuleWaterfallFXWrapper waterfallFXModule = new ModuleWaterfallFXWrapper(module);
                        return waterfallFXModule;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Instantiates a new ModuleWaterfallFXWrapper
        /// </summary>
        /// <param name="module">The PartModule representing the FX module.</param>
        public ModuleWaterfallFXWrapper(PartModule module)
        {
            if (WaterfallFactoryWrapper.waterfallAssembly == null)
                WaterfallFactoryWrapper.Init();

            partModule = module;
        }

        /// <summary>
        /// Sets the override state for the specified controller.
        /// </summary>
        /// <param name="controllerName">A string containing the name of the controller to override.</param>
        /// <param name="overriden">A bool indicating whether or not to override the controller.</param>
        public void SetControllerOverride(string controllerName, bool overriden = true)
        {
            if (miSetControllerOverride == null || string.IsNullOrEmpty(controllerName))
                return;

            miSetControllerOverride.Invoke(partModule, new object[] { controllerName, overriden });
        }

        /// <summary>
        /// Sets the override value for the specified controller
        /// </summary>
        /// <param name="controllerName">A string containing the name of the controller to override.</param>
        /// <param name="value">A float containing the override value.</param>
        public void SetControllerOverrideValue(string controllerName, float value)
        {
            if (miSetControllerOverrideValue == null || string.IsNullOrEmpty(controllerName))
                return;

            SetControllerOverride(controllerName);
            miSetControllerOverrideValue.Invoke(partModule, new object[] { controllerName, value });
        }

        /// <summary>
        /// Sets the value for the specified controller
        /// </summary>
        /// <param name="controllerName">A string containing the name of the controller to override.</param>
        /// <param name="value">A float containing the override value.</param>
        public void SetControllerValue(string controllerName, float value)
        {
            if (miSetControllerValue == null || string.IsNullOrEmpty(controllerName))
                return;

            miSetControllerValue.Invoke(partModule, new object[] { controllerName, value });
        }
    }
}
