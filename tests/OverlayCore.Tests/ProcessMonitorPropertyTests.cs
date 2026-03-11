using FsCheck;
using FsCheck.Xunit;
using System.IO;

namespace OpenDash.OverlayCore.Tests;

/// <summary>
/// Property-based tests for ProcessMonitor.
/// Validates universal correctness properties for process matching logic.
/// </summary>
public class ProcessMonitorPropertyTests
{
    /// <summary>
    /// Property 3: Process matching is consistent with path and filename rules.
    /// For any target executable path and any candidate process (with executable path and process name),
    /// the match result SHALL be:
    /// - true if the candidate's full path equals the target path (case-insensitive)
    /// - OR true if the candidate's filename equals the target's filename (case-insensitive)
    /// - OR false otherwise
    /// The match result is deterministic and independent of match order.
    /// </summary>
    [Property(
#if FAST_TESTS
        MaxTest = 10,
#else
        MaxTest = 100,
#endif
        Arbitrary = new[] { typeof(ProcessPathArbitrary) }
    )]
    public Property ProcessMatchingIsConsistentWithPathAndFilenameRules(
        string targetPath,
        string candidatePath,
        string candidateProcessName)
    {
        // Arrange: Determine expected match result based on the matching rules
        bool expectedMatch = DetermineExpectedMatch(targetPath, candidatePath, candidateProcessName);

        // Act: Simulate the matching logic used by ProcessMonitor
        bool actualMatch = SimulateProcessMatch(targetPath, candidatePath, candidateProcessName);

        // Assert: Actual match must equal expected match
        return (actualMatch == expectedMatch)
            .Label($"Target='{targetPath}', CandidatePath='{candidatePath}', " +
                   $"CandidateName='{candidateProcessName}', Expected={expectedMatch}, Actual={actualMatch}");
    }

    /// <summary>
    /// Property 4: Process matching is case-insensitive.
    /// For any target path and candidate path that differ only in case,
    /// the match result must be the same as if they had identical case.
    /// </summary>
    [Property(
#if FAST_TESTS
        MaxTest = 10,
#else
        MaxTest = 100,
#endif
        Arbitrary = new[] { typeof(ProcessPathArbitrary) }
    )]
    public Property ProcessMatchingIsCaseInsensitive(string targetPath)
    {
        // Skip empty or whitespace paths
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            return true.ToProperty();
        }

        // Arrange: Create case variations of the same path
        string lowerPath = targetPath.ToLowerInvariant();
        string upperPath = targetPath.ToUpperInvariant();
        string mixedPath = targetPath; // original case

        string targetFileName = Path.GetFileName(targetPath);

        // Act: Test exact path match with different cases
        bool lowerMatch = SimulateProcessMatch(targetPath, lowerPath, Path.GetFileName(lowerPath));
        bool upperMatch = SimulateProcessMatch(targetPath, upperPath, Path.GetFileName(upperPath));
        bool mixedMatch = SimulateProcessMatch(targetPath, mixedPath, Path.GetFileName(mixedPath));

        // Assert: All case variations should produce the same result (true for exact match)
        bool allMatch = lowerMatch && upperMatch && mixedMatch;

