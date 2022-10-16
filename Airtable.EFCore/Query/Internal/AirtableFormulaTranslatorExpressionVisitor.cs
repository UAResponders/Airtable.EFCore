using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;
using System.Reflection;

namespace Airtable.EFCore.Query.Internal;

internal sealed class AirtableFormulaTranslatorExpressionVisitor : ExpressionVisitor
{
    private readonly IFormulaExpressionFactory _formulaExpressionFactory;
    private readonly IEntityType _entityType;

    public AirtableFormulaTranslatorExpressionVisitor(
        IFormulaExpressionFactory formulaExpressionFactory,
        IEntityType entityType)
    {
        _formulaExpressionFactory = formulaExpressionFactory;
        _entityType = entityType;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if(node.Expression is EntityShaperExpression)
        {
            if(node.Member is PropertyInfo propertyInfo)
            {
                var property = _entityType.FindProperty(propertyInfo) ?? throw new InvalidOperationException($"Failed to map property '{propertyInfo.Name}' on entity {_entityType.Name}");

                return _formulaExpressionFactory.MakeTablePropertyReference(property);
            }
        }

        return base.VisitMember(node);
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        var value = node.Value;

        if (value == null)
        {
            return _formulaExpressionFactory.MakeConstant(null);
        }
        else
        {
            return _formulaExpressionFactory.MakeConstant(value);
        }
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        var visitedLeft = Visit(node.Left) as FormulaExpression ?? throw new InvalidOperationException("Failed to translate\n" + node.Left);
        var visitedRight = Visit(node.Right) as FormulaExpression ?? throw new InvalidOperationException("Failed to translate\n" + node.Right);

        switch (node.NodeType)
        {
            case ExpressionType.And:
            case ExpressionType.AndAlso:
                return _formulaExpressionFactory.MakeAnd(visitedLeft, visitedRight);
            default:
                return _formulaExpressionFactory.MakeBinary(
                    node.NodeType,
                    visitedLeft,
                    visitedRight,
                    null);
        }
    }
}
