// Feature: OpenDash Monorepo Rebrand, Property 6: Namespaced tag format round-trips correctly
using FsCheck;
using FsCheck.Xunit;

namespace OpenDash.OverlayCore.Tests;

/// <summary>
/// Property tests validating the namespaced release tag format contract.
/// Pattern: {app-name}/v{major}.{minor}.{patch}
/// Valid app-names: lowercase letters, digits, hyphens; must start with a letter.
/// </summary>
public class TagFormatPropertyTests
{
    // ---------------------------------------------------------------------------
    // Helpers (self-contained — format/parse logic lives here as the contract spec)
    // ---------------------------------------------------------------------------

    private static string FormatTag(string appName, int major, int minor, int patch)
        => $"{appName}/v{major}.{minor}.{patch}";

    /// <summary>
    /// Returns parsed components if the tag matches {app-name}/v{M}.{m}.{p}; null otherwise.
    /// </summary>
    private static (string AppName, int Major, int Minor, int Patch)? ParseTag(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return null;

        var slashIdx = tag.IndexOf('/');
        if (slashIdx <= 0) return null;

        var appName = tag[..slashIdx];
        var versionPart = tag[(slashIdx + 1)..];

        if (!versionPart.StartsWith('v')) return null;

        var semver = versionPart[1..];
        var parts = semver.Split('.');
        if (parts.Length != 3) return null;

        if (!int.TryParse(parts[0], out var major) || major < 0) return null;
        if (!int.TryParse(parts[1], out var minor) || minor < 0) return null;
        if (!int.TryParse(parts[2], out var patch) || patch < 0) return null;

        if (appName.Length == 0 || !char.IsLetter(appName[0])) return null;
        foreach (var c in appName)
            if (!char.IsLetterOrDigit(c) && c != '-') return null;

        return (appName, major, minor, patch);
    }

    // ---------------------------------------------------------------------------
    // Generators
    // ---------------------------------------------------------------------------

    private static readonly string[] KnownAppNames =
        ["wheel-overlay", "discord-notify", "lap-counter", "fuel-gauge", "app1", "a"];

    private static Arbitrary<(string, int, int, int)> TagComponentsArb() =>
        Gen.Elements(KnownAppNames)
            .SelectMany(appName =>
                Gen.Choose(0, 99).SelectMany(major =>
                Gen.Choose(0, 99).SelectMany(minor =>
                Gen.Choose(0, 99).Select(patch =>
                    (appName, major, minor, patch)))))
            .ToArbitrary();

    // ---------------------------------------------------------------------------
    // Property 6a: format → parse recovers all original components
    // ---------------------------------------------------------------------------

    [Property(
#if FAST_TESTS
        MaxTest = 10
#else
        MaxTest = 100
#endif
    )]
    public Property FormatThenParse_RecoverAllComponents()
    {
        return Prop.ForAll(TagComponentsArb(), tuple =>
        {
            var (appName, major, minor, patch) = tuple;
            var tag = FormatTag(appName, major, minor, patch);
            var parsed = ParseTag(tag);

            return (parsed is not null
                && parsed.Value.AppName == appName
                && parsed.Value.Major == major
                && parsed.Value.Minor == minor
                && parsed.Value.Patch == patch)
                .Label($"tag={tag} should round-trip to ({appName},{major},{minor},{patch})");
        });
    }

    // ---------------------------------------------------------------------------
    // Property 6b: parsed components reconstruct the exact original tag string
    // ---------------------------------------------------------------------------

    [Property(
#if FAST_TESTS
        MaxTest = 10
#else
        MaxTest = 100
#endif
    )]
    public Property ParsedComponents_ReconstructExactTag()
    {
        return Prop.ForAll(TagComponentsArb(), tuple =>
        {
            var (appName, major, minor, patch) = tuple;
            var original = FormatTag(appName, major, minor, patch);
            var parsed = ParseTag(original);
            if (parsed is null) return false.Label("valid tag should parse");

            var reconstructed = FormatTag(parsed.Value.AppName, parsed.Value.Major, parsed.Value.Minor, parsed.Value.Patch);
            return (reconstructed == original)
                .Label($"reconstructed={reconstructed} should equal original={original}");
        });
    }

    // ---------------------------------------------------------------------------
    // Property 6c: invalid tag formats do not parse as valid
    // ---------------------------------------------------------------------------

    private static readonly string[] InvalidTags =
    [
        // No app-name prefix
        "v0.7.0", "v1.0.0",
        // Missing 'v'
        "wheel-overlay/0.7.0", "discord-notify/1.2.3",
        // Underscore instead of hyphen in app-name
        "wheel_overlay/v0.7.0", "discord_notify/v1.0.0",
        // Empty / whitespace
        "", " ", "   ",
        // No slash
        "wheeloverlayv070",
        // Wrong version component count
        "wheel-overlay/v0.7", "wheel-overlay/v0.7.0.1",
        // Non-numeric version components
        "wheel-overlay/v0.7.x", "wheel-overlay/va.b.c",
        // App-name starts with digit
        "1app/v0.1.0",
        // App-name contains invalid character
        "wheel.overlay/v0.1.0",
    ];

    [Property(
#if FAST_TESTS
        MaxTest = 10
#else
        MaxTest = 100
#endif
    )]
    public Property InvalidFormats_DoNotParse()
    {
        return Prop.ForAll(
            Gen.Elements(InvalidTags).ToArbitrary(),
            tag => (ParseTag(tag) is null).Label($"'{tag}' should not parse as a valid tag"));
    }
}
