using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KSP.Localization;

namespace WildBlueCore.PartModules.IVA
{
    /// <summary>
    /// This module lets users rotate a seat in a part's IVA if the seat is occupied.
    /// </summary>
    /// <example>
    /// <code>
    /// MODULE
    /// {
    ///     name = ModuleSeatRotator
    ///     
    ///     // Name of the seat transform to rotate. This needs to be the same name as in the IVA's 3D model and in the IVA's config file.
    ///     seatName = Seat001
    ///     
    ///     // The name of the prop that the kerbal sits on. This is optional.
    ///     propName = NF_SEAT_Chair_Basic
    ///     
    ///     // If your list of props has more than one prop for the seats, then specify the index of the seat prop.
    ///     propIndex = 2
    ///     
    ///     // The x, y, and z axis to rotate the prop by. The default is 0,0,1
    ///     propRotationAxis = 0,1,0
    /// }
    /// </code>
    /// </example>
    public class ModuleSeatRotator: BasePartModule
    {
        #region Fields
        /// <summary>
        /// Name of the seat transform to rotate. This needs to be the same name as in the IVA's 3D model and in the IVA's config file.
        /// If you use a prop in addition to the seat transform, be sure to specify the propName and propIndex as well.
        /// </summary>
        [KSPField]
        public string seatName = "Seat001";

        /// <summary>
        /// The name of the prop that the kerbals sit on. If the seat transform in your IVA's 3D model is NOT the same thing as the seat prop, then
        /// specify the propName as wel as the propIndex in order to rotate the prop along with the seat transform.
        /// </summary>
        [KSPField]
        public string propName = string.Empty;

        /// <summary>
        /// The x, y, and z axis to rotate the prop by. The default is 0,0,1
        /// </summary>
        [KSPField]
        public string propRotationAxis = string.Empty;

        /// <summary>
        /// If your list of props has more than one prop for the seats, then specify the index of the seat prop (as it appears in order in the config file) to rotate.
        /// </summary>
        [KSPField]
        public int propIndex = 0;

        /// <summary>
        /// Rate at which to rotate the seat, in degrees per second.
        /// </summary>
        [KSPField]
        public float rotationRate = 15f;

        /// <summary>
        /// How far to rotate the seat when commanded to rotate the seat
        /// </summary>
        [KSPField]
        public float rotationAmount = 90f;
        #endregion

        #region Housekeeping
        [KSPField]
        float currentAngle = -1f;
        [KSPField(isPersistant = true)]
        float targetAngle = -1f;

        InternalSeat internalSeat = null;
        InternalProp internalProp = null;
        bool isRotating = false;
        bool isRotatingLeft = false;
        Vector3 eulerAngles = Vector3.zero;
        Vector3 eulerAnglesProp = Vector3.zero;
        Vector3 propRotAxis = Vector3.zero;
        string rotateLeftString = string.Empty;
        string rotateRightString = string.Empty;
        #endregion

        #region Overrides
        protected void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || internalSeat == null || !isRotating)
                return;

            float rotation = (isRotatingLeft ? -rotationRate : rotationRate) * TimeWarp.fixedDeltaTime;
            currentAngle += rotation;
            float rotationAngle = normalizeAngle(currentAngle);
            float normalizedTargetAngle = normalizeAngle(targetAngle);

            if ((currentAngle <= targetAngle && isRotatingLeft) || (currentAngle >= targetAngle && !isRotatingLeft))
            {
                rotationAngle = normalizedTargetAngle;
                isRotating = false;
            }

            if (propRotAxis.x != 0)
                eulerAngles.x = rotationAngle * propRotAxis.x;
            if (propRotAxis.y != 0)
                eulerAngles.y = rotationAngle * propRotAxis.y;
            if (propRotAxis.z != 0)
                eulerAngles.z = rotationAngle * propRotAxis.z;
            internalSeat.seatTransform.localEulerAngles = eulerAngles;

