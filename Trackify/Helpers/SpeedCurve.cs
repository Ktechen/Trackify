using System.Globalization;

namespace Trackify.Helpers;

/// <summary>Evaluates the acceleration/braking f(x) profiles used by the train configurator and the track planner.</summary>
public static class SpeedCurve
{
    private static readonly double[] ProbePoints = [0d, 0.5, 1d];

    public static double Evaluate(SpeedFunctionType type, double x) => type switch
    {
        SpeedFunctionType.Linear => x,
        SpeedFunctionType.EaseIn => x * x,
        SpeedFunctionType.EaseOut => 1 - (1 - x) * (1 - x),
        SpeedFunctionType.SCurve => x * x * (3 - 2 * x),
        SpeedFunctionType.Exponential => (Math.Exp(3 * x) - 1) / (Math.Exp(3) - 1),
        _ => x,
    };

    /// <summary>Compiles a user-entered formula (x ∈ [0,1], +-*/^, sin/cos/sqrt/exp/pi …) and validates it at x=0, .5, 1.</summary>
    public static bool TryCompile(string? expression, out Func<double, double>? fn)
    {
        fn = null;
        if (string.IsNullOrWhiteSpace(expression)) return false;
        try
        {
            var parser = new ExpressionParser(expression);
            var parsed = parser.ParseExpression();
            parser.ExpectEnd();
            foreach (var probe in ProbePoints)
            {
                var r = parsed(probe);
                if (double.IsNaN(r) || double.IsInfinity(r)) return false;
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

    public static SpeedProfileGraph BuildGraph(double maxNormalized, Func<double, double> accelAt, Func<double, double> brakeAt)
    {
        const double gw = 300, gh = 130, pl = 34, pr = 12, ptp = 12;
        const double plotW = gw - pl - pr, plotH = gh - ptp - 24;
        const double a = 0.34, bs = 0.66;

        double XAt(double u) => pl + plotW * u;
        double YAt(double v) => ptp + plotH * (1 - v);
        static double Clamp01(double v) => Math.Max(0, Math.Min(1, v));

        var points = new (double X, double Y)[65];
        for (var i = 0; i <= 64; i++)
        {
            var u = i / 64.0;
            double v;
            if (u < a) v = maxNormalized * accelAt(u / a);
            else if (u < bs) v = maxNormalized;
            else v = maxNormalized * (1 - brakeAt((u - bs) / (1 - bs)));
            points[i] = (XAt(u), YAt(Clamp01(v)));
        }

        string F(double d) => d.ToString("0.0", CultureInfo.InvariantCulture);

        var line = "M " + string.Join(" L ", points.Select(p => $"{F(p.X)} {F(p.Y)}"));
        var fill = $"{line} L {F(XAt(1))} {F(YAt(0))} L {F(XAt(0))} {F(YAt(0))} Z";

        return new SpeedProfileGraph(gw, gh, pl, gw - pr, ptp, XAt(a), XAt(bs), YAt(0), YAt(0.5), YAt(1), line, fill);
    }

    private sealed class ExpressionParser
    {
        private readonly string _s;
        private int _pos;

        public ExpressionParser(string expression) => _s = expression.Replace(" ", "").Replace("^", "†");

        public void ExpectEnd()
        {
            if (_pos != _s.Length) throw new FormatException("Unexpected trailing input");
        }

        public Func<double, double> ParseExpression()
        {
            var left = ParseTerm();
            while (Peek() is '+' or '-')
            {
                var op = Next();
                var right = ParseTerm();
                var prevLeft = left;
                left = op == '+' ? x => prevLeft(x) + right(x) : x => prevLeft(x) - right(x);
            }
            return left;
        }

        private Func<double, double> ParseTerm()
        {
            var left = ParsePower();
            while (Peek() is '*' or '/')
            {
                var op = Next();
                var right = ParsePower();
                var prevLeft = left;
                left = op == '*' ? x => prevLeft(x) * right(x) : x => prevLeft(x) / right(x);
            }
            return left;
        }

        private Func<double, double> ParsePower()
        {
            var left = ParseUnary();
            if (Peek() != '†') return left;
            Next();
            var right = ParsePower();
            var prevLeft = left;
            return x => Math.Pow(prevLeft(x), right(x));
        }

        private Func<double, double> ParseUnary()
        {
            if (Peek() == '-')
            {
                Next();
                var operand = ParseUnary();
                return x => -operand(x);
            }
            if (Peek() == '+')
            {
                Next();
                return ParseUnary();
            }
            return ParsePrimary();
        }

        private Func<double, double> ParsePrimary()
        {
            var c = Peek();
            if (c == '(')
            {
                Next();
                var inner = ParseExpression();
                Expect(')');
                return inner;
            }
            if (char.IsDigit(c) || c == '.') return ParseNumber();
            if (char.IsLetter(c)) return ParseIdentifierOrCall();
            throw new FormatException($"Unexpected character '{c}' at position {_pos}");
        }

        private Func<double, double> ParseNumber()
        {
            var start = _pos;
            while (_pos < _s.Length && (char.IsDigit(_s[_pos]) || _s[_pos] == '.')) _pos++;
            var value = double.Parse(_s[start.._pos], CultureInfo.InvariantCulture);
            return _ => value;
        }

        private Func<double, double> ParseIdentifierOrCall()
        {
            var start = _pos;
            while (_pos < _s.Length && char.IsLetter(_s[_pos])) _pos++;
            var name = _s[start.._pos].ToLowerInvariant();

            if (Peek() != '(')
            {
                return name switch
                {
                    "x" => x => x,
                    "pi" => _ => Math.PI,
                    "e" => _ => Math.E,
                    _ => throw new FormatException($"Unknown identifier '{name}'"),
                };
            }

            Next();
            var args = new List<Func<double, double>> { ParseExpression() };
            while (Peek() == ',')
            {
                Next();
                args.Add(ParseExpression());
            }
            Expect(')');
            return CallFunction(name, args);
        }

        private static Func<double, double> CallFunction(string name, List<Func<double, double>> args)
        {
            Func<double, double> Arg(int i) => args[i];
            return name switch
            {
                "sin" => x => Math.Sin(Arg(0)(x)),
                "cos" => x => Math.Cos(Arg(0)(x)),
                "tan" => x => Math.Tan(Arg(0)(x)),
                "sqrt" => x => Math.Sqrt(Arg(0)(x)),
                "abs" => x => Math.Abs(Arg(0)(x)),
                "exp" => x => Math.Exp(Arg(0)(x)),
                "log" => x => Math.Log(Arg(0)(x)),
                "floor" => x => Math.Floor(Arg(0)(x)),
                "ceil" => x => Math.Ceiling(Arg(0)(x)),
                "round" => x => Math.Round(Arg(0)(x)),
                "pow" when args.Count == 2 => x => Math.Pow(Arg(0)(x), Arg(1)(x)),
                "min" when args.Count == 2 => x => Math.Min(Arg(0)(x), Arg(1)(x)),
                "max" when args.Count == 2 => x => Math.Max(Arg(0)(x), Arg(1)(x)),
                _ => throw new FormatException($"Unknown function '{name}'"),
            };
        }

        private char Peek() => _pos < _s.Length ? _s[_pos] : '\0';
        private char Next() => _s[_pos++];

        private void Expect(char c)
        {
            if (Peek() != c) throw new FormatException($"Expected '{c}' at position {_pos}");
            _pos++;
        }
    }
}

public readonly record struct SpeedProfileGraph(
    double Width,
    double Height,
    double PlotLeft,
    double PlotRight,
    double PlotTop,
    double XAccelEnd,
    double XBrakeStart,
    double Y0,
    double YMid,
    double Y1,
    string LinePathData,
    string FillPathData);
