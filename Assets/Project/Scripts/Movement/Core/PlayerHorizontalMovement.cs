using UnityEngine;

namespace ModularMovement
{
    // Changes the selected target speed gradually, regardless of which ability supplied it.
    [DisallowMultipleComponent, AddComponentMenu("")]
    public sealed class PlayerHorizontalMovement : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float acceleration = 25f;
        [SerializeField, Min(0f)] private float deceleration = 35f;
        [SerializeField, HideInInspector] private int legacySettingsPriority = int.MaxValue;

        public float Acceleration { get => acceleration; set => acceleration = NonNegative(value, 25f); }
        public float Deceleration { get => deceleration; set => deceleration = NonNegative(value, 35f); }

        // Start from the speed actually reached after collisions, not an ability's requested speed.
        internal Vector3 CalculateVelocity(Vector3 current, Vector3 target, float deltaTime)
        {
            current.y = target.y = 0f;
            if (deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
                return current;
            // Disabling this component bypasses smoothing without disabling movement.
            if (!isActiveAndEnabled)
                return target;
            // A negative dot product means the new direction points back against the current movement.
            if (Vector3.Dot(current, target) < 0f)
            {
                // Brake first, then use the remaining time to accelerate in the new direction.
                float brakingTime = deceleration > 0f ? current.magnitude / deceleration : 0f;
                return brakingTime >= deltaTime
                    ? Approach(current, Vector3.zero, deceleration, deltaTime)
                    : Approach(Vector3.zero, target, acceleration, deltaTime - brakingTime);
            }
            float rate = target.sqrMagnitude < current.sqrMagnitude ? deceleration : acceleration;
            return Approach(current, target, rate, deltaTime);
        }

        internal void ApplyLegacySettings(int priority, float oldAcceleration, float oldDeceleration)
        {
            // Old settings with a smaller priority win, even if they are copied later.
            if (priority >= legacySettingsPriority) return;
            if (oldAcceleration >= 0f) Acceleration = oldAcceleration;
            if (oldDeceleration >= 0f) Deceleration = oldDeceleration;
            legacySettingsPriority = priority;
        }

        private void OnValidate()
        {
            Acceleration = acceleration;
            Deceleration = deceleration;
        }

        // A zero rate means an instant speed change, rather than disabling movement.
        private static Vector3 Approach(Vector3 current, Vector3 target, float rate, float deltaTime)
            => rate > 0f ? Vector3.MoveTowards(current, target, rate * deltaTime) : target;
        private static float NonNegative(float value, float fallback)
            => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Max(0f, value);
    }
}
