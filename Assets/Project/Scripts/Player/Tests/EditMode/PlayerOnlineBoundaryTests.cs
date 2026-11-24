using NUnit.Framework;
using UnityEngine;

namespace ModularMovement.Tests
{
    // Checks that remote controls and visual replicas stay separate for each player.
    public sealed class PlayerOnlineBoundaryTests
    {
        private GameObject first;
        private GameObject second;

        private sealed class Source : IPlayerInputSource
        {
            public PlayerInputSnapshot Snapshot;
            public PlayerInputSnapshot ReadInput() => Snapshot;
        }

        [TearDown]
        public void Cleanup()
        {
            if (first != null) Object.DestroyImmediate(first);
            if (second != null) Object.DestroyImmediate(second);
        }

        [Test]
        public void ExternalControlsBelongToOnlyOnePlayer()
        {
            first = new GameObject("First");
            second = new GameObject("Second");
            var a = first.AddComponent<PlayerInputActions>();
            var b = second.AddComponent<PlayerInputActions>();
            a.SetExternalSource(new Source { Snapshot = new PlayerInputSnapshot { Move = Vector2.right, RunHeld = true } });
            b.SetExternalSource(new Source { Snapshot = new PlayerInputSnapshot { Move = Vector2.up } });
            Assert.That(a.CaptureSnapshot().Move, Is.EqualTo(Vector2.right));
            Assert.That(b.CaptureSnapshot().Move, Is.EqualTo(Vector2.up));
            Assert.That(b.RunHeld, Is.False);
            Assert.That(a.RunHeld, Is.True);
        }

        [Test]
        public void ExternalDirectionUsesTheSharedDeadzoneAndCannotIncreaseDiagonalSpeed()
        {
            first = new GameObject("Player");
            var input = first.AddComponent<PlayerInputActions>();
            var source = new Source { Snapshot = new PlayerInputSnapshot { Move = new Vector2(4f, 4f) } };
            input.SetExternalSource(source);
            Assert.That(input.CaptureSnapshot().Move.magnitude, Is.EqualTo(1f).Within(0.0001f));
            source.Snapshot = new PlayerInputSnapshot { Move = new Vector2(0.02f, 0f) };
            Assert.That(input.CaptureSnapshot().Move, Is.EqualTo(Vector2.zero));
            source.Snapshot = new PlayerInputSnapshot { Move = new Vector2(float.NaN, 0f) };
            Assert.That(input.CaptureSnapshot().Move, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void ReplicaCannotOverwriteAnAuthoritativePlayersState()
        {
            first = new GameObject("Player");
            PlayerMovementDependencies.EnsureDependencies(first);
            var states = first.GetComponent<PlayerStates>();
            states.ApplyReplica(Vector3.right, true, true, "Run", 0.5f);
            Assert.That(states.IsReplica, Is.False);
            Assert.That(states.ActualVelocity, Is.EqualTo(Vector3.zero));
            first.GetComponent<PlayerMovementResolver>().SimulationEnabled = false;
            states.ApplyReplica(Vector3.right * 3f, true, true, "Run", 0.5f);
            Assert.That(states.IsReplica, Is.True);
            Assert.That(states.PlanarSpeed, Is.EqualTo(3f));
            Assert.That(states.ReplicaDashProgress, Is.EqualTo(0.5f));
        }

        [Test]
        public void ReplicaDashSignalDoesNotStartALocalAbility()
        {
            first = new GameObject("Player");
            PlayerMovementDependencies.EnsureDependencies(first);
            var bus = first.GetComponent<PlayerBus>();
            int received = 0;
            bus.DashStarted += () => received++;
            bus.ReceiveReplicaDash();
            Assert.That(received, Is.Zero);
            first.GetComponent<PlayerMovementResolver>().SimulationEnabled = false;
            first.GetComponent<PlayerStates>().ApplyReplica(Vector3.zero, false, true, "None", 0f);
            bus.ReceiveReplicaDash();
            Assert.That(received, Is.EqualTo(1));
            Assert.That(first.GetComponent<PlayerStates>().ActiveMovementSource, Is.EqualTo("None"));
        }

        [Test]
        public void RemovingExternalControlsReturnsToTheLocalInputPath()
        {
            first = new GameObject("Player");
            var input = first.AddComponent<PlayerInputActions>();
            input.SetExternalSource(new Source { Snapshot = new PlayerInputSnapshot { Move = Vector2.right } });
            Assert.That(input.CaptureSnapshot().Move, Is.EqualTo(Vector2.right));
            input.SetExternalSource(null);
            input.SetInputEnabled(false);
            Assert.That(input.CaptureSnapshot().Move, Is.EqualTo(Vector2.zero));
        }
    }
}
