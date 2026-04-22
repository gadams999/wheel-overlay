using System;
using FsCheck;
using FsCheck.Xunit;

namespace OpenDash.SpeakerSight.Tests.Services;

/// <summary>
/// FsCheck property tests for the VoiceSessionService debounce and grace-period
/// state machine specification.  Tests run against a synchronous simulator that
/// encodes the same state-transition rules as the inner SpeakerStateMachine so
/// they can execute without WPF infrastructure or real timers.
/// </summary>
public class VoiceSessionServiceTests
{
    // Feature: SpeakerSight, Property 1: debounce events shorter than threshold produce no state transition
#if FAST_TESTS
    [Property(MaxTest = 10)]
#else
    [Property(MaxTest = 100)]
#endif
    public Property Property_DebounceEventShorterThanThreshold_DoesNotReachActive()
    {
        // threshold T ∈ [1, 1000] ms
        var gen = Gen.Choose(1, 1000);

        return Prop.ForAll(gen.ToArbitrary(), threshold =>
        {
            // SPEAKING_START (starts debounce timer) → Debouncing
            // SPEAKING_STOP  (arrives before timer fires) → Idle
            // Invariant: participant never reaches Active state
            var sim = new SpeakerStateSimulator();
            sim.OnSpeakingStart(threshold);
            sim.OnSpeakingStop();

            return sim.State != SimState.Active;
        });
    }

    // Feature: SpeakerSight, Property 2: debounce threshold elapsed transitions participant to Active
#if FAST_TESTS
    [Property(MaxTest = 10)]
#else
    [Property(MaxTest = 100)]
#endif
    public Property Property_DebounceTimerElapsed_TransitionsToActive()
    {
        // threshold T ∈ [1, 500] ms
        var gen = Gen.Choose(1, 500);

        return Prop.ForAll(gen.ToArbitrary(), threshold =>
        {
            // SPEAKING_START → Debouncing; debounce timer fires → Active
            var sim = new SpeakerStateSimulator();
            sim.OnSpeakingStart(threshold);
            sim.FireDebounceElapsed();

            return sim.State == SimState.Active && sim.Opacity == 1.0;
        });
    }

    // Feature: SpeakerSight, Property 3: opacity fade monotone during grace period
#if FAST_TESTS
    [Property(MaxTest = 10)]
#else
    [Property(MaxTest = 100)]
#endif
    public Property Property_OpacityFadeIsMonotoneDuringGracePeriod()
    {
        // grace G ∈ [0.05, 2.0] s — values above 0 to ensure the fade timer is active
        var gen = Gen.Choose(5, 200).Select(n => n / 100.0);

        return Prop.ForAll(gen.ToArbitrary(), gracePeriodSeconds =>
        {
            var sim = new SpeakerStateSimulator();
            sim.OnSpeakingStart(0);  // threshold = 0 → immediate Active
            sim.OnSpeakingStop();    // → RecentlyActive, Opacity = 1.0

            bool monotone = true;

            for (int i = 0; i < 200 && sim.State == SimState.RecentlyActive; i++)
            {
                double opacityBefore = sim.Opacity;
                sim.FireFadeTick(gracePeriodSeconds);

                // While still fading, opacity must strictly decrease each tick
                if (sim.State == SimState.RecentlyActive && sim.Opacity >= opacityBefore)
                {
                    monotone = false;
                    break;
                }
            }

            return monotone;
        });
    }

