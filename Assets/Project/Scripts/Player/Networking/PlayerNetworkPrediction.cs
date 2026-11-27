#if KUCHARZE_ONLINE
using System.Collections.Generic;
using UnityEngine;

namespace ModularMovement
{
    // Predicts the owner's motor and replays unacknowledged input after a server correction.
    [DisallowMultipleComponent]
    public sealed class PlayerNetworkPrediction : MonoBehaviour
    {
        public const float StepSeconds = 1f / 60f;
        private const int Capacity = 128;
        private readonly List<PlayerInputFrame> history = new List<PlayerInputFrame>(Capacity);
        private readonly SortedDictionary<uint, PlayerInputFrame> commands = new SortedDictionary<uint, PlayerInputFrame>();
        private PlayerNetworkInput network;
        private PlayerInputActions input;
        private PlayerMovementResolver resolver;
        private PlayerMotor motor;
        private PlayerRun run;
        private PlayerDash dash;
        private PlayerBus bus;
        private Transform visual;
        private Vector3 baseVisualPosition, visualError, lastServerPosition;
        private uint epoch, sequence, acknowledged, runBaseline, dashBaseline;
        private uint deviceRunReset, deviceDashReset;
        private float accumulator, nextSend, lastCommandTime;
        private bool ready, lastAllowed, timedOut;
        private PlayerServerFrame? pending;

        public float SnapshotAge { get; private set; }
        public float LastCorrection { get; private set; }
        public int CorrectionCount { get; private set; }
        public int PendingInputCount => history.Count;
        private float lastSnapshotTime;

        internal void Initialize(PlayerNetworkInput owner)
        {
            network = owner;
            input = GetComponent<PlayerInputActions>();
            resolver = GetComponent<PlayerMovementResolver>();
            motor = GetComponent<PlayerMotor>();
            run = GetComponent<PlayerRun>();
            dash = GetComponent<PlayerDash>();
            bus = GetComponent<PlayerBus>();
            var rotation = GetComponent<PlayerRotation>();
            if (rotation != null && rotation.ResolvedModel != null)
            {
                visual = rotation.ResolvedModel.parent != transform ? rotation.ResolvedModel.parent : rotation.ResolvedModel;
                baseVisualPosition = visual.localPosition;
            }
            resolver.ManualSimulation = true;
            commands.Clear();
            history.Clear();
            pending = null;
            visualError = Vector3.zero;
            lastSnapshotTime = Time.unscaledTime;
            timedOut = false;
            epoch = network.IsServer ? 1u : 0u;
            sequence = acknowledged = 0;
            accumulator = 0;
            nextSend = Time.unscaledTime;
            lastServerPosition = transform.position;
            lastAllowed = resolver.SimulationEnabled;
            ready = network.IsServer;
            ResetBaselines();
        }

        internal void Receive(PlayerInputFrame frame)
        {
            if (!network.IsServer || frame.Epoch != epoch || !resolver.SimulationEnabled) return;
            int ahead = unchecked((int)(frame.Sequence - acknowledged));
            if (ahead <= 0 || ahead > Capacity || commands.Count >= Capacity) return;
            Vector2 move = frame.Input.Move;
            if (float.IsNaN(move.x) || float.IsNaN(move.y) || float.IsInfinity(move.x) || float.IsInfinity(move.y)) return;
            frame.Input.Move = Vector2.ClampMagnitude(move, 1);
            commands[frame.Sequence] = frame;
            lastCommandTime = Time.unscaledTime;
            timedOut = false;
        }

        internal void Receive(PlayerServerFrame frame)
        {
            if (network.IsServer || !network.IsOwner) return;
            if (unchecked((int)(frame.Epoch - epoch)) < 0) return;
            if (pending.HasValue && (frame.Epoch < pending.Value.Epoch
                || (frame.Epoch == pending.Value.Epoch && frame.Sequence < pending.Value.Sequence))) return;
            pending = frame;
            lastSnapshotTime = Time.unscaledTime;
        }

