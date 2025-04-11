using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using WildBlueCore.Utilities;

namespace WildBlueCore.PartModules.IVA
{
    public class WBIInternalModuleAnimation: WBIInternalBaseModule
    {
        #region Game Events
        public static EventData<InternalProp, bool> onAnimationPlayed = new EventData<InternalProp, bool>("onAnimationPlayed");
        public static EventData<InternalProp, bool> onAnimationFinished = new EventData<InternalProp, bool>("onAnimationFinished");
        #endregion

        #region Fields
        protected const int kDefaultAnimationLayer = 2;

        [KSPField]
        public int animationLayer = kDefaultAnimationLayer;

        [KSPField]
        public string animationName;

        [KSPField]
        public string animationTriggerName = string.Empty;
        #endregion

        #region Housekeeping
        public Animation animation = null;
        protected AnimationState animationState;
        bool isMoving = false;
        bool playInReverse = true;
        #endregion

        #region Overrides
        public override void OnStart()
        {
            if (HighLogic.LoadedSceneIsFlight == false)
                return;
            Animation[] animations = internalProp.FindModelAnimators(animationName);
            if (animations == null || animations.Length <= 0)
            {
                Debug.Log("[WBIInternalModuleAnimation] - Could not find " + animationName);
                return;
            }

            // Get the animation
            animation = animations[0];
            animationState = animation[animationName];
            animationState.wrapMode = WrapMode.Once;
            animation[animationName].layer = animationLayer;
            animation[animationName].normalizedTime = 0f;
            animation[animationName].speed = 0f;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (HighLogic.LoadedSceneIsFlight == false)
                return;
            if (animation == null)
                return;

            //Play end
            else if (animation.isPlaying == false && isMoving)
            {
                isMoving = false;
                onAnimationFinished.Fire(internalProp, playInReverse);
            }
        }
        #endregion

        #region Helpers
        protected override void onTriggerClick()
        {
            if (isMoving)
                return;

            playInReverse = !playInReverse;

            float animationSpeed = playInReverse == false ? 1.0f : -1.0f;

            animation[animationName].speed = animationSpeed;

            if (playInReverse)
                animation[animationName].time = animation[animationName].length;

            animation.Play(animationName);

            isMoving = true;

            onAnimationPlayed.Fire(internalProp, playInReverse);
        }
        #endregion
    }
}
