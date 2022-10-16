using Microsoft.EntityFrameworkCore.Query;

namespace Airtable.EFCore.Query.Internal;

internal sealed class AirtableShapedQueryCompilingExpressionVisitorFactory : IShapedQueryCompilingExpressionVisitorFactory
{
    private readonly ShapedQueryCompilingExpressionVisitorDependencies _dependencies;

    public AirtableShapedQueryCompilingExpressionVisitorFactory(ShapedQueryCompilingExpressionVisitorDependencies dependencies)
    {
        _dependencies = dependencies;
    }

    public ShapedQueryCompilingExpressionVisitor Create(QueryCompilationContext queryCompilationContext) 
        => new AirtableShapedQueryCompilingExpressionVisitor(_dependencies, queryCompilationContext);
}