        internal void Tick(float deltaTime)
        {
            if (network == null || !network.IsSpawned || (!network.IsServer && !network.IsOwner)) return;
            if (network.IsServer)
            {
                // Scene placement and pauses invalidate old input, including packets still in flight.
                if ((transform.position - lastServerPosition).sqrMagnitude > 0.0001f || lastAllowed != resolver.SimulationEnabled)
                {
                    epoch++;
                    sequence = acknowledged = 0;
                    commands.Clear();
                    accumulator = 0;
                    ResetBaselines();
                    RestoreFreshMotor();
                }
                lastAllowed = resolver.SimulationEnabled;
            }
            else if (pending.HasValue)
            {
                Reconcile(pending.Value);
                pending = null;
            }
            SnapshotAge = network.IsServer ? 0 : Time.unscaledTime - lastSnapshotTime;
            accumulator = Mathf.Min(accumulator + deltaTime, StepSeconds * 6);
            PlayerInputSnapshot local = network.IsOwner ? ReadLocal() : default;
            int steps = 0;
            while (accumulator >= StepSeconds && steps++ < 6)
            {
                if (!resolver.SimulationEnabled || (!network.IsServer && !ready)) { accumulator = 0; break; }
                if (network.IsServer && !network.IsOwner)
                {
                    if (commands.Count == 0)
                    {
                        if (Time.unscaledTime - lastCommandTime > 0.5f)
                        {
                            if (!timedOut)
                            {
                                timedOut = true;
                                epoch++;
                                acknowledged = 0;
                                RestoreFreshMotor();
                            }
                            resolver.Simulate(StepSeconds, new PlayerInputSnapshot { ResetVersion = epoch, DashResetVersion = epoch });
                            accumulator -= StepSeconds;
                            continue;
                        }
                        break;
                    }
                    PlayerInputFrame command = default;
                    foreach (var pair in commands) { command = pair.Value; break; }
                    commands.Remove(command.Sequence);
                    acknowledged = command.Sequence;
                    resolver.Simulate(StepSeconds, command.Input);
                }
                else
                {
                    var frame = new PlayerInputFrame { Sequence = ++sequence, Epoch = epoch, Input = local };
                    resolver.Simulate(StepSeconds, frame.Input);
                    if (!network.IsServer)
                    {
                        if (history.Count == Capacity) history.RemoveAt(0);
                        history.Add(frame);
                    }
                }
                accumulator -= StepSeconds;
            }
            if (Time.unscaledTime >= nextSend)
            {
                nextSend += 1f / 30;
                if (nextSend < Time.unscaledTime - 1f / 30) nextSend = Time.unscaledTime + 1f / 30;
                if (network.IsServer && !network.IsOwner) network.SendCheckpoint(Capture());
                else if (!network.IsServer && history.Count > 0 && ready)
                {
                    int last = history.Count - 1;
                    network.SendControls(history[last], history[Mathf.Max(0, last - 1)], history[Mathf.Max(0, last - 2)]);
                }
            }
            if (network.IsServer) lastServerPosition = transform.position;
        }

        private PlayerInputSnapshot ReadLocal()
        {
            PlayerInputSnapshot value = input.CaptureSnapshot();
            if (value.ResetVersion != deviceRunReset) { deviceRunReset = value.ResetVersion; runBaseline = 0; }
            if (value.DashResetVersion != deviceDashReset) { deviceDashReset = value.DashResetVersion; dashBaseline = 0; }
            value.RunPressCount = unchecked(value.RunPressCount - runBaseline);
            value.DashPressCount = unchecked(value.DashPressCount - dashBaseline);
            value.ResetVersion = unchecked(value.ResetVersion + epoch);
            value.DashResetVersion = unchecked(value.DashResetVersion + epoch);
            return value;
        }

        private void ResetBaselines()
        {
            if (!network.IsOwner) return;
            PlayerInputSnapshot current = input.CaptureSnapshot();
            runBaseline = current.RunPressCount;
            dashBaseline = current.DashPressCount;
            deviceRunReset = current.ResetVersion;
            deviceDashReset = current.DashResetVersion;
        }

        private void RestoreFreshMotor()
        {
            motor.RestoreSimulation(new PlayerMotorSnapshot { Position = transform.position, Source = "None" });
            if (run != null) run.RestoreSimulation(default);
            if (dash != null) dash.RestoreSimulation(default);
        }

        private PlayerServerFrame Capture() => new PlayerServerFrame
        {
            Epoch = epoch, Sequence = acknowledged, CanSimulate = resolver.SimulationEnabled,
            Motor = motor.CaptureSimulation(), Run = run != null ? run.CaptureSimulation() : default,
            Dash = dash != null ? dash.CaptureSimulation() : default
        };

        private void Reconcile(PlayerServerFrame state)
        {
            bool reset = epoch != state.Epoch || !ready;
            if (!reset && unchecked((int)(state.Sequence - acknowledged)) < 0) return;
            Vector3 before = transform.position;
            if (reset || !state.CanSimulate)
            {
                epoch = state.Epoch;
                sequence = state.Sequence;
                history.Clear();
                ResetBaselines();
                accumulator = 0;
            }
            else history.RemoveAll(frame => unchecked((int)(frame.Sequence - state.Sequence)) <= 0);
            acknowledged = state.Sequence;
            ready = state.CanSimulate;
            resolver.SimulationEnabled = state.CanSimulate;
            bool suppressed = bus.SuppressEvents;
            bus.SuppressEvents = true;
            try
            {
                motor.RestoreSimulation(state.Motor);
                if (run != null) run.RestoreSimulation(state.Run);
                if (dash != null) dash.RestoreSimulation(state.Dash);
                if (ready) foreach (PlayerInputFrame frame in history) resolver.Simulate(StepSeconds, frame.Input);
            }
            finally { bus.SuppressEvents = suppressed; }
            Vector3 error = before - transform.position;
            LastCorrection = error.magnitude;
            if (LastCorrection > 0.02f) CorrectionCount++;
            visualError = reset || LastCorrection > 2 ? Vector3.zero : Vector3.ClampMagnitude(visualError + error, 1);
        }

        private void LateUpdate()
        {
            if (network == null || !network.IsSpawned || network.IsServer || !network.IsOwner || visual == null) return;
            visualError *= Mathf.Exp(-20 * Time.unscaledDeltaTime);
            visual.localPosition = baseVisualPosition + transform.InverseTransformVector(visualError);
        }

        internal void Stop()
        {
            if (visual != null) visual.localPosition = baseVisualPosition;
            if (resolver != null) resolver.ManualSimulation = false;
            history.Clear();
            commands.Clear();
            ready = false;
        }
    }
}
#endif
