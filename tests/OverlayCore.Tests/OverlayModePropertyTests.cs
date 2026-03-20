// Feature: OpenDash Monorepo Rebrand, Property 5: Overlay mode state machine alternates correctly
using FsCheck;
using FsCheck.Xunit;

namespace OpenDash.OverlayCore.Tests;

/// <summary>
/// Property tests for the overlay mode state machine.
/// Models the two-state toggle: OverlayMode (click-through) ↔ PositioningMode (draggable).
/// </summary>
public class OverlayModePropertyTests
{
    // ---------------------------------------------------------------------------
    // State machine model
    // ---------------------------------------------------------------------------

    private enum OverlayMode { Overlay, Positioning }

    /// <summary>Represents one toggle transition, returning the new mode and whether position was saved.</summary>
    private static (OverlayMode NewMode, bool PositionSaved) Toggle(OverlayMode current)
    {
        if (current == OverlayMode.Positioning)
        {
            // Exiting positioning mode via toggle = confirm (save position)
            return (OverlayMode.Overlay, true);
        }
        else
        {
            // Entering positioning mode
            return (OverlayMode.Positioning, false);
        }
    }

    /// <summary>Applies N toggles from an initial mode, returning the final mode.</summary>
    private static OverlayMode ApplyToggles(OverlayMode initial, int n)
    {
        var mode = initial;
        for (int i = 0; i < n; i++)
            mode = Toggle(mode).NewMode;
        return mode;
    }

    // ---------------------------------------------------------------------------
    // Property 5a: N toggles from any initial mode — even N preserves, odd N flips
    // ---------------------------------------------------------------------------

    [Property(
#if FAST_TESTS
        MaxTest = 10
#else
        MaxTest = 100
#endif
    )]
    public Property NToggles_EvenPreservesOddFlips()
    {
        var initialArb = Arb.From(Gen.Elements(OverlayMode.Overlay, OverlayMode.Positioning));
        var nArb = Arb.From(Gen.Choose(0, 20));

        return Prop.ForAll(initialArb, nArb, (initial, n) =>
        {
            var result = ApplyToggles(initial, n);
            var expectedFlipped = n % 2 == 1;

            var correct = expectedFlipped
                ? result != initial   // odd toggles: must be opposite
                : result == initial;  // even toggles: must be same

            return correct.Label(
                $"initial={initial}, n={n}, result={result}, expectedFlipped={expectedFlipped}");
        });
    }

    // ---------------------------------------------------------------------------
    // Property 5b: Positioning→Overlay via toggle ALWAYS triggers position save (confirm semantics)
    // ---------------------------------------------------------------------------

    [Property(
#if FAST_TESTS
        MaxTest = 10
#else
        MaxTest = 100
#endif
    )]
    public Property PositioningToOverlay_AlwaysSavesPosition()
    {
        // For any starting state that is Positioning, toggling must confirm (save) the position
        return Prop.ForAll(
            Arb.From(Gen.Constant(OverlayMode.Positioning)),
            positioning =>
            {
                var (newMode, positionSaved) = Toggle(positioning);
                return (newMode == OverlayMode.Overlay && positionSaved)
                    .Label("Positioning→Overlay transition must save position (confirm=true)");
            });
    }

    // ---------------------------------------------------------------------------
    // Property 5c: Overlay→Positioning via toggle does NOT save position (enter semantics)
    // ---------------------------------------------------------------------------

    [Property(
#if FAST_TESTS
        MaxTest = 10
#else
        MaxTest = 100
#endif
    )]
    public Property OverlayToPositioning_DoesNotSavePosition()
    {
        return Prop.ForAll(
            Arb.From(Gen.Constant(OverlayMode.Overlay)),
            overlay =>
            {
                var (newMode, positionSaved) = Toggle(overlay);
                return (newMode == OverlayMode.Positioning && !positionSaved)
                    .Label("Overlay→Positioning transition must NOT save position");
            });
    }

    // ---------------------------------------------------------------------------
    // Property 5d: Two toggles always return to the original mode (idempotent pair)
    // ---------------------------------------------------------------------------

    [Property(
#if FAST_TESTS
        MaxTest = 10
#else
        MaxTest = 100
#endif
    )]
    public Property TwoToggles_ReturnToOriginal()
    {
        return Prop.ForAll(
            Arb.From(Gen.Elements(OverlayMode.Overlay, OverlayMode.Positioning)),
            initial =>
            {
                var result = ApplyToggles(initial, 2);
                return (result == initial)
                    .Label($"Two toggles from {initial} should return to {initial}, got {result}");
            });
    }
}
