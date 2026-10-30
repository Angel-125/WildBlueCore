using UnityEngine;

namespace WildBlueCore.PartModules.Aero
{
    // Kept independent of vessel state so reference frames and torque limits can
    // be checked without running KSP. All angular rates are radians/second here.
    internal static class ParafoilAttitudeMath
    {
        internal static float UpdateLiftAuthority(float authority, float errorDegrees, float rateDegrees,
            float deltaTime, float rampTime, float recoveryAuthority)
        {
            float floor = Mathf.Clamp01(recoveryAuthority);
            float step = rampTime <= 0f ? 1f : Mathf.Max(0f, deltaTime) / rampTime;
            if (errorDegrees > 60f || rateDegrees > 90f)
                return Mathf.MoveTowards(authority, floor, authority > floor ? 2f * step : step);
            if (errorDegrees < 30f && rateDegrees < 30f)
                return Mathf.MoveTowards(authority, 1f, step);
            return authority < floor ? Mathf.MoveTowards(authority, floor, step) : Mathf.Clamp01(authority);
        }

        // Integrate aerodynamic trim separately from the capped desired rotation
        // rate. Accept changes that stay within actuator authority OR unwind an
        // already saturated command. Freezing all integration at saturation can
        // otherwise retain an old correction when the disturbance reverses.
        internal static Vector3 IntegrateTrim(Vector3 previous, Vector3 rateError, float deltaTime,
            float gain, float limit, Vector3 untrimmedTorque, Quaternion referenceRotation,
            Vector3 inertia, float maximumTorque)
        {
            Vector3 candidate = Vector3.ClampMagnitude(previous + rateError *
                (Mathf.Max(0f, gain) * Mathf.Max(0f, deltaTime)), Mathf.Max(0f, limit));
            Vector3 oldTorque = untrimmedTorque + referenceRotation * Vector3.Scale(inertia,
                Quaternion.Inverse(referenceRotation) * previous);
            Vector3 newTorque = untrimmedTorque + referenceRotation * Vector3.Scale(inertia,
                Quaternion.Inverse(referenceRotation) * candidate);
            if (!IsFinite(candidate) || !IsFinite(newTorque))
                return previous;
            float cap = Mathf.Max(0f, maximumTorque);
            return newTorque.sqrMagnitude <= cap * cap || newTorque.sqrMagnitude < oldTorque.sqrMagnitude
                ? candidate : previous;
        }

        internal static bool IsFinite(Vector3 value)
        {
            return !(float.IsNaN(value.x) || float.IsInfinity(value.x) ||
                float.IsNaN(value.y) || float.IsInfinity(value.y) ||
                float.IsNaN(value.z) || float.IsInfinity(value.z));
        }

        internal static Vector3 RotationError(Quaternion current, Quaternion target)
        {
            Quaternion error = target * Quaternion.Inverse(current);
            // q and -q describe the same attitude. Always take the short rotation.
            if (error.w < 0f)
                error = new Quaternion(-error.x, -error.y, -error.z, -error.w);
            float angle;
            Vector3 axis;
            error.ToAngleAxis(out angle, out axis);
            return IsFinite(axis) && angle > 0.001f ? axis.normalized * (angle * Mathf.Deg2Rad) : Vector3.zero;
        }

        internal static bool TryTargetRotation(Vector3 heading, Vector3 up, Vector3 localForward,
            Vector3 localUp, float pitchDegrees, float bankDegrees, out Quaternion target)
        {
            target = Quaternion.identity;
            heading = Vector3.ProjectOnPlane(heading, up).normalized;
            localUp = Vector3.ProjectOnPlane(localUp, localForward).normalized;
            if (!IsFinite(heading) || !IsFinite(up) || !IsFinite(localForward) || !IsFinite(localUp) ||
                heading.sqrMagnitude < 0.5f || up.sqrMagnitude < 0.5f ||
                localForward.sqrMagnitude < 0.000001f || localUp.sqrMagnitude < 0.5f)
                return false;

            Vector3 right = Vector3.Cross(up, heading).normalized;
            Quaternion pitch = Quaternion.AngleAxis(-pitchDegrees, right);
            Vector3 nose = pitch * heading;
            Vector3 normal = Quaternion.AngleAxis(-bankDegrees, nose) * (pitch * up);
            target = Quaternion.LookRotation(nose, normal) *
                Quaternion.Inverse(Quaternion.LookRotation(localForward, localUp));
            return true;
        }

        internal static Vector3 CalculateTorque(Vector3 error, Vector3 worldAngularVelocity,
            Vector3 feedForwardRate, Quaternion referenceRotation, Vector3 inertia,
            float response, float damping, float maximumCorrectionRate, Vector3 integralAcceleration,
            float maximumTorque)
        {
            Vector3 desiredRate = DesiredRate(error, feedForwardRate, response, maximumCorrectionRate);
            Vector3 acceleration = (desiredRate - worldAngularVelocity) * Mathf.Max(0f, damping) + integralAcceleration;
            Vector3 localAcceleration = Quaternion.Inverse(referenceRotation) * acceleration;
            Vector3 torque = referenceRotation * Vector3.Scale(inertia, localAcceleration);
            return IsFinite(torque) ? Vector3.ClampMagnitude(torque, Mathf.Max(0f, maximumTorque)) : Vector3.zero;
        }

        internal static Vector3 DesiredRate(Vector3 error, Vector3 feedForwardRate, float response, float maximumCorrectionRate)
        {
            return Vector3.ClampMagnitude(error * Mathf.Max(0f, response),
                Mathf.Max(0f, maximumCorrectionRate)) + feedForwardRate;
        }
    }
}
