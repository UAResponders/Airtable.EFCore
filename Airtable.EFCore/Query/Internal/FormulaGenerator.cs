using System.Globalization;
using System.Linq.Expressions;
using System.Text;

namespace Airtable.EFCore.Query.Internal;

internal sealed class FormulaGenerator : FormulaExpressionVisitor
{
    private readonly StringBuilder _builder = new();
    private readonly IReadOnlyDictionary<string, object?> _parameters;

    public FormulaGenerator(IReadOnlyDictionary<string, object?> parameters)
    {
        _parameters = parameters;
    }

    protected override Expression VisitTableProperty(TablePropertyReferenceExpression tableProperty)
    {
        _builder.Append('{');
        _builder.Append(tableProperty.Name);
        _builder.Append('}');

        return tableProperty;
    }

    protected override Expression VisitFunction(FormulaCallExpression callExpression)
    {
        _builder.Append(callExpression.FormulaName);
        _builder.Append("(");
        bool isFirst = true;
        foreach (var item in callExpression.Arguments)
        {
            if (isFirst) isFirst = false;
            else _builder.Append(',');

            Visit(item);
        }

        _builder.Append(")");
        return callExpression;
    }

    protected override Expression VisitBinary(FormulaBinaryExpression binaryExpression)
    {
        _builder.Append("(");
        Visit(binaryExpression.Left);

        var op = (binaryExpression.OperatorType) switch
        {
            ExpressionType.Equal => "=",
            ExpressionType.NotEqual => "!=",
            ExpressionType.GreaterThan => ">",
            ExpressionType.LessThan => "<",
            ExpressionType.LessThanOrEqual => "<=",
            ExpressionType.GreaterThanOrEqual => ">=",
            _ => throw new InvalidOperationException("Unknown operator " + binaryExpression.OperatorType),
        };

        _builder.Append(op);

        Visit(binaryExpression.Right);
        _builder.Append(")");

        return binaryExpression;
    }

    protected override Expression VisitConstant(FormulaConstantExpression constantExpression)
    {
        if (constantExpression.Value == null)
        {
            _builder.Append("null");
            return constantExpression;
        }

        if (constantExpression.Type == typeof(string))
        {
            _builder.Append('\"');
            var str = (string)constantExpression.Value;
            _builder.Append(str.Replace("\\", "\\\\").Replace("\"", "\\\""));
            _builder.Append('\"');
            return constantExpression;
        }

        if (constantExpression.Value is DateTimeOffset dateTimeOffset)
        {
            _builder.Append('"');
            _builder.Append(dateTimeOffset.ToString("o"));
            _builder.Append('"');
            return constantExpression;
        }

        if (constantExpression.Value is IFormattable formattable)
        {
            _builder.Append(formattable.ToString(null, CultureInfo.InvariantCulture));
            return constantExpression;
        }

        throw new NotSupportedException($"Failed to write constant of type '{constantExpression.Type}' with value '{constantExpression.Value}'");
    }

    public string GetFormula(FormulaExpression expression)
    {
        _builder.Clear();

        Visit(expression);

        return _builder.ToString();
    }

    protected override Expression VisitParameter(FormulaParameterExpression parameterExpression)
    {
        var name = parameterExpression.ParameterExpression.Name ?? throw new InvalidOperationException("Parameter name is null");
        if (!_parameters.TryGetValue(name, out var parameters))
            throw new InvalidOperationException($"Can't find parameter for '{name}'");

        return VisitConstant(new FormulaConstantExpression(parameters, parameterExpression.Type));
    }
}
