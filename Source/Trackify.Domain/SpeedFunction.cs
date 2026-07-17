using Trackify.Domain.Enums;

namespace Trackify.Domain;

/// <summary>
/// Evaluates the acceleration/braking f(x) profiles used by the train configurator and the track
/// planner. Pure math (no UI): the SVG graph builder that consumes these lives in the presentation layer.
/// </summary>
public static class SpeedFunction
{
    private static readonly double[] ProbePoints = [0d, 0.5, 1d];

    public static double Evaluate(SpeedFunctionType type, double x)
    {
        return type switch
        {
            SpeedFunctionType.Linear => x,
            SpeedFunctionType.EaseIn => x * x,
            SpeedFunctionType.EaseOut => 1 - (1 - x) * (1 - x),
            SpeedFunctionType.SCurve => x * x * (3 - 2 * x),
            SpeedFunctionType.Exponential => (Math.Exp(3 * x) - 1) / (Math.Exp(3) - 1),
            SpeedFunctionType.Custom => throw new NotImplementedException(),
            _ => x,
        };
    }

    /// <summary>Compiles a user-entered formula (x ∈ [0,1], +-*/^, sin/cos/sqrt/exp/pi …) and validates it at x=0, .5, 1.</summary>
    public static bool TryCompile(string? expression, out Func<double, double>? fn)
    {
        fn = null;
        if (string.IsNullOrWhiteSpace(expression))
        {
            return false;
        }

        try
        {
            var parser = new ExpressionParser(expression);
            var parsed = parser.ParseExpression();
            parser.ExpectEnd();
            if (ProbePoints.Select(probe => parsed(probe)).Any(r => double.IsNaN(r) || double.IsInfinity(r)))
            {
                return false;
            }
            fn = parsed;
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Resolves a phase (accel/brake) to a 0..1 function, falling back to linear (identity) if a custom formula is invalid.</summary>
    public static Func<double, double> ResolvePhaseFunction(SpeedFunctionType fn, string? expression)
    {
        if (fn != SpeedFunctionType.Custom) return x => Evaluate(fn, x);
        return TryCompile(expression, out var compiled) && compiled is not null
            ? x => Math.Clamp(compiled(x), 0, 1)
            : x => x;
    }
}
