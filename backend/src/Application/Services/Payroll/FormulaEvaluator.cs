using System.Text.RegularExpressions;
using System.Globalization;

namespace Application.Services.Payroll;

/// <summary>
/// Safe formula evaluator for payroll calculations.
/// Supports basic math operations, comparison operators, and common functions.
/// </summary>
public class FormulaEvaluator
{
    private readonly Dictionary<string, decimal> _variables;
    private readonly List<string> _usedVariables = new();
    private string _expression = string.Empty;
    private int _position;

    // Supported functions
    private static readonly HashSet<string> SupportedFunctions = new(StringComparer.OrdinalIgnoreCase)
    {
        "MIN", "MAX", "ROUND", "FLOOR", "CEILING", "ABS", "IF"
    };

    public FormulaEvaluator()
    {
        _variables = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
    }

    public FormulaEvaluator(Dictionary<string, decimal> variables)
    {
        _variables = new Dictionary<string, decimal>(variables, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Set a variable value
    /// </summary>
    public void SetVariable(string name, decimal value)
    {
        _variables[name] = value;
    }

    /// <summary>
    /// Get list of variables used in the last evaluation
    /// </summary>
    public IReadOnlyList<string> UsedVariables => _usedVariables.AsReadOnly();

    /// <summary>
    /// Evaluate a formula expression
    /// </summary>
    public decimal Evaluate(string expression)
    {
        _expression = expression.Trim();
        _position = 0;
        _usedVariables.Clear();

        if (string.IsNullOrEmpty(_expression))
            throw new FormulaException("Expression cannot be empty");

        var result = ParseExpression();

        SkipWhitespace();
        if (_position < _expression.Length)
            throw new FormulaException($"Unexpected character '{_expression[_position]}' at position {_position}");

        return result;
    }

    /// <summary>
    /// Validate a formula expression without evaluating it
    /// </summary>
    public FormulaValidationResult Validate(string expression)
    {
        try
        {
            // Set dummy values for all known variables
            var dummyVariables = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            var variablePattern = new Regex(@"\b([a-z_][a-z0-9_]*)\b", RegexOptions.IgnoreCase);
            var matches = variablePattern.Matches(expression);

            foreach (Match match in matches)
            {
                var name = match.Value;
                if (!SupportedFunctions.Contains(name) && !IsKeyword(name))
                {
                    dummyVariables[name] = 10000m; // Dummy value
                }
            }

            var evaluator = new FormulaEvaluator(dummyVariables);
            var result = evaluator.Evaluate(expression);

            return new FormulaValidationResult
            {
                IsValid = true,
                UsedVariables = evaluator.UsedVariables.ToList(),
                SampleResult = result
            };
        }
        catch (FormulaException ex)
        {
            return new FormulaValidationResult
            {
                IsValid = false,
                ErrorMessage = ex.Message
            };
        }
        catch (Exception ex)
        {
            return new FormulaValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Validation error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Extract all variable names from an expression
    /// </summary>
    public static List<string> ExtractVariables(string expression)
    {
        var variables = new List<string>();
        var variablePattern = new Regex(@"\b([a-z_][a-z0-9_]*)\b", RegexOptions.IgnoreCase);
        var matches = variablePattern.Matches(expression);

        foreach (Match match in matches)
        {
            var name = match.Value;
            if (!SupportedFunctions.Contains(name) && !IsKeyword(name) && !variables.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                variables.Add(name);
            }
        }

        return variables;
    }

    private static bool IsKeyword(string name)
    {
        return name.Equals("true", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("false", StringComparison.OrdinalIgnoreCase);
    }

    // ===================== Parser Implementation =====================

    private decimal ParseExpression()
    {
        return ParseTernary();
    }

    private decimal ParseTernary()
    {
        // Handle IF(condition, trueValue, falseValue) as a function
        // Also handle ternary: condition ? trueValue : falseValue
        var left = ParseOr();

        SkipWhitespace();
        if (_position < _expression.Length && _expression[_position] == '?')
        {
            _position++;
            var trueValue = ParseExpression();
            SkipWhitespace();
            Expect(':');
            var falseValue = ParseExpression();
            return left != 0 ? trueValue : falseValue;
        }

        return left;
    }

    private decimal ParseOr()
    {
        var left = ParseAnd();

        while (true)
        {
            SkipWhitespace();
            if (MatchKeyword("OR") || Match("||"))
            {
                var right = ParseAnd();
                left = (left != 0 || right != 0) ? 1 : 0;
            }
            else
            {
                break;
            }
        }

        return left;
    }

    private decimal ParseAnd()
    {
        var left = ParseComparison();

        while (true)
        {
            SkipWhitespace();
            if (MatchKeyword("AND") || Match("&&"))
            {
                var right = ParseComparison();
                left = (left != 0 && right != 0) ? 1 : 0;
            }
            else
            {
                break;
            }
        }

        return left;
    }

    private decimal ParseComparison()
    {
        var left = ParseAddition();

        SkipWhitespace();
        if (Match(">="))
        {
            return left >= ParseAddition() ? 1 : 0;
        }
        if (Match("<="))
        {
            return left <= ParseAddition() ? 1 : 0;
        }
        if (Match("!=") || Match("<>"))
        {
            return left != ParseAddition() ? 1 : 0;
        }
        if (Match("==") || (Peek() == '=' && !Match("=")))
        {
            if (Peek() == '=') _position++;
            return left == ParseAddition() ? 1 : 0;
        }
        if (Match(">"))
        {
            return left > ParseAddition() ? 1 : 0;
        }
        if (Match("<"))
        {
            return left < ParseAddition() ? 1 : 0;
        }

        return left;
    }

    private decimal ParseAddition()
    {
        var left = ParseMultiplication();

        while (true)
        {
            SkipWhitespace();
            if (Match("+"))
            {
                left += ParseMultiplication();
            }
            else if (Match("-"))
            {
                left -= ParseMultiplication();
            }
            else
            {
                break;
            }
        }

        return left;
    }

    private decimal ParseMultiplication()
    {
        var left = ParseUnary();

        while (true)
        {
            SkipWhitespace();
            if (Match("*"))
            {
                left *= ParseUnary();
            }
            else if (Match("/"))
            {
                var right = ParseUnary();
                if (right == 0)
                    throw new FormulaException("Division by zero");
                left /= right;
            }
            else if (Match("%"))
            {
                var right = ParseUnary();
                if (right == 0)
                    throw new FormulaException("Modulo by zero");
                left %= right;
            }
            else
            {
                break;
            }
        }

        return left;
    }

    private decimal ParseUnary()
    {
        SkipWhitespace();

        if (Match("-"))
        {
            return -ParseUnary();
        }
        if (Match("+"))
        {
            return ParseUnary();
        }
        if (Match("!") || MatchKeyword("NOT"))
        {
            return ParseUnary() == 0 ? 1 : 0;
        }

        return ParsePrimary();
    }

    private decimal ParsePrimary()
    {
        SkipWhitespace();

        // Parentheses
        if (Match("("))
        {
            var result = ParseExpression();
            SkipWhitespace();
            Expect(')');
            return result;
        }

        // Number
        if (char.IsDigit(Peek()) || (Peek() == '.' && _position + 1 < _expression.Length && char.IsDigit(_expression[_position + 1])))
        {
            return ParseNumber();
        }

        // Identifier (variable or function)
        if (char.IsLetter(Peek()) || Peek() == '_')
        {
            return ParseIdentifier();
        }

        throw new FormulaException($"Unexpected character '{Peek()}' at position {_position}");
    }

    private decimal ParseNumber()
    {
        var start = _position;
        while (_position < _expression.Length && (char.IsDigit(_expression[_position]) || _expression[_position] == '.'))
        {
            _position++;
        }

        var numberStr = _expression.Substring(start, _position - start);
        if (!decimal.TryParse(numberStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            throw new FormulaException($"Invalid number: {numberStr}");

        return value;
    }

    private decimal ParseIdentifier()
    {
        var start = _position;
        while (_position < _expression.Length && (char.IsLetterOrDigit(_expression[_position]) || _expression[_position] == '_'))
        {
            _position++;
        }

        var name = _expression.Substring(start, _position - start);

        // Boolean literals
        if (name.Equals("true", StringComparison.OrdinalIgnoreCase))
            return 1;
        if (name.Equals("false", StringComparison.OrdinalIgnoreCase))
            return 0;

        // Function call
        SkipWhitespace();
        if (Peek() == '(')
        {
            return ParseFunctionCall(name);
        }

        // Variable
        if (!_variables.TryGetValue(name, out var value))
            throw new FormulaException($"Unknown variable: {name}");

        if (!_usedVariables.Contains(name, StringComparer.OrdinalIgnoreCase))
            _usedVariables.Add(name);

        return value;
    }

    private decimal ParseFunctionCall(string functionName)
    {
        Expect('(');
        var args = new List<decimal>();

        SkipWhitespace();
        if (Peek() != ')')
        {
            args.Add(ParseExpression());

            while (true)
            {
                SkipWhitespace();
                if (Match(","))
                {
                    args.Add(ParseExpression());
                }
                else
                {
                    break;
                }
            }
        }

        SkipWhitespace();
        Expect(')');

        return EvaluateFunction(functionName, args);
    }

    private decimal EvaluateFunction(string name, List<decimal> args)
    {
        switch (name.ToUpperInvariant())
        {
            case "MIN":
                if (args.Count < 2)
                    throw new FormulaException("MIN requires at least 2 arguments");
                return args.Min();

            case "MAX":
                if (args.Count < 2)
                    throw new FormulaException("MAX requires at least 2 arguments");
                return args.Max();

            case "ROUND":
                if (args.Count < 1)
                    throw new FormulaException("ROUND requires at least 1 argument");
                var decimals = args.Count > 1 ? (int)args[1] : 0;
                return Math.Round(args[0], decimals, MidpointRounding.AwayFromZero);

            case "FLOOR":
                if (args.Count != 1)
                    throw new FormulaException("FLOOR requires exactly 1 argument");
                return Math.Floor(args[0]);

            case "CEILING":
                if (args.Count != 1)
                    throw new FormulaException("CEILING requires exactly 1 argument");
                return Math.Ceiling(args[0]);

            case "ABS":
                if (args.Count != 1)
                    throw new FormulaException("ABS requires exactly 1 argument");
                return Math.Abs(args[0]);

            case "IF":
                if (args.Count != 3)
                    throw new FormulaException("IF requires exactly 3 arguments: IF(condition, trueValue, falseValue)");
                return args[0] != 0 ? args[1] : args[2];

            default:
                throw new FormulaException($"Unknown function: {name}");
        }
    }

    // ===================== Helper Methods =====================

    private void SkipWhitespace()
    {
        while (_position < _expression.Length && char.IsWhiteSpace(_expression[_position]))
            _position++;
    }

    private char Peek()
    {
        return _position < _expression.Length ? _expression[_position] : '\0';
    }

    private bool Match(string expected)
    {
        if (_position + expected.Length <= _expression.Length &&
            _expression.Substring(_position, expected.Length) == expected)
        {
            _position += expected.Length;
            return true;
        }
        return false;
    }

    private bool MatchKeyword(string keyword)
    {
        var start = _position;
        if (_position + keyword.Length <= _expression.Length &&
            _expression.Substring(_position, keyword.Length).Equals(keyword, StringComparison.OrdinalIgnoreCase))
        {
            // Make sure it's not part of a larger identifier
            var nextPos = _position + keyword.Length;
            if (nextPos >= _expression.Length || !char.IsLetterOrDigit(_expression[nextPos]))
            {
                _position = nextPos;
                return true;
            }
        }
        return false;
    }

    private void Expect(char expected)
    {
        SkipWhitespace();
        if (_position >= _expression.Length || _expression[_position] != expected)
            throw new FormulaException($"Expected '{expected}' at position {_position}");
        _position++;
    }
}

/// <summary>
/// Exception thrown when formula evaluation fails
/// </summary>
public class FormulaException : Exception
{
    public FormulaException(string message) : base(message) { }
}

/// <summary>
/// Result of formula validation
/// </summary>
public class FormulaValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public decimal? SampleResult { get; set; }
    public List<string> UsedVariables { get; set; } = new();
}
