using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModularMovement
{
    // Chooses which ability controls the player's sideways and forward movement.
    // It does not move the player; PlayerMotor carries out the selected movement.
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    [RequireComponent(typeof(PlayerContext), typeof(PlayerInputActions), typeof(PlayerMotor))]
    [RequireComponent(typeof(PlayerHorizontalMovement))]
    public sealed class PlayerMovementResolver : MonoBehaviour
    {
        // Abilities such as PlayerWalk send movement requests using the same set of rules.
        private readonly List<IMovementProvider> providers = new List<IMovementProvider>();
        // Use a copy of the ability list, because abilities can turn off during this check.
        private readonly List<IMovementProvider> stepProviders = new List<IMovementProvider>();
        // Remember which ability made each request, so disabled abilities can be skipped.
        private readonly List<KeyValuePair<IMovementProvider, MovementIntent>> candidates = new List<KeyValuePair<IMovementProvider, MovementIntent>>();
        private PlayerContext context;
        private bool isStepping;

        // Other scripts may read this list; Register and Unregister control who can offer movement.
        public IReadOnlyList<IMovementProvider> Providers => providers.AsReadOnly();

        // A module calls this when enabled so its movement proposals can be considered.
        public bool Register(IMovementProvider provider)
        {
            // Accept only components on this player's GameObject, not another player's modules.
            if (!(provider is MonoBehaviour component) || component == null || component.gameObject != gameObject)
            {
                return false;
            }

            // Registering the same module twice must not make it run twice.
            if (providers.Contains(provider))
                return true;

            // None is reserved for steps where no movement source is selected.
            if (string.IsNullOrWhiteSpace(provider.SourceId) || provider.SourceId == "None")
            {
                return false;
            }

            // Give each ability a different name so changes can be reported correctly.
            foreach (IMovementProvider registered in providers)
            {
                if (registered is MonoBehaviour existing && existing != null && registered.SourceId == provider.SourceId)
                {
                    return false;
                }
            }

            providers.Add(provider);
            return true;
        }

        // Disabled or removed modules stop participating without affecting the remaining modules.
        public void Unregister(IMovementProvider provider)
        {
            providers.Remove(provider);
        }

        private void Update()
        {
            // Unity calls Update every frame. One coordinator keeps all movement in the same step.
            Step(Time.deltaTime);
        }

        internal void Step(float deltaTime)
        {
            // Ignore invalid time and prevent another movement step from starting before this one ends.
            if (!isActiveAndEnabled || isStepping || deltaTime <= 0f || !IsFinite(deltaTime))
                return;

            if (context == null)
                context = GetComponent<PlayerContext>();

            // Do not perform a partial move when the required foundation is missing.
            if (context == null || !context.ValidateDependencies())
            {
                return;
            }

            if (!context.Motor.CanMove())
                return;

            isStepping = true;
            try
            {
                // Read the controls once, so every ability uses the same information.
                Vector2 input = context.Input.ReadSnapshot();
                MovementStep step = new MovementStep(deltaTime, input, context.States.ActualVelocity, context.States.IsGrounded);
                MovementIntent intent = Resolve(in step, out IMovementProvider provider, out MovementIntent continuation);
                // Record the chosen source before the motor updates state and sends movement events.
                string previousSource = context.States.ActiveMovementSource;
                string source = intent.IsValid ? intent.SourceId : "None";
                context.States.SetActiveMovementSource(this, source);
                // No valid proposal means zero horizontal velocity; the motor still applies gravity.
                // Adjust speed gradually for the chosen ability, starting from the speed actually reached.
                Vector3 velocity = intent.IsValid
                    ? (intent.UseSharedSmoothing
                        ? context.HorizontalMovement.CalculateVelocity(context.States.ActualVelocity, intent.PlanarVelocity, deltaTime)
                        : intent.PlanarVelocity)
                    : Vector3.zero;
                if (provider is ITimedMovementProvider timedProvider)
                    velocity = CalculateTimedVelocity(timedProvider, in continuation, velocity, deltaTime);
                context.Motor.Move(velocity, deltaTime, previousSource);

                // Notify listeners only on a change, after this step's state is fully updated.
                if (previousSource != source)
                {
                    context.Bus.PublishMovementSourceChanged(previousSource, source);
                }
                if (provider is IMovementSelectionListener listener && provider is MonoBehaviour component
                    && component != null && component.isActiveAndEnabled && providers.Contains(provider))
                {
                    try { listener.OnMovementSelected(); }
                    catch (Exception)
                    {
                        // A notification must not interrupt the completed movement step.
                    }
                }
            }
            finally
            {
                // Allow the next movement step even if this one could not finish.
                isStepping = false;
            }
        }

        private Vector3 CalculateTimedVelocity(ITimedMovementProvider provider, in MovementIntent continuation,
            Vector3 fallbackVelocity, float deltaTime)
        {
            TimedMovement movement;
            try { movement = provider.GetTimedMovement(in continuation); }
            catch (Exception) { return fallbackVelocity; }
            if (!IsFinite(movement.Duration) || movement.Duration <= 0f || movement.Duration > deltaTime
                || !IsPlanarFinite(movement.AverageVelocity) || !IsPlanarFinite(movement.ExitVelocity))
                return fallbackVelocity;
            float remainingTime = deltaTime - movement.Duration;
            if (remainingTime <= 0f) return movement.AverageVelocity;
            Vector3 target = continuation.IsValid ? continuation.PlanarVelocity : Vector3.zero;
            Vector3 nextVelocity = !continuation.IsValid || continuation.UseSharedSmoothing
                ? context.HorizontalMovement.CalculateVelocity(movement.ExitVelocity, target, remainingTime)
                : target;
            // Combine both distances before the single motor move, without losing the end of the frame.
            return (movement.AverageVelocity * movement.Duration + nextVelocity * remainingTime) / deltaTime;
        }

        private MovementIntent Resolve(in MovementStep step, out IMovementProvider selectedProvider,
            out MovementIntent continuation)
        {
            // Reuse the temporary lists, but discard proposals from the previous step.
            MovementIntent selected = MovementIntent.None;
            selectedProvider = null;
            continuation = MovementIntent.None;
            candidates.Clear();
            stepProviders.Clear();
            stepProviders.AddRange(providers);

            foreach (IMovementProvider provider in stepProviders)
            {
                // Remove destroyed modules even if they did not unregister themselves.
                if (!(provider is MonoBehaviour component) || component == null)
                {
                    Unregister(provider);
                    continue;
                }

                if (!component.isActiveAndEnabled || !providers.Contains(provider))
                    continue;

                try
                {
                    if (!provider.IsEligible)
                        continue;

                    // A module may change its own activity while calculating its proposal.
                    MovementIntent candidate = provider.Evaluate(in step);
                    if (!candidate.IsValid || !component.isActiveAndEnabled || !providers.Contains(provider))
                        continue;

                    // Ignore an unusable movement request instead of passing it to PlayerMotor.
                    if (!IsValid(candidate, provider))
                    {
                        continue;
                    }

                    candidates.Add(new KeyValuePair<IMovementProvider, MovementIntent>(provider, candidate));
                }
                catch (Exception)
                {
                    // One broken module must not stop the other modules from offering movement.
                    continue;
                }
            }

            // One ability may turn another off while movement requests are being collected.
            foreach (KeyValuePair<IMovementProvider, MovementIntent> entry in candidates)
            {
                if (!(entry.Key is MonoBehaviour component) || component == null ||
                    !component.isActiveAndEnabled || !providers.Contains(entry.Key))
                    continue;
                try
                {
                    if (!entry.Key.IsEligible || !IsValid(entry.Value, entry.Key))
                        continue;
                    MovementIntent candidate = entry.Value;
                    // Higher priority wins. Equal priorities use source-name order for a stable result.
                    if (Outranks(in candidate, in selected))
                    {
                        continuation = selected;
                        selected = candidate;
                        selectedProvider = entry.Key;
                    }
                    else if (Outranks(in candidate, in continuation))
                        continuation = candidate;
                }
                catch (Exception)
                {
                    // Skip a module that fails during the final safety check.
                    continue;
                }
            }

            return selected;
        }

        private static bool Outranks(in MovementIntent candidate, in MovementIntent other)
            => !other.IsValid || candidate.Priority > other.Priority ||
                (candidate.Priority == other.Priority && string.CompareOrdinal(candidate.SourceId, other.SourceId) < 0);

        private static bool IsPlanarFinite(Vector3 velocity)
            => velocity.y == 0f && IsFinite(velocity.x) && IsFinite(velocity.z);

        private static bool IsValid(MovementIntent intent, IMovementProvider provider)
        {
            // Accept only valid speeds from the named ability; gravity handles up and down movement.
            Vector3 velocity = intent.PlanarVelocity;
            return !string.IsNullOrWhiteSpace(intent.SourceId) && intent.SourceId != "None" &&
                intent.SourceId == provider.SourceId && intent.Priority == provider.Priority &&
                velocity.y == 0f && IsFinite(velocity.x) && IsFinite(velocity.z);
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}

