using System.Collections.Generic;
using UnityEngine;

namespace ModularMovement
{
    // Keeps remote animation on the same delayed clock as the visible movement.
    public sealed class PlayerPresentationTimeline
    {
        public struct Sample
        {
            public double Time;
            public Vector3 Velocity;
            public Quaternion Facing;
            public bool Moving, Grounded;
            public string Source;
            public float DashProgress, DashDuration;
            public uint DashId;
        }

        private readonly List<Sample> samples = new List<Sample>(64);
        public void Clear() => samples.Clear();
        public void Add(Sample sample)
        {
            if (samples.Count > 0 && sample.Time <= samples[samples.Count - 1].Time) return;
            samples.Add(sample);
            if (samples.Count > 64) samples.RemoveAt(0);
        }

        public bool Evaluate(double time, out Sample result)
        {
            result = default;
            if (samples.Count == 0) return false;
            while (samples.Count > 2 && samples[1].Time <= time) samples.RemoveAt(0);
            result = samples[0];
            if (time < result.Time) return true;
            if (samples.Count > 1)
            {
                Sample next = samples[1];
                if (time >= next.Time) result = next;
                else
                {
                    float fraction = (float)((time - result.Time) / (next.Time - result.Time));
                    result.Velocity = Vector3.Lerp(result.Velocity, next.Velocity, fraction);
                    result.Facing = Quaternion.Slerp(result.Facing, next.Facing, fraction);
                    // Do not blend different dashes or switch abilities before their timestamp.
                    if (result.Source == "Dash" && next.Source == "Dash" && result.DashId == next.DashId)
                    {
                        result.DashProgress = Mathf.Lerp(result.DashProgress, next.DashProgress, fraction);
                        result.Time = time;
                    }
                }
            }
            if (result.Source == "Dash" && result.DashDuration > 0f)
                result.DashProgress = Mathf.Clamp01(result.DashProgress
                    + (float)System.Math.Max(0, time - result.Time) / result.DashDuration);
            return true;
        }
    }
}
