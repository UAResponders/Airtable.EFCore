using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Airtable.EFCore.Query.Internal.MethodTranslators;

internal interface IMethodCallTranslatorProvider
{
    FormulaExpression? Translate(
        IModel model,
        FormulaExpression? instance,
        MethodInfo method,
        IReadOnlyList<FormulaExpression> arguments);
}
