using Uno.Extensions.Navigation;

namespace Trackify.Ui.Tests.Fakes;

/// <summary>
/// Stands in for Uno's navigator. The ViewModel only ever calls <c>NavigateBackAsync</c>, which is an
/// extension method over <see cref="NavigateAsync"/>, so recording the requests is enough.
/// </summary>
public sealed class FakeNavigator : INavigator
{
    public List<NavigationRequest> Requests { get; } = [];

    public Route? Route => null;

    public Task<bool> CanNavigate(Route route) => Task.FromResult(true);

    public Task<NavigationResponse?> NavigateAsync(NavigationRequest request)
    {
        Requests.Add(request);

        return Task.FromResult<NavigationResponse?>(null);
    }
}
