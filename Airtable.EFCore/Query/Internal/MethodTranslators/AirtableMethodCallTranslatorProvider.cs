using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Airtable.EFCore.Query.Internal.MethodTranslators;

internal sealed class AirtableMethodCallTranslatorProvider : IMethodCallTranslatorProvider
{
    private readonly List<IMethodCallTranslator> _translators = new();

    public AirtableMethodCallTranslatorProvider(IFormulaExpressionFactory formulaExpressionFactory)
    {
        _translators.Add(new AirtableRegexMethodTranslator(formulaExpressionFactory));
        _translators.Add(new AirtableStringMethodTranslator(formulaExpressionFactory));
    }

    public FormulaExpression? Translate(
        IModel model,
        FormulaExpression? instance,
        MethodInfo method,
        IReadOnlyList<FormulaExpression> arguments)
    {
        return _translators.Select(i => i.Translate(model, instance, method, arguments)).OfType<FormulaExpression>().FirstOrDefault();
    }
}