            if (internalProp != null)
            {
                if (propRotAxis.x != 0)
                    eulerAnglesProp.x = rotation * propRotAxis.x;
                if (propRotAxis.y != 0)
                    eulerAnglesProp.y = rotation * propRotAxis.y;
                if (propRotAxis.z != 0)
                    eulerAnglesProp.z = rotation * propRotAxis.z;

                internalProp.transform.localEulerAngles += eulerAnglesProp;
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (!HighLogic.LoadedSceneIsFlight || internalSeat == null)
                return;

            // Check for occupants. If none then hide the rotation controls. Show them if we have an occupant.
            Events["RotateLeft"].active = internalSeat.taken;
            Events["RotateRight"].active = internalSeat.taken;

            if (internalSeat.taken)
            {
                Events["RotateLeft"].guiName = internalSeat.crew.displayName + rotateLeftString;
                Events["RotateRight"].guiName = internalSeat.crew.displayName + rotateRightString;
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            findSeat();
            if (internalSeat != null)
            {
                eulerAngles.x = normalizeAngle(targetAngle);
                internalSeat.seatTransform.localEulerAngles = eulerAngles;
            }

            Fields["currentAngle"].guiActive = debugMode;
            Fields["targetAngle"].guiActive = debugMode;

            rotateLeftString = Localizer.Format("#LOC_WILDBLUECORE_rotateLeft");
            rotateRightString = Localizer.Format("#LOC_WILDBLUECORE_rotateRight");
            Actions["ActionRotateLeft"].guiName = seatName + rotateLeftString;
            Actions["ActionRotateRight"].guiName = seatName + rotateRightString;

            ModuleSeatChanger.onSeatsReassigned.Add(onSeatsReassigned);
        }

        public void OnDestroy()
        {
            ModuleSeatChanger.onSeatsReassigned.Remove(onSeatsReassigned);
        }
        #endregion

        #region Actions
        [KSPAction("#LOC_WILDBLUECORE_rotateLeft")]
        public void ActionRotateLeft(KSPActionParam param)
        {
            RotateLeft();
        }

        [KSPAction("#LOC_WILDBLUECORE_rotateRight")]
        public void ActionRotateRight(KSPActionParam param)
        {
            RotateRight();
        }
        #endregion

        #region Events
        /// <summary>
        /// Rotates the seat to the left.
        /// </summary>
        [KSPEvent(guiActive = true, guiName = "Rotate Left", groupName = "#LOC_WILDBLUECORE_seatsGroupName", groupDisplayName = "#LOC_WILDBLUECORE_seatsGroupName")]
        public void RotateLeft()
        {
            if (isRotating)
                return;

            targetAngle = (currentAngle + rotationAmount) % 360f;
            isRotating = true;
            isRotatingLeft = false;
        }

        /// <summary>
        /// Rotates the seat to the right.
        /// </summary>
        [KSPEvent(guiActive = true, guiName = "Rotate right", groupName = "#LOC_WILDBLUECORE_seatsGroupName", groupDisplayName = "#LOC_WILDBLUECORE_seatsGroupName")]
        public void RotateRight()
        {
            if (isRotating)
                return;

            targetAngle = currentAngle - rotationAmount;
            isRotating = true;
            isRotatingLeft = true;
        }
        #endregion

        #region Helpers
        float normalizeAngle(float angle)
        {
            if (angle > 0)
                return angle % 360f;
            else
                return 360f + angle;
        }

        void findSeat()
        {
            InternalSeat seat;
            int count = part.internalModel.seats.Count;

            for (int index = 0; index < count; index++)
            {
                seat = part.internalModel.seats[index];
                if (seat.seatTransformName == seatName)
                {
                    internalSeat = seat;
                    eulerAngles = seat.seatTransform.localEulerAngles;

                    // Now get the rotation axis
                    if (!string.IsNullOrEmpty(propRotationAxis))
                    {
                        propRotAxis = Vector3.zero;

                        string[] axis = propRotationAxis.Split(new char[] { ',' });
                        float value = 0f;
                        if (axis.Length >= 3)
                        {
                            if (float.TryParse(axis[0], out value) && value != 0)
                            {
                                propRotAxis.x = value;
                                currentAngle = seat.seatTransform.localEulerAngles.x;
                            }
                            value = 0f;
                            if (float.TryParse(axis[1], out value) && value != 0)
                            {
                                propRotAxis.y = value;
                                currentAngle = seat.seatTransform.localEulerAngles.y;
                            }
                            value = 0f;
                            if (float.TryParse(axis[2], out value) && value != 0)
                            {
                                propRotAxis.z = value;
                                currentAngle = seat.seatTransform.localEulerAngles.z;
                            }
                        }
                        else
                        {
                            propRotAxis = new Vector3(0, 0, 1);
                        }
                    }

                    // Now look for the prop, if any.
                    findSeatProp();
                    return;
                }
            }
        }

        void findSeatProp()
        {
            if (string.IsNullOrEmpty(propName))
                return;

            int count = part.internalModel.props.Count;
            InternalProp prop;
            List<InternalProp> props = new List<InternalProp>();
            for (int index = 0; index < count; index++)
            {
                prop = part.internalModel.props[index];
                if (prop.propName == propName)
                    props.Add(prop);
            }

            count = props.Count;
            if (count > 0 && propIndex <= count - 1)
            {
                internalProp = props[propIndex];
                eulerAnglesProp = internalProp.transform.localEulerAngles;
            }
        }

        void onSeatsReassigned(ModuleSeatChanger seatChanger)
        {
            if (seatChanger.part != part)
                return;
            findSeat();
        }
        #endregion
    }
}
