using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Airtable.EFCore.Query.Internal.MethodTranslators;

internal class AirtableRegexMethodTranslator : IMethodCallTranslator
{
    private static readonly MethodInfo _isMatch =
        typeof(Regex).GetRuntimeMethod(nameof(Regex.IsMatch), new[] { typeof(string), typeof(string) })!;

    private static readonly MethodInfo _isMatchWithRegexOptions =
        typeof(Regex).GetRuntimeMethod(nameof(Regex.IsMatch), new[] { typeof(string), typeof(string), typeof(RegexOptions) })!;

    private readonly IFormulaExpressionFactory _formulaExpressionFactory;

    public AirtableRegexMethodTranslator(IFormulaExpressionFactory formulaExpressionFactory)
    {
        _formulaExpressionFactory = formulaExpressionFactory;
    }

    public FormulaExpression? Translate(
        IModel model,
        FormulaExpression? instance,
        MethodInfo method,
        IReadOnlyList<FormulaExpression> arguments)
    {
        if (method != _isMatch && method != _isMatchWithRegexOptions)
            return null;

        var (input, pattern) = (arguments[0], arguments[1]);

        if (method == _isMatchWithRegexOptions && arguments[2] is FormulaConstantExpression { Value: RegexOptions regexOptions })
        {
            if (regexOptions != 0) throw new InvalidOperationException($"Unsupported regex options: {regexOptions}");
        }

        return _formulaExpressionFactory.MakeCall("REGEX_MATCH", input, pattern);
    }
}
