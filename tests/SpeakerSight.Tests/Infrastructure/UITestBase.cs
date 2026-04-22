using Xunit;

namespace OpenDash.SpeakerSight.Tests.Infrastructure;

/// <summary>
/// Base class for WPF STA-thread tests.
/// Inherit from this class and annotate test methods with [StaFact] or [StaTheory]
/// (from the Xunit.StaFact package) to ensure tests run on an STA thread,
/// which is required for creating WPF UI elements (UIElement, DependencyObject, etc.).
/// </summary>
public abstract class UITestBase
{
    // Marker class — StaFact/StaTheory attributes handle the STA thread setup.
    // Shared helper utilities for WPF UI tests can be added here as needed.
}
