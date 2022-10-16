using Microsoft.EntityFrameworkCore.Query;

namespace Airtable.EFCore.Query.Internal;

internal sealed class AirtableQueryableMethodTranslatingExpressionVisitorFactory : IQueryableMethodTranslatingExpressionVisitorFactory
{
    private readonly QueryableMethodTranslatingExpressionVisitorDependencies _dependencies;
    private readonly IFormulaExpressionFactory _formulaExpressionFactory;

    public AirtableQueryableMethodTranslatingExpressionVisitorFactory(QueryableMethodTranslatingExpressionVisitorDependencies dependencies, IFormulaExpressionFactory formulaExpressionFactory)
    {
        _dependencies = dependencies;
        _formulaExpressionFactory = formulaExpressionFactory;
    }

    public QueryableMethodTranslatingExpressionVisitor Create(QueryCompilationContext queryCompilationContext)
    {
        return new AirtableQueryableMethodTranslatingExpressionVisitor(_dependencies, queryCompilationContext, _formulaExpressionFactory);
    }
}
