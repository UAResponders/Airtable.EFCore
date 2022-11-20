using System.Collections.Immutable;
using System.Linq.Expressions;
using Airtable.EFCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace Airtable.EFCore.Query.Internal;

internal class FormulaExpressionFactory : IFormulaExpressionFactory
{
    public FormulaExpressionFactory()
    {
    }

    public FormulaBinaryExpression MakeBinary(ExpressionType operatorType, FormulaExpression left, FormulaExpression right, CoreTypeMapping? typeMapping)
    {
        var returnType = left.Type;
        switch (operatorType)
        {
            case ExpressionType.Equal:
            case ExpressionType.GreaterThan:
            case ExpressionType.GreaterThanOrEqual:
            case ExpressionType.LessThan:
            case ExpressionType.LessThanOrEqual:
            case ExpressionType.NotEqual:
                returnType = typeof(bool);
                break;
        }

        return new FormulaBinaryExpression(operatorType, left, right, returnType);
    }

    public SelectExpression Select(IEntityType entityType)
    {
        return new SelectExpression(entityType);
    }

    public FormulaExpression MakeTablePropertyReference(IProperty property)
    {
        if (property.IsPrimaryKey()) return RecordIdPropertyReferenceExpression.Instance;

        return new TablePropertyReferenceExpression(property.GetColumnName() ?? property.Name, property.ClrType);
    }

    public FormulaConstantExpression MakeConstant(object? value)
    {
        return new FormulaConstantExpression(value, value?.GetType() ?? typeof(object));
    }

    private FormulaExpression LogicalFormulaExpression(string formula, params FormulaExpression[] expressions)
    {
        return
            (FormulaExpression)
                new LogicalArgsReducingExpressionVisitor()
                    .Visit(
                    new FormulaCallExpression(
                        formula,
                        expressions.ToImmutableList(),
                        typeof(bool)));
    }

    public FormulaExpression MakeAnd(params FormulaExpression[] expressions)
        => LogicalFormulaExpression("AND", expressions);

    public FormulaExpression MakeOr(params FormulaExpression[] expressions)
        => LogicalFormulaExpression("OR", expressions);
    public FormulaCallExpression MakeNot(FormulaExpression value) => new FormulaCallExpression("NOT", ImmutableList.Create(value), typeof(bool));
}
