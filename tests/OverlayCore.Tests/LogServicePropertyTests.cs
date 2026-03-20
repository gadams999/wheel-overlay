using FsCheck;
using FsCheck.Xunit;
using OpenDash.OverlayCore.Services;
using System.Reflection;
using System.IO;

namespace OpenDash.OverlayCore.Tests;

/// <summary>
/// Property-based tests for LogService.
/// Validates universal correctness properties across all valid inputs.
/// </summary>
public class LogServicePropertyTests : IDisposable
{
    private readonly string _testAppName;
    private readonly string _testLogPath;

    public LogServicePropertyTests()
    {
        // Use a unique app name for each test run to avoid conflicts
        _testAppName = $"LogServiceTest_{Guid.NewGuid():N}";
        
        // Initialize LogService for testing
        LogService.Initialize(_testAppName);
        _testLogPath = LogService.GetLogPath();
    }

    public void Dispose()
    {
        // Clean up test log file and directory after tests
        try
        {
            if (File.Exists(_testLogPath))
            {
                File.Delete(_testLogPath);
            }

            var dir = Path.GetDirectoryName(_testLogPath);
            if (dir != null && Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
        catch
        {
            // Best effort cleanup
        }

        // Reset LogService state for next test
        ResetLogService();
    }

    /// <summary>
    /// Property 2: Log file never exceeds 1 MB plus one message.
    /// For any sequence of log messages (of arbitrary content and length),
    /// after each call to LogService.Info() or LogService.Error(),
    /// the log file size SHALL be at most 1 MB plus the length of the most recent message.
    /// </summary>
    [Property(
#if FAST_TESTS
        MaxTest = 10,
#else
        MaxTest = 100,
#endif
        Arbitrary = new[] { typeof(LogMessageArbitrary) }
    )]
    public Property LogFileNeverExceedsOneMegabytePlusOneMessage(List<string> messages)
    {
        // Skip empty message lists
        if (messages == null || messages.Count == 0)
        {
            return true.ToProperty();
        }

        // Arrange: Reset log file to known state
        ResetLogService();
        LogService.Initialize(_testAppName);

        const long oneMegabyte = 1 * 1024 * 1024;
        bool allChecksPass = true;
        string? failureMessage = null;

        try
        {
            // Act & Assert: Write each message and verify size constraint
            foreach (var message in messages)
            {
                // Write the message
                LogService.Info(message);

                // Get the actual file size
                var fileInfo = new FileInfo(_testLogPath);
                long actualSize = fileInfo.Exists ? fileInfo.Length : 0;

                // Calculate the formatted message size (includes timestamp, prefix, newline)
                // Format: "[HH:mm:ss.fff] [INFO] {message}\r\n"
                // Approximate: timestamp (13) + brackets (2) + space (1) + [INFO] (7) + space (1) + message + newline (2)
                // Total overhead: ~26 characters + message length
                long formattedMessageSize = 26 + message.Length + Environment.NewLine.Length;

                // The file size should be <= 1 MB + formatted message size
                long maxAllowedSize = oneMegabyte + formattedMessageSize;

                if (actualSize > maxAllowedSize)
                {
                    allChecksPass = false;
                    failureMessage = $"Log file size {actualSize} bytes exceeds maximum allowed {maxAllowedSize} bytes " +
                                   $"(1 MB + message size {formattedMessageSize}). Message length: {message.Length}";
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            allChecksPass = false;
            failureMessage = $"Exception during test: {ex.Message}";
        }

        return allChecksPass
            .When(messages.Count > 0)
            .Label(failureMessage ?? $"Processed {messages.Count} messages successfully");
    }

    /// <summary>
    /// Property 3: Log truncation preserves most recent message.
    /// When the log file exceeds 1 MB and is truncated, the most recent message
    /// must still be present in the log file after truncation.
    /// </summary>
    [Property(
#if FAST_TESTS
        MaxTest = 10,
#else
        MaxTest = 100,
#endif
        Arbitrary = new[] { typeof(LogMessageArbitrary) }
    )]
    public Property LogTruncationPreservesMostRecentMessage(NonEmptyString uniqueMarker)
    {
        // Arrange: Reset log file and fill it to near 1 MB
        ResetLogService();
        LogService.Initialize(_testAppName);

        const long oneMegabyte = 1 * 1024 * 1024;
        
        // Fill log file to just under 1 MB with padding messages
        string paddingMessage = new string('X', 1000); // 1 KB messages
        int messageCount = 0;
        
        while (true)
        {
            var fileInfo = new FileInfo(_testLogPath);
            if (fileInfo.Exists && fileInfo.Length >= oneMegabyte - 10000)
            {
                break; // Stop when we're close to 1 MB
            }
            
            LogService.Info(paddingMessage);
            messageCount++;
            
            // Safety limit to prevent infinite loop
            if (messageCount > 2000)
            {
                break;
            }
        }

        // Act: Write a message with a unique marker that should trigger truncation
        string markerMessage = $"UNIQUE_MARKER_{uniqueMarker.Get}";
        LogService.Info(markerMessage);

        // Assert: The marker message should be present in the log file
        string logContent = File.ReadAllText(_testLogPath);
        bool markerPresent = logContent.Contains(markerMessage);

        return markerPresent
            .Label($"Marker '{markerMessage}' should be present after truncation. " +
                   $"Log size: {new FileInfo(_testLogPath).Length} bytes");
    }

    /// <summary>
    /// Property 4: Multiple rapid writes maintain size constraint.
    /// Even when writing many messages rapidly in sequence, the log file
    /// size constraint must be maintained after each write.
    /// </summary>
    [Property(
#if FAST_TESTS
        MaxTest = 10,
#else
        MaxTest = 100,
#endif
        Arbitrary = new[] { typeof(LogMessageArbitrary) }
    )]
    public Property RapidWritesMaintainSizeConstraint(PositiveInt messageCount)
    {
        // Arrange: Reset log file
        ResetLogService();
        LogService.Initialize(_testAppName);

        const long oneMegabyte = 1 * 1024 * 1024;
        
        // Limit message count to reasonable range for test performance
        int count = Math.Min(messageCount.Get, 500);
        
        // Generate messages of varying sizes
        var random = new System.Random(messageCount.Get);
        var messages = Enumerable.Range(0, count)
            .Select(i => new string('A', random.Next(100, 5000)))
            .ToList();

        bool allChecksPass = true;
        string? failureMessage = null;

        try
        {
            // Act: Write all messages rapidly
            foreach (var message in messages)
            {
                LogService.Info(message);
                
                // Verify size constraint after each write
                var fileInfo = new FileInfo(_testLogPath);
                long actualSize = fileInfo.Exists ? fileInfo.Length : 0;
                
                // Approximate formatted message size
                long formattedMessageSize = 26 + message.Length + Environment.NewLine.Length;
                long maxAllowedSize = oneMegabyte + formattedMessageSize;

                if (actualSize > maxAllowedSize)
                {
                    allChecksPass = false;
                    failureMessage = $"Size constraint violated after rapid write. " +
                                   $"Actual: {actualSize}, Max: {maxAllowedSize}";
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            allChecksPass = false;
            failureMessage = $"Exception during rapid writes: {ex.Message}";
        }

        return allChecksPass
            .Label(failureMessage ?? $"Successfully wrote {count} messages rapidly");
    }

    /// <summary>
    /// Resets the LogService static state using reflection.
    /// This is necessary because LogService is a static class with private state.
    /// </summary>
    private void ResetLogService()
    {
        try
        {
            var logServiceType = typeof(LogService);
            
            // Reset _initialized field
            var initializedField = logServiceType.GetField("_initialized", 
                BindingFlags.NonPublic | BindingFlags.Static);
            initializedField?.SetValue(null, false);
            
            // Reset _logPath field
            var logPathField = logServiceType.GetField("_logPath", 
                BindingFlags.NonPublic | BindingFlags.Static);
            logPathField?.SetValue(null, null);

            // Delete the log file if it exists
            if (File.Exists(_testLogPath))
            {
                File.Delete(_testLogPath);
            }
        }
        catch
        {
            // Best effort reset
        }
    }

    /// <summary>
    /// Custom FsCheck arbitrary generator for log messages.
    /// Generates strings of varying lengths suitable for log message testing.
    /// </summary>
    public class LogMessageArbitrary
    {
        /// <summary>
        /// Generates lists of log messages with varying lengths.
        /// Includes short messages, medium messages, and large messages
        /// to test different scenarios including truncation.
        /// </summary>
        public static Arbitrary<List<string>> LogMessages()
        {
            var shortMessage = Gen.Choose(10, 100)
                .Select(len => new string('a', len));
            
            var mediumMessage = Gen.Choose(1000, 5000)
                .Select(len => new string('b', len));
            
            var largeMessage = Gen.Choose(50000, 200000)
                .Select(len => new string('c', len));

            var messageGen = Gen.OneOf(shortMessage, mediumMessage, largeMessage);
            
            return Gen.Choose(1, 50)
                .SelectMany(count => Gen.ListOf(count, messageGen)
                    .Select(fsList => fsList.ToList()))
                .ToArbitrary();
        }
    }
}
