using Airtable.EFCore.Query.Internal.MethodTranslators;
using Microsoft.EntityFrameworkCore.Query;

namespace Airtable.EFCore.Query.Internal;

internal sealed class AirtableQueryableMethodTranslatingExpressionVisitorFactory : IQueryableMethodTranslatingExpressionVisitorFactory
{
    private readonly QueryableMethodTranslatingExpressionVisitorDependencies _dependencies;
    private readonly IFormulaExpressionFactory _formulaExpressionFactory;
    private readonly IMethodCallTranslatorProvider _methodCallTranslator;

    public AirtableQueryableMethodTranslatingExpressionVisitorFactory(
        QueryableMethodTranslatingExpressionVisitorDependencies dependencies,
        IFormulaExpressionFactory formulaExpressionFactory,
        IMethodCallTranslatorProvider methodCallTranslator)
    {
        _dependencies = dependencies;
        _formulaExpressionFactory = formulaExpressionFactory;
        _methodCallTranslator = methodCallTranslator;
    }

    public QueryableMethodTranslatingExpressionVisitor Create(QueryCompilationContext queryCompilationContext)
    {
        return new AirtableQueryableMethodTranslatingExpressionVisitor(_dependencies, queryCompilationContext, _formulaExpressionFactory, _methodCallTranslator);
    }
}
