using System;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using OpenDash.SpeakerSight.Models;
using OpenDash.SpeakerSight.ViewModels;

namespace OpenDash.SpeakerSight.Tests.ViewModels;

public class OverlayViewModelTests
{
    // Feature: SpeakerSight, Property 1: speaker cap never exceeds 8 items regardless of event count
#if FAST_TESTS
    [Property(MaxTest = 10)]
#else
    [Property(MaxTest = 100)]
#endif
    public Property Property_SpeakerCapNeverExceeds8()
    {
        // Generate N > 8 active speakers and verify the display list is always capped at 8
        var countGen = Gen.Choose(9, 50);

        return Prop.ForAll(countGen.ToArbitrary(), n =>
        {
            var speakers = Enumerable.Range(0, n)
                .Select(i => new ActiveSpeaker
                {
                    UserId            = i.ToString(),
                    DisplayName       = $"Speaker{i}",
                    State             = SpeakerState.Active,
                    Opacity           = 1.0,
                    LastActivatedUtc  = DateTimeOffset.UtcNow - TimeSpan.FromSeconds(i)
                })
                .ToList();

            var result = OverlayViewModel.BuildDisplayList(speakers, DisplayMode.SpeakersOnly);
            return result.Count <= 8;
        });
    }

    // Feature: SpeakerSight, Property 2: display list never exceeds 8 items in all-members mode
#if FAST_TESTS
    [Property(MaxTest = 10)]
#else
    [Property(MaxTest = 100)]
#endif
    public Property Property_SpeakerCapNeverExceeds8_AllMembersMode()
    {
        var countGen = Gen.Choose(9, 50);

        return Prop.ForAll(countGen.ToArbitrary(), n =>
        {
            var speakers = Enumerable.Range(0, n)
                .Select(i => new ActiveSpeaker
                {
                    UserId       = i.ToString(),
                    DisplayName  = $"Speaker{i}",
                    State        = i % 3 == 0 ? SpeakerState.Active
                                 : i % 3 == 1 ? SpeakerState.RecentlyActive
                                 : SpeakerState.Silent,
                    Opacity      = i % 3 == 0 ? 1.0 : i % 3 == 1 ? 0.5 : 0.0,
                    LastActivatedUtc = DateTimeOffset.UtcNow - TimeSpan.FromSeconds(i)
                })
                .ToList();

            var result = OverlayViewModel.BuildDisplayList(speakers, DisplayMode.AllMembers);
            return result.Count <= 8;
        });
    }
}
