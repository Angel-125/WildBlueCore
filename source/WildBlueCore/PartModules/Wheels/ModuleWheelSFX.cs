using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ModuleWheels;

namespace WildBlueCore
{
    /// <summary>
    /// This part module adds sound effects to wheels when their motors are engaged. Effects are defined via the standard EFFECT config node.
    /// </summary>
    /// <example>
    /// <code>
    /// MODULE
    /// {
    ///     name = ModuleWheelSFX
    ///     runningEffect = running
    ///     revTime = 0.05
    /// }
    /// </code>
    /// </example>
    public class ModuleWheelSFX: BasePartModule
    {
        #region Fields
        /// <summary>
        /// The name of the effect to play when the wheel is running (motors are producing torque).
        /// </summary>
        [KSPField]
        public string runningEffect = string.Empty;

        /// <summary>
        /// How quickly, in %, to play the effect from 0 (fully off) to 1 (fully on)
        /// </summary>
        [KSPField]
        public float revTime = 0.05f;
        #endregion

        #region Housekeeping
        ModuleWheelMotor wheelMotor;
        float runningPowerLevel = 0f;
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            wheelMotor = part.FindModuleImplementing<ModuleWheelMotor>();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            if (wheelMotor == null || (wheelMotor.state != ModuleWheelMotor.MotorState.Running && wheelMotor.state != ModuleWheelMotor.MotorState.Idle))
            {
                part.Effect(runningEffect, 0f);
                return;
            }

            if (wheelMotor.state == ModuleWheelMotor.MotorState.Running)
            {
                runningPowerLevel = Mathf.Lerp(runningPowerLevel, 1, revTime);
                if (runningPowerLevel > 0.99f)
                    runningPowerLevel = 1f;
                part.Effect(runningEffect, runningPowerLevel);
            }
            else if (wheelMotor.state == ModuleWheelMotor.MotorState.Idle)
            {
                // Now back the power level down to 0.
                runningPowerLevel = Mathf.Lerp(runningPowerLevel, 0, revTime);
                if (runningPowerLevel < 0.001)
                    runningPowerLevel = 0f;
            }
            part.Effect(runningEffect, runningPowerLevel);
        }
        #endregion
    }
}
