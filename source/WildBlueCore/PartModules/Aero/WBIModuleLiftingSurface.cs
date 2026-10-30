using UnityEngine;

namespace WildBlueCore.PartModules.Aero
{
    /// <summary>
    /// Stock lifting-surface curves with independent orientation, velocity sampling,
    /// and force application. ModuleParafoil can manage this module as a primary wing.
    /// </summary>
    public class WBIModuleLiftingSurface : ModuleLiftingSurface, ILiftProvider
    {
        /// <summary>Follow the named transform's rotation; otherwise capture its part-relative rotation at OnStart.</summary>
        [KSPField]
        public bool followTransformRotation = false;

        /// <summary>Additional Euler rotation in degrees relative to the selected aerodynamic reference.</summary>
        [KSPField]
        public Vector3 orientationOffset = Vector3.zero;

        /// <summary>Airflow sampling position in part-local coordinates, independent of transformName.</summary>
        [KSPField]
        public Vector3 velocitySampleOffset = Vector3.zero;

        /// <summary>Use the part's CoLOffset instead of liftForceOffset.</summary>
        [KSPField]
        public bool usePartLiftOffset = true;

        /// <summary>Lift application position in part-local coordinates when usePartLiftOffset is false.</summary>
        [KSPField]
        public Vector3 liftForceOffset = Vector3.zero;

        /// <summary>Use the part's CoPOffset instead of dragForceOffset.</summary>
        [KSPField]
        public bool usePartDragOffset = true;

        /// <summary>Drag application position in part-local coordinates when usePartDragOffset is false.</summary>
        [KSPField]
        public Vector3 dragForceOffset = Vector3.zero;

        /// <summary>Log observations of this module's force calculation.</summary>
        [KSPField]
        public bool debugMode = false;

        /// <summary>Seconds between diagnostic lines; zero logs every physics update.</summary>
        [KSPField]
        public float debugLogInterval = 0.25f;

        Quaternion referenceLocalRotation;
        bool initialized;
        float nextDebugLogTime;

        /// <summary>Load stock curves and capture the selected orientation relative to the part.</summary>
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            referenceLocalRotation = Quaternion.Inverse(part.partTransform.rotation) * baseTransform.rotation;
            initialized = true;

            if (!string.IsNullOrEmpty(transformName) && part.FindModelTransform(transformName) == null)
                Debug.LogWarning("[WBIModuleLiftingSurface] - Missing orientation transform '" + transformName + "'; using part root.");

            if (displaceVelocity || velocityOffset != Vector3.zero)
                Debug.LogWarning("[WBIModuleLiftingSurface] - Use velocitySampleOffset in part-local coordinates; stock displaceVelocity/velocityOffset are not used.");
        }

