using FsCheck;

namespace OpenDash.SpeakerSight.Tests.Infrastructure;

/// <summary>
/// Shared FsCheck configuration for property tests.
/// Fast (PR) mode: 10 iterations. Release mode: 100 iterations.
/// </summary>
public static class TestConfiguration
{
#if FAST_TESTS
    public const int Iterations = 10;
#else
    public const int Iterations = 100;
#endif

    public static Configuration FsCheckConfig => new()
    {
        MaxNbOfTest = Iterations
    };
}
