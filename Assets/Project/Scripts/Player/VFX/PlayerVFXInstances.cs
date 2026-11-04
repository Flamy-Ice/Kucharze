using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace ModularMovement
{
    // Owns one-shot copies and removes them when finished or when their lifetime expires.
    internal sealed class PlayerVFXInstances
    {
        // One record keeps the objects and time limit for one copy of an effect.
        private sealed class Instance
        {
            internal GameObject Root;
            internal ParticleSystem[] Systems;
            internal bool HasVisualEffects;
            internal bool IsStep;
            internal float Remaining;
            // Give newly started particles one frame before checking whether they have finished.
            internal int StartFrame;
        }

        private readonly List<Instance> active = new List<Instance>();
        private GameObject stagingRoot;

        // Return a setup problem for the Inspector, or null when the prefab can be used.
        // This checks the saved effect without changing its asset.
        internal static string GetPrefabIssue(GameObject prefab)
        {
            if (prefab == null) return null;
            if (prefab.scene.IsValid()) return "Assign a prefab asset, not an object from the scene.";
            ParticleSystem[] systems = prefab.GetComponentsInChildren<ParticleSystem>(true);
            VisualEffect[] effects = prefab.GetComponentsInChildren<VisualEffect>(true);
            if (systems.Length == 0 && effects.Length == 0)
                return "The prefab needs a Particle System or a Visual Effect component.";
            // Extra scripts could create effects this helper cannot track or clean up.
            if (prefab.GetComponentsInChildren<MonoBehaviour>(true).Length != 0)
                return "Use a Particle System or Visual Effect prefab without gameplay or custom effect scripts.";
            bool hasStarter = false;
            foreach (VisualEffect effect in effects)
            {
                if (effect.visualEffectAsset == null)
                    return "Assign a Visual Effect Graph asset to every Visual Effect component.";
                if (string.IsNullOrWhiteSpace(effect.initialEventName))
                    return "Set Initial Event Name on every Visual Effect component, usually OnPlay.";
                if (effect.enabled && IsEnabledInPrefab(effect.transform, prefab.transform)) hasStarter = true;
            }
            var subEmitters = GetSubEmitters(systems);
            foreach (ParticleSystem subEmitter in subEmitters)
                if (subEmitter != null && subEmitter.transform != prefab.transform
                    && !subEmitter.transform.IsChildOf(prefab.transform))
                    return "Sub emitters must belong to this prefab.";
            foreach (ParticleSystem system in systems)
            {
                var main = system.main;
                if (main.loop) return "Disable Looping on every Particle System, including children.";
                if (main.playOnAwake) return "Disable Play On Awake on every Particle System.";
                if (main.stopAction != ParticleSystemStopAction.None)
                    return "Set Stop Action to None; PlayerVFX removes the complete effect.";
                if (!subEmitters.Contains(system) && IsEnabledInPrefab(system.transform, prefab.transform))
                    hasStarter = true;
            }
            return hasStarter ? null : "The prefab needs an active Particle System starter or an enabled Visual Effect.";
        }

        // The copied root will be enabled on spawn; inactive children stay inactive.
        private static bool IsEnabledInPrefab(Transform child, Transform root)
        {
            for (Transform current = child; current != root; current = current.parent)
                if (!current.gameObject.activeSelf) return false;
            return true;
        }

        // Find systems triggered by other particles, so they are not started on their own.
        private static HashSet<ParticleSystem> GetSubEmitters(ParticleSystem[] systems)
        {
            var result = new HashSet<ParticleSystem>();
            foreach (ParticleSystem system in systems)
            {
                var module = system.subEmitters;
                if (!module.enabled) continue;
                for (int index = 0; index < module.subEmittersCount; index++)
                    result.Add(module.GetSubEmitterSystem(index));
            }
            return result;
        }

        // Make a fresh copy for each trigger, so an earlier effect is not restarted.
        internal void Play(GameObject prefab, Transform point, bool isStep, float lifetime)
        {
            if (prefab == null || point == null || GetPrefabIssue(prefab) != null) return;
            if (stagingRoot == null)
            {
                stagingRoot = new GameObject("PlayerVFXStaging");
                stagingRoot.SetActive(false);
            }
            // Prepare the copy while inactive, so its graph starts only after it is placed in the world.
            GameObject copy = Object.Instantiate(prefab, point.position, point.rotation, stagingRoot.transform);
            ParticleSystem[] systems = copy.GetComponentsInChildren<ParticleSystem>(true);
            VisualEffect[] effects = copy.GetComponentsInChildren<VisualEffect>(true);
            var subEmitters = GetSubEmitters(systems);
            copy.SetActive(true);
            copy.transform.SetParent(null, true);
            // Enabling sends the configured Initial Event. Calling Play or Reinit too would start it twice.
            bool hasVisualEffects = false;
            foreach (VisualEffect effect in effects)
                if (effect.isActiveAndEnabled) hasVisualEffects = true;
            foreach (ParticleSystem system in systems)
            {
                // Playing children recursively would also start sub emitters before their trigger.
                if (system.gameObject.activeInHierarchy && !subEmitters.Contains(system)) system.Play(false);
            }
            active.Add(new Instance
            {
                Root = copy, Systems = systems, HasVisualEffects = hasVisualEffects, IsStep = isStep,
                Remaining = lifetime, StartFrame = Time.frameCount
            });
        }

        // Count down game time and remove only copies that have finished or reached their limit.
        internal void Tick(float deltaTime)
        {
            for (int index = active.Count - 1; index >= 0; index--)
            {
                Instance instance = active[index];
                if (!float.IsNaN(deltaTime) && !float.IsInfinity(deltaTime) && deltaTime > 0f)
                    instance.Remaining -= deltaTime;
                // A graph can have no particles before a delayed burst; keep it until its time limit.
                if (instance.Root != null && instance.Remaining > 0f
                    && (instance.HasVisualEffects || instance.StartFrame == Time.frameCount || IsAlive(instance.Systems))) continue;
                Remove(index);
            }
        }

        // Particles can remain after emission stops, so check every active child too.
        private static bool IsAlive(ParticleSystem[] systems)
        {
            foreach (ParticleSystem system in systems)
                if (system != null && system.gameObject.activeInHierarchy && system.IsAlive(false)) return true;
            return false;
        }

        // Count our existing copies. A graph copy may remain after its visible particles disappear.
        internal void GetActiveCounts(out int runCount, out int dashCount)
        {
            runCount = dashCount = 0;
            foreach (Instance instance in active)
            {
                if (instance.Root == null || !instance.Root.activeInHierarchy) continue;
                if (instance.IsStep) runCount++;
                else dashCount++;
            }
        }

        // Starting a dash interrupts footstep copies while leaving earlier dash copies alone.
        internal void ClearSteps()
        {
            for (int index = active.Count - 1; index >= 0; index--)
                if (active[index].IsStep) Remove(index);
        }

        // Turning off PlayerVFX removes its own copies and their inactive preparation object.
        internal void Clear()
        {
            for (int index = active.Count - 1; index >= 0; index--) Remove(index);
            if (stagingRoot != null) Object.Destroy(stagingRoot);
            stagingRoot = null;
        }

        // Remove the whole copy, including any child particle systems or graphs.
        private void Remove(int index)
        {
            GameObject root = active[index].Root;
            if (root != null)
            {
                // Hide now, since Unity finishes Destroy at the end of the frame.
                root.SetActive(false);
                Object.Destroy(root);
            }
            active.RemoveAt(index);
        }
    }
}