        /// <summary>
        /// Own the Unity physics message: never call base.FixedUpdate, which would
        /// apply a second set of forces at the stock positions.
        /// </summary>
        public new void FixedUpdate()
        {
            if (!initialized || !HighLogic.LoadedSceneIsFlight || part == null)
                return;

            liftField.guiActive = PhysicsGlobals.AeroDataDisplay;
            dragField.guiActive = PhysicsGlobals.AeroDataDisplay && useInternalDragModel;

            Vector3 samplePosition = part.partTransform.TransformPoint(velocitySampleOffset);
            Vector3 liftPosition = part.partTransform.TransformPoint(usePartLiftOffset ? part.CoLOffset : liftForceOffset);
            Vector3 dragPosition = part.partTransform.TransformPoint(usePartDragOffset ? part.CoPOffset : dragForceOffset);
            ResetForces();

            bool active = part.Rigidbody != null && !part.packed &&
                !part.ShieldedFromAirstream && deflectionLiftCoeff != 0f;
            if (active)
            {
                pointVelocity = part.Rigidbody.GetPointVelocity(samplePosition) + Krakensbane.GetFrameVelocityV3f();
                active = IsFinite(pointVelocity) && pointVelocity.sqrMagnitude > 0f;
            }

            if (active)
            {
                double submerged = part.submergedPortion;
                Qlift = (part.dynamicPressurekPa * (1.0 - submerged) +
                    part.submergedDynamicPressurekPa * submerged * part.submergedLiftScalar) * 1000.0;
                Qdrag = (part.dynamicPressurekPa * (1.0 - submerged) +
                    part.submergedDynamicPressurekPa * submerged * part.submergedDragScalar) * 1000.0;

                SetupIndependentCoefficients(pointVelocity);
                liftForce = ApplyLiftForceAdjustments(GetLiftVector(liftVector, liftDot, absDot, Qlift, (float)part.machNumber));
                if (useInternalDragModel)
                    dragForce = GetDragVector(nVel, absDot, Qdrag);

                // Reject invalid curve/adjuster output before it reaches the physics engine.
                if (IsFinite(liftForce) && IsFinite(dragForce))
                {
                    part.AddForceAtPosition((Vector3d)liftForce, (Vector3d)liftPosition);
                    if (useInternalDragModel)
                        part.AddForceAtPosition((Vector3d)dragForce, (Vector3d)dragPosition);
                }
                else
                {
                    ResetForces();
                    active = false;
                }
            }

            UpdateForceArrows(liftPosition, dragPosition);
            LogDiagnostics(active, samplePosition, liftPosition, dragPosition);
        }

        /// <summary>Use the same orientation and lift application position for editor/ILiftProvider queries.</summary>
        public new void OnCenterOfLiftQuery(CenterOfLiftQuery qry)
        {
            if (!initialized || part == null)
                return;

            qry.pos = part.partTransform.TransformPoint(usePartLiftOffset ? part.CoLOffset : liftForceOffset);
            qry.dir = Vector3.zero;
            qry.lift = 0f;
            if (part.ShieldedFromAirstream || !IsFinite(qry.refVector) || qry.refVector.sqrMagnitude == 0f)
                return;

            // Stock GetLiftVector uses nVel for perpendicularOnly. Restore the flight
            // state after the query so an editor/CoL query cannot corrupt diagnostics.
            Vector3 savedVelocity = nVel;
            float savedScalar = liftScalar;
            float savedDot = liftDot;
            float savedAbsDot = absDot;
            Vector3 savedNormal = liftVector;
            SetupIndependentCoefficients(qry.refVector);
            CelestialBody body = vessel != null ? vessel.mainBody : FlightGlobals.GetHomeBody();
            double speedOfSound = body.GetSpeedOfSound(qry.refStaticPressure, qry.refAirDensity);
            float mach = speedOfSound > 0.0 ? (float)(qry.refVector.magnitude / speedOfSound) : 0f;
            double pressure = 0.5 * qry.refAirDensity * qry.refVector.sqrMagnitude;
            Vector3 force = GetLiftVector(liftVector, liftDot, absDot, pressure, mach);
            if (IsFinite(force))
            {
                qry.lift = force.magnitude;
                qry.dir = force.normalized;
            }
            nVel = savedVelocity;
            liftScalar = savedScalar;
            liftDot = savedDot;
            absDot = savedAbsDot;
            liftVector = savedNormal;
        }

        void SetupIndependentCoefficients(Vector3 velocity)
        {
            Quaternion rotation = followTransformRotation
                ? baseTransform.rotation
                : part.partTransform.rotation * referenceLocalRotation;
            rotation *= Quaternion.Euler(orientationOffset);
            Vector3 axis = transformDir == TransformDir.Z ? Vector3.forward :
                (transformDir == TransformDir.Y ? Vector3.up : Vector3.right);
            liftVector = rotation * axis * transformSign;
            nVel = velocity.normalized;
            liftDot = Vector3.Dot(nVel, liftVector);
            absDot = omnidirectional ? Mathf.Abs(liftDot) : Mathf.Clamp01(liftDot);
        }