    // Feature: SpeakerSight, Property 4: grace period resumption always transitions to Active and restores opacity
#if FAST_TESTS
    [Property(MaxTest = 10)]
#else
    [Property(MaxTest = 100)]
#endif
    public Property Property_SpeakingStartDuringGracePeriod_RestoresActiveAndOpacity()
    {
        // G ∈ [0.5, 2.0] s, ticks ∈ [1, 3].
        // With G=0.5 and 3 ticks: decrement ≈ 0.066, opacity after 3 ticks ≈ 0.80 — well above 0.
        var gen = from g     in Gen.Choose(50, 200).Select(n => n / 100.0)
                  from ticks in Gen.Choose(1, 3)
                  select (g, ticks);

        return Prop.ForAll(gen.ToArbitrary(), args =>
        {
            var (gracePeriodSeconds, ticks) = args;

            var sim = new SpeakerStateSimulator();
            sim.OnSpeakingStart(0);  // → Active
            sim.OnSpeakingStop();    // → RecentlyActive, Opacity = 1.0

            for (int i = 0; i < ticks; i++)
                sim.FireFadeTick(gracePeriodSeconds);

            if (sim.State == SimState.RecentlyActive)
            {
                // Participant resumes speaking mid-fade — threshold is irrelevant when RecentlyActive
                sim.OnSpeakingStart(0);
                return sim.State == SimState.Active && sim.Opacity == 1.0;
            }

            // Degenerate: already faded to Silent — no resumption to test; treat as vacuously passing
            return true;
        });
    }

    // ── State machine simulator ─────────────────────────────────────────────

    private enum SimState { Idle, Debouncing, Active, RecentlyActive, Silent }

    /// <summary>
    /// Lightweight synchronous model of the <c>SpeakerStateMachine</c> inner class in
    /// <c>VoiceSessionService</c>.  Encodes the same state-transition rules without
    /// WPF infrastructure or real timers, making it suitable for fast FsCheck property tests.
    /// </summary>
    private sealed class SpeakerStateSimulator
    {
        public SimState State   { get; private set; } = SimState.Idle;
        public double   Opacity { get; private set; } = 0.0;

        /// <summary>Process a SPEAKING_START event with the given debounce threshold (ms).</summary>
        public void OnSpeakingStart(int thresholdMs)
        {
            switch (State)
            {
                case SimState.RecentlyActive:
                    // Interrupt fade — go directly to Active (FR-004b)
                    State   = SimState.Active;
                    Opacity = 1.0;
                    break;

                case SimState.Idle:
                case SimState.Silent:
                    if (thresholdMs > 0)
                        State = SimState.Debouncing;
                    else
                    {
                        State   = SimState.Active;
                        Opacity = 1.0;
                    }
                    break;

                case SimState.Debouncing:
                    break; // reset debounce window; state stays Debouncing

                case SimState.Active:
                    break; // already active
            }
        }

        /// <summary>Process a SPEAKING_STOP event.</summary>
        public void OnSpeakingStop()
        {
            switch (State)
            {
                case SimState.Debouncing:
                    State = SimState.Idle; // timer cancelled — Active was never reached
                    break;

                case SimState.Active:
                    State   = SimState.RecentlyActive;
                    Opacity = 1.0; // fade starts from full opacity
                    break;
            }
        }

        /// <summary>
        /// Simulate the debounce timer firing (equivalent to <c>OnDebounceElapsed</c>
        /// being called after the threshold duration has elapsed).
        /// </summary>
        public void FireDebounceElapsed()
        {
            if (State == SimState.Debouncing)
            {
                State   = SimState.Active;
                Opacity = 1.0;
            }
        }

        /// <summary>
        /// Simulate one fade-timer tick (~33 ms) for the given grace period.
        /// Mirrors the production <c>OnFadeTick</c> logic: decrements Opacity by
        /// <c>1 / (gracePeriodSeconds / 0.033)</c>, clamped to [0, 1]; transitions
        /// to Silent when Opacity reaches 0.
        /// </summary>
        public void FireFadeTick(double gracePeriodSeconds)
        {
            if (State != SimState.RecentlyActive) return;

            double decrement = 1.0 / (gracePeriodSeconds / 0.033);
            Opacity = Math.Max(0.0, Opacity - decrement);

            if (Opacity <= 0.0)
            {
                Opacity = 0.0;
                State   = SimState.Silent;
            }
        }
    }
}
