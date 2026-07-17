using System.Globalization;

namespace Trackify.Domain;

/// <summary>
/// A tiny recursive-descent parser for user speed formulas (x ∈ [0,1]) supporting +-*/^, parentheses,
/// the constants x/pi/e, and sin/cos/tan/sqrt/abs/exp/log/floor/ceil/round/pow/min/max.
/// Used by <see cref="SpeedFunction.TryCompile"/>.
/// </summary>
internal sealed class ExpressionParser
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