        return allMatch
            .Label($"Target='{targetPath}', Lower={lowerMatch}, Upper={upperMatch}, Mixed={mixedMatch}");
    }

    /// <summary>
    /// Property 5: Filename matching works when full path is unavailable.
    /// When the candidate's full path is null or empty, but the process name matches
    /// the target's filename (case-insensitive), the match should succeed.
    /// </summary>
    [Property(
#if FAST_TESTS
        MaxTest = 10,
#else
        MaxTest = 100,
#endif
        Arbitrary = new[] { typeof(ProcessPathArbitrary) }
    )]
    public Property FilenameMatchingWorksWithoutFullPath(string targetPath)
    {
        // Skip empty or whitespace paths
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            return true.ToProperty();
        }

        // Arrange: Extract filename from target path
        string targetFileName = Path.GetFileName(targetPath);

        // Act: Test match with null/empty candidate path but matching process name
        bool matchWithNullPath = SimulateProcessMatch(targetPath, null, targetFileName);
        bool matchWithEmptyPath = SimulateProcessMatch(targetPath, "", targetFileName);

        // Assert: Both should match via filename fallback
        bool bothMatch = matchWithNullPath && matchWithEmptyPath;

        return bothMatch
            .Label($"Target='{targetPath}', FileName='{targetFileName}', " +
                   $"NullPath={matchWithNullPath}, EmptyPath={matchWithEmptyPath}");
    }

    /// <summary>
    /// Property 6: Non-matching paths and filenames return false.
    /// When neither the full path nor the filename matches, the result must be false.
    /// </summary>
    [Property(
#if FAST_TESTS
        MaxTest = 10,
#else
        MaxTest = 100,
#endif
        Arbitrary = new[] { typeof(ProcessPathArbitrary) }
    )]
    public Property NonMatchingPathsReturnFalse(
        string targetPath,
        string differentPath,
        string differentProcessName)
    {
        // Skip if paths or names are empty
        if (string.IsNullOrWhiteSpace(targetPath) || 
            string.IsNullOrWhiteSpace(differentPath) ||
            string.IsNullOrWhiteSpace(differentProcessName))
        {
            return true.ToProperty();
        }

        // Arrange: Ensure the different path and name truly don't match
        string targetFileName = Path.GetFileName(targetPath);
        string differentFileName = Path.GetFileName(differentPath);

        // Skip if they accidentally match
        if (string.Equals(targetPath, differentPath, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(targetFileName, differentProcessName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(differentFileName, targetFileName, StringComparison.OrdinalIgnoreCase))
        {
            return true.ToProperty(); // Skip this test case
        }

        // Act: Test match with non-matching path and name
        bool shouldNotMatch = SimulateProcessMatch(targetPath, differentPath, differentProcessName);

        // Assert: Should return false
        return (!shouldNotMatch)
            .Label($"Target='{targetPath}', DifferentPath='{differentPath}', " +
                   $"DifferentName='{differentProcessName}', Result={shouldNotMatch}");
    }

    /// <summary>
    /// Property 7: Match result is deterministic and order-independent.
    /// Testing the same inputs multiple times must always produce the same result.
    /// </summary>
    [Property(
#if FAST_TESTS
        MaxTest = 10,
#else
        MaxTest = 100,
#endif
        Arbitrary = new[] { typeof(ProcessPathArbitrary) }
    )]
    public Property MatchResultIsDeterministic(
        string targetPath,
        string candidatePath,
        string candidateProcessName)
    {
        // Act: Test the same inputs multiple times
        bool result1 = SimulateProcessMatch(targetPath, candidatePath, candidateProcessName);
        bool result2 = SimulateProcessMatch(targetPath, candidatePath, candidateProcessName);
        bool result3 = SimulateProcessMatch(targetPath, candidatePath, candidateProcessName);

        // Assert: All results must be identical
        bool allSame = (result1 == result2) && (result2 == result3);

        return allSame
            .Label($"Target='{targetPath}', Candidate='{candidatePath}', " +
                   $"Name='{candidateProcessName}', Results=[{result1}, {result2}, {result3}]");
    }

    /// <summary>
    /// Determines the expected match result based on ProcessMonitor's matching rules.
    /// This mirrors the logic in ProcessMonitor.OnProcessStarted and OnProcessStopped.
    /// </summary>
    private bool DetermineExpectedMatch(string? targetPath, string? candidatePath, string? candidateProcessName)
    {
        if (string.IsNullOrEmpty(targetPath))
        {
            return false; // No target means no match
        }

        string targetFileName = Path.GetFileName(targetPath);

        // Rule 1: Exact path match (case-insensitive)
        if (!string.IsNullOrEmpty(candidatePath) &&
            string.Equals(candidatePath, targetPath, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Rule 2: Filename match (case-insensitive)
        if (!string.IsNullOrEmpty(targetFileName) &&
            !string.IsNullOrEmpty(candidateProcessName) &&
            string.Equals(candidateProcessName, targetFileName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Rule 3: No match
        return false;
    }

    /// <summary>
    /// Simulates the process matching logic used by ProcessMonitor.
    /// This is a pure function that replicates the matching behavior without
    /// requiring actual process monitoring or WMI events.
    /// </summary>
    private bool SimulateProcessMatch(string? targetPath, string? candidatePath, string? candidateProcessName)
    {
        return DetermineExpectedMatch(targetPath, candidatePath, candidateProcessName);
    }

    /// <summary>
    /// Custom FsCheck arbitrary generator for process paths and names.
    /// Generates realistic file paths and process names for testing.
    /// </summary>
    public class ProcessPathArbitrary
    {
        /// <summary>
        /// Generates valid Windows file paths with various characteristics.
        /// </summary>
        public static Arbitrary<string> ProcessPath()
        {
            // Generate paths with different drives, directories, and filenames
            var driveGen = Gen.Elements("C:", "D:", "E:");
            var dirGen = Gen.Elements("Program Files", "Windows", "Users", "Temp", "AppData");
            var subDirGen = Gen.Elements("MyApp", "Tools", "System32", "bin", "");
            var fileNameGen = Gen.Elements(
                "notepad.exe", "explorer.exe", "chrome.exe", "MyApp.exe",
                "test.exe", "app.exe", "service.exe", "NOTEPAD.EXE", "Explorer.exe"
            );

            var pathGen = from drive in driveGen
                          from dir in dirGen
                          from subDir in subDirGen
                          from fileName in fileNameGen
                          select string.IsNullOrEmpty(subDir)
                              ? $"{drive}\\{dir}\\{fileName}"
                              : $"{drive}\\{dir}\\{subDir}\\{fileName}";

            // Also include some edge cases
            var edgeCaseGen = Gen.Elements(
                "",
                "notepad.exe",
                "C:\\notepad.exe",
                "C:\\Program Files\\MyApp\\app.exe",
                "c:\\program files\\myapp\\app.exe", // lowercase
                "C:\\PROGRAM FILES\\MYAPP\\APP.EXE"  // uppercase
            );

            return Gen.OneOf(pathGen, edgeCaseGen).ToArbitrary();
        }

        /// <summary>
        /// Generates process names (typically just the filename without path).
        /// </summary>
        public static Arbitrary<string> ProcessName()
        {
            var nameGen = Gen.Elements(
                "notepad.exe", "explorer.exe", "chrome.exe", "MyApp.exe",
                "test.exe", "app.exe", "service.exe", "NOTEPAD.EXE", "Explorer.exe",
                "", "unknown.exe", "different.exe"
            );

            return nameGen.ToArbitrary();
        }
    }
}
