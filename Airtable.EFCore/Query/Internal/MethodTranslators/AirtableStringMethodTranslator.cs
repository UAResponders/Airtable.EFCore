using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Airtable.EFCore.Query.Internal.MethodTranslators;

internal class AirtableStringMethodTranslator : IMethodCallTranslator
{
    private static readonly MethodInfo _equals =
        typeof(string).GetRuntimeMethod(nameof(String.Equals), new[] { typeof(string), typeof(string) })!;

    private static readonly MethodInfo _equalsComparison =
        typeof(string).GetRuntimeMethod(nameof(String.Equals), new[] { typeof(string), typeof(string), typeof(StringComparison) })!;

    private static readonly MethodInfo _contains =
        typeof(string).GetRuntimeMethod(nameof(String.Contains), new[] { typeof(string) })!;

    private static readonly MethodInfo _containsComparison =
        typeof(string).GetRuntimeMethod(nameof(String.Contains), new[] { typeof(string), typeof(StringComparison) })!;

    private readonly IFormulaExpressionFactory _formulaExpressionFactory;

    public AirtableStringMethodTranslator(IFormulaExpressionFactory formulaExpressionFactory)
    {
        _formulaExpressionFactory = formulaExpressionFactory;
    }

    public FormulaExpression? Translate(
        IModel model,
        FormulaExpression? instance,
        MethodInfo method,
        IReadOnlyList<FormulaExpression> arguments)
    {
        if (method == _equals || method == _equalsComparison)
            return TranslateEquals(method, arguments);

        if (method == _contains || method == _containsComparison)
            return TranslateContains(method, instance, arguments);

        return null;
    }

    private FormulaExpression TranslateContains(MethodInfo method, FormulaExpression? instance, IReadOnlyList<FormulaExpression> arguments)
    {
        var ignoreCase = false;
        if (method == _containsComparison)
        {
            if (arguments[1] is FormulaConstantExpression comp)
            {
                var comparison = (StringComparison)comp.Value;

                ignoreCase = comparison
                            is StringComparison.OrdinalIgnoreCase
                            or StringComparison.InvariantCultureIgnoreCase
                            or StringComparison.CurrentCultureIgnoreCase;
            }
        }

        var toFind = arguments[0];
        var whereToFind = instance;

        if (ignoreCase)
        {
            toFind = _formulaExpressionFactory.MakeCall("UPPER", toFind);
            whereToFind = _formulaExpressionFactory.MakeCall("UPPER", whereToFind);
        }

        return _formulaExpressionFactory.MakeCall(
            "FIND",
            toFind,
            whereToFind);
    }

    private FormulaExpression TranslateEquals(MethodInfo method, IReadOnlyList<FormulaExpression> arguments)
    {
        var ignoreCase = false;
        if (method == _equalsComparison)
        {
            if (arguments[2] is FormulaConstantExpression comp)
            {
                var comparison = (StringComparison)comp.Value;

                ignoreCase = comparison
                            is StringComparison.OrdinalIgnoreCase
                            or StringComparison.InvariantCultureIgnoreCase
                            or StringComparison.CurrentCultureIgnoreCase;
            }
        }

        var left = arguments[0];
        var right = arguments[1];

        if (ignoreCase)
        {
            left = _formulaExpressionFactory.MakeCall("UPPER", left);
            right = _formulaExpressionFactory.MakeCall("UPPER", right);
        }

        return _formulaExpressionFactory.MakeBinary(
            ExpressionType.Equal,
            left,
            right,
            null);
    }
}
