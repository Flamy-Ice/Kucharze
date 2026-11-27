using NUnit.Framework;
using UnityEngine;

namespace ModularMovement.Tests
{
    // Checks delayed remote animation and continuous dash playback between network packets.
    public sealed class PlayerPresentationTimelineTests
    {
        private static PlayerPresentationTimeline.Sample Sample(double time, string source, float progress = 0, uint id = 1)
            => new PlayerPresentationTimeline.Sample
            {
                Time = time, Source = source, Moving = source != "None", Grounded = true,
                Velocity = source == "None" ? Vector3.zero : Vector3.right * 5,
                Facing = Quaternion.identity, DashDuration = 0.2f, DashProgress = progress, DashId = id
            };

        [Test]
        public void FutureStopDoesNotStopDelayedAnimation()
        {
            var timeline = new PlayerPresentationTimeline();
            timeline.Add(Sample(1, "Walk"));
            timeline.Add(Sample(1.1, "None"));
            Assert.That(timeline.Evaluate(1.05, out var state), Is.True);
            Assert.That(state.Moving, Is.True);
            Assert.That(state.Velocity.x, Is.EqualTo(2.5f).Within(0.001));
            timeline.Evaluate(1.1, out state);
            Assert.That(state.Moving, Is.False);
        }

        [Test]
        public void DashAdvancesBetweenPacketsWithoutGoingBackAtNextSample()
        {
            var timeline = new PlayerPresentationTimeline();
            timeline.Add(Sample(1, "Dash", 0.25f));
            timeline.Evaluate(1.02, out var before);
            Assert.That(before.DashProgress, Is.EqualTo(0.35f).Within(0.001));
            timeline.Add(Sample(1.04, "Dash", 0.45f));
            timeline.Evaluate(1.03, out var after);
            Assert.That(after.DashProgress, Is.EqualTo(0.4f).Within(0.001));
            Assert.That(after.DashProgress, Is.GreaterThan(before.DashProgress));
        }

        [Test]
        public void DifferentDashesDoNotBlendAndOldPacketsAreIgnored()
        {
            var timeline = new PlayerPresentationTimeline();
            timeline.Add(Sample(1, "Dash", 0.9f, 1));
            timeline.Add(Sample(1.2, "Dash", 0.1f, 2));
            timeline.Add(Sample(1.1, "None"));
            timeline.Evaluate(1.1, out var state);
            Assert.That(state.DashId, Is.EqualTo(1));
            Assert.That(state.DashProgress, Is.EqualTo(1));
            timeline.Evaluate(1.2, out state);
            Assert.That(state.DashId, Is.EqualTo(2));
            Assert.That(state.DashProgress, Is.EqualTo(0.1f).Within(0.001));
        }

        [Test]
        public void FutureDashDoesNotStartEarlyAndClearRemovesOldScene()
        {
            var timeline = new PlayerPresentationTimeline();
            timeline.Add(Sample(1, "Walk"));
            timeline.Add(Sample(1.04, "Dash", 0.1f));
            timeline.Evaluate(1.02, out var state);
            Assert.That(state.Source, Is.EqualTo("Walk"));
            timeline.Evaluate(1.04, out state);
            Assert.That(state.Source, Is.EqualTo("Dash"));
            timeline.Clear();
            Assert.That(timeline.Evaluate(2, out state), Is.False);
        }
    }
}
