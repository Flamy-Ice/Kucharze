using NUnit.Framework;
using System.Reflection;
using UnityEngine;

namespace ModularMovement.Tests
{
    // Checks that corrected movement can replay collisions and abilities without duplicate effects.
    public sealed class PlayerPredictionReplayTests
    {
        private GameObject player, floor, wall;
        private const float Step = 1f / 60;

        [SetUp]
        public void Setup()
        {
            floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = new Vector3(0, -0.5f, 0);
            floor.transform.localScale = new Vector3(100, 1, 100);
            player = new GameObject("PredictedPlayer");
            player.transform.position = new Vector3(0, 1.1f, 0);
            player.AddComponent<PlayerWalk>();
            player.AddComponent<PlayerRun>().ActivationMode = RunActivationMode.Toggle;
            player.AddComponent<PlayerDash>();
            // Edit Mode does not start the ordinary runtime lifecycle for these abilities.
            foreach (MonoBehaviour ability in new MonoBehaviour[]
                { player.GetComponent<PlayerWalk>(), player.GetComponent<PlayerRun>(), player.GetComponent<PlayerDash>() })
                ability.GetType().GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ability, null);
            player.GetComponent<PlayerMovementResolver>().ManualSimulation = true;
            Physics.SyncTransforms();
        }

        [TearDown]
        public void Cleanup()
        {
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(floor);
            if (wall != null) Object.DestroyImmediate(wall);
        }

        [Test]
        public void SimulationDoesNotOverwriteDeviceResetVersions()
        {
            var input = player.GetComponent<PlayerInputActions>();
            uint previous = input.InputResetVersion;
            uint dashPrevious = input.DashInputResetVersion;
            var snapshot = Input(3, 2);
            snapshot.ResetVersion = 123;
            snapshot.DashResetVersion = 456;
            player.GetComponent<PlayerMovementResolver>().Simulate(Step, snapshot);
            Assert.That(input.InputResetVersion, Is.EqualTo(previous));
            Assert.That(input.DashInputResetVersion, Is.EqualTo(dashPrevious));
        }

        [Test]
        public void MenuBlockingFinishesAcceptedDashAndDoesNotQueueAnother()
        {
            var resolver = player.GetComponent<PlayerMovementResolver>();
            var input = player.GetComponent<PlayerInputActions>();
            var dash = player.GetComponent<PlayerDash>();
            var accepted = input.CaptureSnapshot();
            accepted.Move = Vector2.right;
            accepted.DashAvailable = true;
            accepted.DashPressCount++;
            resolver.Simulate(Step, accepted);
            Assert.That(dash.IsDashing, Is.True);
            // The device reading must match the press the motor just consumed.
            typeof(PlayerInputActions).GetMethod("RestoreReadings", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(input, new object[] { accepted });
            input.SetControlsBlocked(true);
            var blocked = input.CaptureSnapshot();
            Assert.That(blocked.Move, Is.EqualTo(Vector2.zero));
            Assert.That(blocked.DashAvailable, Is.False);
            Assert.That(blocked.DashResetVersion, Is.EqualTo(accepted.DashResetVersion));
            float remaining = dash.CaptureSimulation().Remaining;
            resolver.Simulate(Step, blocked);
            Assert.That(dash.IsDashing, Is.True);
            Assert.That(dash.CaptureSimulation().Remaining, Is.EqualTo(remaining - Step).Within(0.00001f));
            for (int i = 0; i < 120; i++) resolver.Simulate(Step, blocked);
            Assert.That(dash.IsDashing, Is.False);
            input.SetControlsBlocked(false);
            resolver.Simulate(Step, input.CaptureSnapshot());
            Assert.That(dash.IsDashing, Is.False);
        }

        [Test]
        public void LocalInputMovesBeforeAnAcknowledgementExists()
        {
            player.GetComponent<PlayerMovementResolver>().Simulate(Step, Input(0));
            Assert.That(player.transform.position.x, Is.GreaterThan(0));
        }

        [Test]
        public void ReplayingToggleRunAndDashRestoresTheSameResultWithoutExtraDashEvents()
        {
            var resolver = player.GetComponent<PlayerMovementResolver>();
            var motor = player.GetComponent<PlayerMotor>();
            var run = player.GetComponent<PlayerRun>();
            var dash = player.GetComponent<PlayerDash>();
            var bus = player.GetComponent<PlayerBus>();
            int events = 0;
            bus.DashStarted += () => events++;
            for (int i = 0; i < 10; i++) resolver.Simulate(Step, Input(0));
            PlayerMotorSnapshot start = motor.CaptureSimulation();
            PlayerRun.SimulationState runStart = run.CaptureSimulation();
            PlayerDash.SimulationState dashStart = dash.CaptureSimulation();
            for (int i = 0; i < 40; i++) resolver.Simulate(Step, Input(i < 4 ? 1u : 2u, i < 8 ? 0u : 1u));
            Vector3 expected = player.transform.position;
            float expectedCooldown = dash.CooldownRemaining;
            Assert.That(events, Is.EqualTo(1));
            motor.RestoreSimulation(start);
            run.RestoreSimulation(runStart);
            dash.RestoreSimulation(dashStart);
            bus.SuppressEvents = true;
            for (int i = 0; i < 40; i++) resolver.Simulate(Step, Input(i < 4 ? 1u : 2u, i < 8 ? 0u : 1u));
            bus.SuppressEvents = false;
            Assert.That(Vector3.Distance(expected, player.transform.position), Is.LessThan(0.002f));
            Assert.That(dash.CooldownRemaining, Is.EqualTo(expectedCooldown).Within(0.0001f));
            Assert.That(events, Is.EqualTo(1));
        }

        [Test]
        public void PredictionAndReplayCannotMoveThroughAWall()
        {
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = new Vector3(2, 1, 0);
            wall.transform.localScale = new Vector3(1, 4, 20);
            Physics.SyncTransforms();
            var resolver = player.GetComponent<PlayerMovementResolver>();
            var motor = player.GetComponent<PlayerMotor>();
            PlayerMotorSnapshot start = motor.CaptureSimulation();
            for (int i = 0; i < 90; i++) resolver.Simulate(Step, Input(0));
            Vector3 expected = player.transform.position;
            Assert.That(expected.x, Is.GreaterThan(0.1f));
            Assert.That(expected.x, Is.LessThan(1.5f));
            motor.RestoreSimulation(start);
            for (int i = 0; i < 90; i++) resolver.Simulate(Step, Input(0));
            Assert.That(Vector3.Distance(expected, player.transform.position), Is.LessThan(0.002f));
        }

        private static PlayerInputSnapshot Input(uint run, uint dash = 0) => new PlayerInputSnapshot
        {
            Move = Vector2.right, RunPressCount = run, DashPressCount = dash, DashAvailable = true
        };
    }
}
