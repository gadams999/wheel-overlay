using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FsCheck;
using FsCheck.Xunit;
using OpenDash.SpeakerSight.Models;
using OpenDash.SpeakerSight.Services;
using OpenDash.OverlayCore.Services;

namespace OpenDash.SpeakerSight.Tests.Services;

public class AliasServiceTests
{
    // Feature: SpeakerSight, Property 1: AliasService Resolve returns custom name when set, falls back to last-known name, then raw Discord name
#if FAST_TESTS
    [Property(MaxTest = 10)]
#else
    [Property(MaxTest = 100)]
#endif
    public Property Property_Resolve_PrioritizesCustomName_ThenLastKnown_ThenRaw()
    {
        // Generate non-null non-empty strings for lastKnownName, optionally null customName.
        var nonEmptyStr  = Arb.Default.NonEmptyString().Generator.Select(s => s.Get);
        var optionalName = Gen.OneOf(
            Gen.Constant<string?>(null),
            nonEmptyStr.Select(s => (string?)s));

        var gen = from lastKnown   in nonEmptyStr
                  from customName  in optionalName
                  from rawDiscord  in nonEmptyStr
                  select (lastKnown, customName, rawDiscord);

        return Prop.ForAll(gen.ToArbitrary(), args =>
        {
            var (lastKnown, customName, rawDiscord) = args;

            var tmpPath = Path.GetTempFileName();
            try
            {
                // Build an aliases.json with one context containing one member
                const string guildId   = "111111111111111111";
                const string channelId = "222222222222222222";
                const string userId    = "333333333333333333";

                var member = new ChannelMember
                {
                    UserId            = userId,
                    LastKnownName     = lastKnown,
                    CustomDisplayName = customName,
                    AvatarVisible     = true
                };
                var context = new ChannelContext
                {
                    GuildId     = guildId,
                    GuildName   = "TestGuild",
                    ChannelId   = channelId,
                    ChannelName = "TestChannel",
                    Members     = new List<ChannelMember> { member }
                };
                File.WriteAllText(tmpPath, JsonSerializer.Serialize(new[] { context }));

                // Ensure LogService is initialized for AliasService internal calls
                LogService.Initialize("AliasServiceTests");

                var svc = new AliasService(tmpPath);
                svc.Load();

                var resolved = svc.Resolve(userId, guildId, channelId, rawDiscord);

                if (customName != null)
                    return (resolved == customName)
                        .Label($"Expected custom='{customName}' but got '{resolved}'");

                return (resolved == lastKnown)
                    .Label($"Expected lastKnown='{lastKnown}' but got '{resolved}'");
            }
            finally
            {
                if (File.Exists(tmpPath)) File.Delete(tmpPath);
            }
        });
    }

    // Feature: SpeakerSight, Property 2: AliasService skips and logs malformed entries without throwing
#if FAST_TESTS
    [Property(MaxTest = 10)]
#else
    [Property(MaxTest = 100)]
#endif
    public Property Property_Load_SkipsMalformedEntries_DoesNotThrow()
    {
        // Generate a count of valid and invalid entries to interleave
        var gen = from validCount   in Gen.Choose(0, 3)
                  from invalidCount in Gen.Choose(1, 3)
                  select (validCount, invalidCount);

        return Prop.ForAll(gen.ToArbitrary(), args =>
        {
            var (validCount, invalidCount) = args;
            var tmpPath = Path.GetTempFileName();
            try
            {
                // Build JSON with a mix of valid contexts and malformed ones
                var items = new List<object>();

                for (int i = 0; i < validCount; i++)
                {
                    items.Add(new
                    {
                        GuildId     = $"1000000000000000{i:D2}",
                        GuildName   = $"Guild{i}",
                        ChannelId   = $"2000000000000000{i:D2}",
                        ChannelName = $"Channel{i}",
                        Members     = new[]
                        {
                            new
                            {
                                UserId            = $"3000000000000000{i:D2}",
                                LastKnownName     = $"User{i}",
                                CustomDisplayName = (string?)null,
                                AvatarVisible     = true
                            }
                        }
                    });
                }

                for (int i = 0; i < invalidCount; i++)
                {
                    // Invalid: non-snowflake GuildId
                    items.Add(new
                    {
                        GuildId     = "not-a-snowflake",
                        GuildName   = "Bad",
                        ChannelId   = "also-bad",
                        ChannelName = "Bad",
                        Members     = Array.Empty<object>()
                    });
                }

                File.WriteAllText(tmpPath, JsonSerializer.Serialize(items));

                LogService.Initialize("AliasServiceTests");

                var svc = new AliasService(tmpPath);
                bool threw = false;
                try
                {
                    svc.Load();
                }
                catch
                {
                    threw = true;
                }

                if (threw) return false.Label("Load() must not throw on malformed input");

                var loaded = svc.GetAllContexts();
                // Only valid entries should survive
                return (loaded.Count == validCount)
                    .Label($"Expected {validCount} valid contexts but got {loaded.Count}");
            }
            finally
            {
                if (File.Exists(tmpPath)) File.Delete(tmpPath);
            }
        });
    }
}
