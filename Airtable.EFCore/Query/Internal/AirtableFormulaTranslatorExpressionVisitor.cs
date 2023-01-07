using System.Linq.Expressions;
using System.Reflection;
using Airtable.EFCore.Query.Internal.MethodTranslators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;

namespace Airtable.EFCore.Query.Internal;

internal sealed class AirtableFormulaTranslatorExpressionVisitor : ExpressionVisitor
{
    private static readonly MethodInfo _efPropertyRef = typeof(EF).GetRuntimeMethod(nameof(EF.Property), new[] { typeof(object), typeof(string) })!;

    private readonly IFormulaExpressionFactory _formulaExpressionFactory;
    private readonly IEntityType _entityType;
    private readonly IMethodCallTranslatorProvider _methodCallTranslator;

    public AirtableFormulaTranslatorExpressionVisitor(
        IFormulaExpressionFactory formulaExpressionFactory,
        IEntityType entityType,
        IMethodCallTranslatorProvider methodCallTranslator)
    {
        _formulaExpressionFactory = formulaExpressionFactory;
        _entityType = entityType;
        _methodCallTranslator = methodCallTranslator;
    }

    public FormulaExpression? Translate(Expression expression)
    {
        var result = Visit(expression);

        return result as FormulaExpression;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.IsGenericMethod && node.Method.GetGenericMethodDefinition() == _efPropertyRef)
        {
            var shaper = node.Arguments[0] as EntityShaperExpression;
            var property = node.Arguments[1].GetConstantValue<string>();

            if (_entityType.GetProperty(property).IsPrimaryKey())
            {
                return RecordIdPropertyReferenceExpression.Instance;
            }

            return new TablePropertyReferenceExpression(property, node.Method.GetGenericArguments()[0]);
        }

        var obj = Visit(node.Object) as FormulaExpression;
        var args = node.Arguments.Select(Visit).Cast<FormulaExpression>().ToList();

        var translated = _methodCallTranslator.Translate(_entityType.Model, obj, node.Method, args);

        if (translated != null) return translated;

        throw new InvalidOperationException("Can't translate node:\n" + node.ToString());
    }

    protected override Expression VisitExtension(Expression node)
    {
        if (node is EntityShaperExpression)
        {
            return new RootReferenceExpression(_entityType, "shaper");
        }

        return base.VisitExtension(node);
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

    protected override Expression VisitParameter(ParameterExpression node)
        => new FormulaParameterExpression(node);

    private static (string, bool) OperatorToDateTimeFunction(ExpressionType expressionType)
    {
        return (expressionType) switch
        {
            ExpressionType.GreaterThan => ("IS_AFTER", false),
            ExpressionType.GreaterThanOrEqual => ("IS_BEFORE", true),
            ExpressionType.LessThan => ("IS_BEFORE", false),
            ExpressionType.LessThanOrEqual => ("IS_AFTER", true),
            ExpressionType.Equal => ("IS_SAME", false),
            ExpressionType.NotEqual => ("IS_SAME", true),
            var type => throw new InvalidOperationException($"Failed to tranlsate datetime operator '{type}'")
        };
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

        if (visitedLeft.Type == typeof(DateTimeOffset) && visitedRight.Type == typeof(DateTimeOffset))
        {
            var (func, invert) = OperatorToDateTimeFunction(node.NodeType);

            var result = _formulaExpressionFactory.MakeCall(func, visitedLeft, visitedRight);
            if (invert) result = _formulaExpressionFactory.MakeNot(result);

            return result;
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
