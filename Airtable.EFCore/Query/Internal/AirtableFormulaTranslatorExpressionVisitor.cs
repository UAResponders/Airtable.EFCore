using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;

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

    public FormulaExpression? Translate(Expression expression)
    {
        var result = Visit(expression);

        return result as FormulaExpression;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.IsStatic)
        {
            if (node.Method.DeclaringType == typeof(string))
            {
                if (node.Method.Name == nameof(String.Equals))
                {
                    var ignoreCase = false;
                    if (node.Arguments.Count == 3)
                    {
                        if (node.Arguments[2] is ConstantExpression comp)
                        {
                            var comparison = comp.GetConstantValue<StringComparison>();

                            ignoreCase = comparison
                                is StringComparison.OrdinalIgnoreCase
                                or StringComparison.InvariantCultureIgnoreCase
                                or StringComparison.CurrentCultureIgnoreCase;
                        }
                    }

                    var left = Translate(node.Arguments[0]) ?? throw new InvalidOperationException("Failed to translate equals argument");
                    var right = Translate(node.Arguments[1]) ?? throw new InvalidOperationException("Failed to translate equals argument");

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
        }

        throw new InvalidOperationException("Can't translate node:\n" + node.ToString());
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Expression is EntityShaperExpression)
        {
            if (node.Member is PropertyInfo propertyInfo)
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

        if (visitedRight is FormulaConstantExpression constant && constant.Value is null)
        {
            if (node.NodeType == ExpressionType.Equal)
                return _formulaExpressionFactory.MakeNot(visitedLeft);
            if (node.NodeType == ExpressionType.NotEqual)
                return visitedLeft;
        }

        switch (node.NodeType)
        {
            case ExpressionType.And:
            case ExpressionType.AndAlso:
                return _formulaExpressionFactory.MakeAnd(visitedLeft, visitedRight);
            case ExpressionType.Or:
            case ExpressionType.OrElse:
                return _formulaExpressionFactory.MakeOr(visitedLeft, visitedRight);
            default:
                return _formulaExpressionFactory.MakeBinary(
                    node.NodeType,
                    visitedLeft,
                    visitedRight,
                    null);
        }
    }
}
