﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using WildBlueCore.PartModules.IVA;

namespace WildBlueCore.Utilities
{
    public class TriggerClickWatcher : MonoBehaviour
    {
        #region Events
        /// <summary>
        /// Tells listeners the trigger was clicked.
        /// </summary>
        public static EventData<InternalBaseModule> onTriggerClicked = new EventData<InternalBaseModule>("onTriggerClicked");

        /// <summary>
        /// Tells listeners the trigger was clicked.
        /// </summary>
        public static EventData<InternalBaseModule> onMouseDown = new EventData<InternalBaseModule>("onMouseDown");
        #endregion

        #region Fields
        public InternalBaseModule internalBaseModule = null;
        #endregion

        #region Housekeeping
        protected bool mouseDown;
        #endregion

        #region Event Handlers
        public void OnMouseDown()
        {
            mouseDown = true;
            onMouseDown.Fire(internalBaseModule);
        }

        public void OnMouseUp()
        {
            if (mouseDown)
            {
                mouseDown = false;
                onTriggerClicked.Fire(internalBaseModule);
            }
        }
        #endregion
    }
}
