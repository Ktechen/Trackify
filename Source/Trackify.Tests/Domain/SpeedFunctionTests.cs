using Trackify.Domain;
using Trackify.Domain.Enums;
using Xunit;

namespace Trackify.Tests.Domain;

public class SpeedFunctionTests
{
    [Theory]
    [InlineData(SpeedFunctionType.Linear, 0.5, 0.5)]
    [InlineData(SpeedFunctionType.EaseIn, 0.5, 0.25)]
    [InlineData(SpeedFunctionType.EaseOut, 0.0, 0.0)]
    public void Evaluate_matches_known_points(SpeedFunctionType type, double x, double expected)
        => Assert.Equal(expected, SpeedFunction.Evaluate(type, x), 3);

    [Fact]
    public void TryCompile_accepts_a_valid_formula()
    {
        Assert.True(SpeedFunction.TryCompile("1-(1-x)^2", out var fn));
        Assert.NotNull(fn);
        Assert.Equal(0.0, fn!(0), 3);
        Assert.Equal(1.0, fn(1), 3);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1/0*x+")]
    [InlineData("frobnicate(x)")]
    public void TryCompile_rejects_invalid_formulas(string expression)
        => Assert.False(SpeedFunction.TryCompile(expression, out _));

    [Fact]
    public void ResolvePhaseFunction_falls_back_to_identity_for_invalid_custom()
    {
        var fn = SpeedFunction.ResolvePhaseFunction(SpeedFunctionType.Custom, "not a formula");
        Assert.Equal(0.42, fn(0.42), 3);
    }
}
