using System;
using System.Text;
using System.Text.Json;
using FsCheck;
using FsCheck.Xunit;
using OpenDash.SpeakerSight.Models;

namespace OpenDash.SpeakerSight.Tests.Models;

public class ChannelMemberTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    // Feature: SpeakerSight, Property 1: ChannelMember with arbitrary Unicode custom name serializes and deserializes identically
#if FAST_TESTS
    [Property(MaxTest = 10)]
#else
    [Property(MaxTest = 100)]
#endif
    public Property Property_ChannelMember_UnicodeCustomName_SerializationRoundTrip()
    {
        // Generate arbitrary non-null strings — FsCheck's string generator covers ASCII,
        // multi-byte Unicode, emoji, surrogates, and empty strings by default.
        return Prop.ForAll(Arb.Default.String(), rawName =>
        {
            // Null strings are covered separately; test non-null paths here.
            if (rawName == null) return true.Label("skip null");

            var original = new ChannelMember
            {
                UserId            = "123456789012345678",
                LastKnownName     = "DiscordName",
                CustomDisplayName = rawName,
                AvatarVisible     = true
            };

            var json       = JsonSerializer.Serialize(original, JsonOptions);
            var deserialized = JsonSerializer.Deserialize<ChannelMember>(json, JsonOptions)!;

            // Compare byte-for-byte via UTF-8 encoding to catch any surrogate-pair normalization issues.
            var originalBytes     = Encoding.UTF8.GetBytes(rawName);
            var deserializedBytes = Encoding.UTF8.GetBytes(deserialized.CustomDisplayName ?? string.Empty);

            return originalBytes.AsSpan().SequenceEqual(deserializedBytes.AsSpan())
                .Label($"Round-trip mismatch for CustomDisplayName: original={rawName} deserialized={deserialized.CustomDisplayName}");
        });
    }
}