        void ResetForces()
        {
            liftForce = dragForce = pointVelocity = nVel = liftVector = Vector3.zero;
            liftScalar = dragScalar = liftDot = absDot = 0f;
            Qlift = Qdrag = 0.0;
        }

        static bool IsFinite(Vector3 value)
        {
            return !(float.IsNaN(value.x) || float.IsInfinity(value.x) ||
                float.IsNaN(value.y) || float.IsInfinity(value.y) ||
                float.IsNaN(value.z) || float.IsInfinity(value.z));
        }

        void UpdateForceArrows(Vector3 liftPosition, Vector3 dragPosition)
        {
            if (!PhysicsGlobals.AeroForceDisplay)
            {
                DestroyArrow(ref liftArrow);
                DestroyArrow(ref dragArrow);
                return;
            }

            UpdateArrow(ref liftArrow, liftPosition, liftForce, Color.blue);
            if (useInternalDragModel)
                UpdateArrow(ref dragArrow, dragPosition, dragForce, Color.red);
            else
                DestroyArrow(ref dragArrow);
        }

        void UpdateArrow(ref ArrowPointer arrow, Vector3 position, Vector3 force, Color color)
        {
            Vector3 offset = part.partTransform.InverseTransformPoint(position);
            float length = force.magnitude * PhysicsGlobals.AeroForceDisplayScale;
            if (arrow == null)
                arrow = ArrowPointer.Create(part.partTransform, offset, force, length, color, true);
            arrow.Offset = offset;
            arrow.Direction = force;
            arrow.Length = length;
        }

        static void DestroyArrow(ref ArrowPointer arrow)
        {
            if (arrow != null)
                Object.Destroy(arrow.gameObject);
            arrow = null;
        }

        void OnDestroy()
        {
            DestroyArrow(ref liftArrow);
            DestroyArrow(ref dragArrow);
        }

        void LogDiagnostics(bool active, Vector3 samplePosition, Vector3 liftPosition, Vector3 dragPosition)
        {
            if (!debugMode || (debugLogInterval > 0f && Time.realtimeSinceStartup < nextDebugLogTime))
                return;
            nextDebugLogTime = Time.realtimeSinceStartup + Mathf.Max(0f, debugLogInterval);
            Vector3 rootVelocity = part.Rigidbody != null
                ? part.Rigidbody.GetPointVelocity(part.partTransform.position) + Krakensbane.GetFrameVelocityV3f()
                : Vector3.zero;
            Vector3 center = vessel != null ? (Vector3)vessel.CurrentCoM : part.partTransform.position;
            Vector3 moment = Vector3.Cross(liftPosition - center, liftForce) + Vector3.Cross(dragPosition - center, dragForce);
            Debug.Log(string.Format(
                "[WBIModuleLiftingSurface] - fixedTime={0:F4} part={1} active={2} reference={3} followRotation={4}" +
                " sample={5} pointVelocity={6} rootVelocity={7} normal={8} liftDot={9:F5} curveInput={10:F5}" +
                " coeff={11:F4} Qlift={12:F4} Qdrag={13:F4} mach={14:F4}" +
                " liftForce={15} liftPosition={16} dragForce={17} dragPosition={18} CoM={19} moment={20} angularVelocity={21}",
                Time.fixedTime, part.name, active, baseTransform.name, followTransformRotation,
                samplePosition.ToString("F4"), pointVelocity.ToString("F4"), rootVelocity.ToString("F4"),
                liftVector.ToString("F4"), liftDot, absDot, deflectionLiftCoeff, Qlift, Qdrag, part.machNumber,
                liftForce.ToString("F4"), liftPosition.ToString("F4"), dragForce.ToString("F4"),
                dragPosition.ToString("F4"), center.ToString("F4"), moment.ToString("F4"),
                part.Rigidbody != null ? part.Rigidbody.angularVelocity.ToString("F4") : "<none>"));
        }
    }
}
