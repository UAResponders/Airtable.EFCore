using System.Collections.Immutable;
using System.Linq.Expressions;

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

    protected override Expression VisitTableProperty(TablePropertyReferenceExpression tableProperty) => tableProperty;
}
