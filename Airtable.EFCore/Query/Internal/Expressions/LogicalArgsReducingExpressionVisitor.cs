using System.Collections.Immutable;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Airtable.EFCore.Query.Internal;

internal sealed class LogicalArgsReducingExpressionVisitor : FormulaExpressionVisitor
{
    private static readonly HashSet<string> _reducableFunctions = new() { "AND", "OR" };

    protected override Expression VisitBinary(FormulaBinaryExpression binaryExpression)
    {
        var left = (FormulaExpression)Visit(binaryExpression.Left);
        var right = (FormulaExpression)Visit(binaryExpression.Right);

        return binaryExpression.Update(left, right);
    }

    protected override Expression VisitConstant(FormulaConstantExpression constantExpression) => constantExpression;

    protected override Expression VisitFunction(FormulaCallExpression callExpression)
    {
        if (_reducableFunctions.Contains(callExpression.FormulaName))
        {
            if (callExpression.Arguments.Count == 1)
            {
                return callExpression.Arguments[0];
            }

            var args = new List<FormulaExpression>();

            foreach (var item in callExpression.Arguments)
            {
                var visited = (FormulaExpression)Visit(item);

                if (visited is FormulaCallExpression visitedCall && visitedCall.FormulaName == callExpression.FormulaName)
                {
                    args.AddRange(visitedCall.Arguments);
                }
                else
                {
                    args.Add(visited);
                }
            }

            if (!args.SequenceEqual(callExpression.Arguments))
            {
                return new FormulaCallExpression(callExpression.FormulaName, args.ToImmutableList(), callExpression.Type);
            }
        }

        return callExpression;
    }

    protected override Expression VisitParameter(FormulaParameterExpression parameterExpression) => parameterExpression;
    protected override Expression VisitRecordId(RecordIdPropertyReferenceExpression recordIdProperty) => recordIdProperty;
    protected override Expression VisitTableProperty(TablePropertyReferenceExpression tableProperty) => tableProperty;
}



internal sealed class TruthyValuesComparisonTransformingExpressionVisitor : FormulaExpressionVisitor
{
    protected override Expression VisitBinary(FormulaBinaryExpression binaryExpression)
    {
        var left = (FormulaExpression)Visit(binaryExpression.Left);
        var right = (FormulaExpression)Visit(binaryExpression.Right);

        if (right is FormulaConstantExpression constant && constant.Value is null)
        {
            if (binaryExpression.OperatorType == ExpressionType.Equal)
                return new FormulaCallExpression("NOT", ImmutableList.Create(left), typeof(bool));
            if (binaryExpression.OperatorType == ExpressionType.NotEqual)
                return left;
        }

        return binaryExpression.Update(left, right);
    }

    protected override Expression VisitConstant(FormulaConstantExpression constantExpression) => constantExpression;
    protected override Expression VisitFunction(FormulaCallExpression callExpression)
    {
        var visitedArgs = callExpression.Arguments.Select(Visit).OfType<FormulaExpression>().ToImmutableList();

        if (visitedArgs.SequenceEqual(callExpression.Arguments))
            return callExpression;

        return new FormulaCallExpression(callExpression.FormulaName, visitedArgs, callExpression.Type);
    }

    protected override Expression VisitParameter(FormulaParameterExpression parameterExpression) => parameterExpression;
    protected override Expression VisitRecordId(RecordIdPropertyReferenceExpression recordIdProperty) => recordIdProperty;
    protected override Expression VisitTableProperty(TablePropertyReferenceExpression tableProperty) => tableProperty;
}