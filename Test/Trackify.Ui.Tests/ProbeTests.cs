using Trackify.Domain.Tracks;
using Trackify.Helpers;
using Xunit;

namespace Trackify.Ui.Tests;

/// <summary>
/// Proves the test host can load the app head at all. If these fail, nothing else in this project
/// can work and the problem is the project setup, not the code under test.
/// </summary>
public class ProbeTests
{
    [Fact]
    public void The_domain_is_reachable()
        => Assert.Equal(16, TrackMetrics.HeadingCount);

    [Fact]
    public void The_app_heads_own_code_is_reachable()
        => Assert.Empty(TrackPlanFitter.Fit(TrackPlanLayout.Empty).Pieces);
}
