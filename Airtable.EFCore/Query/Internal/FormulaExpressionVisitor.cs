using System.Linq.Expressions;

namespace Airtable.EFCore.Query.Internal;

internal abstract class FormulaExpressionVisitor : ExpressionVisitor
{
    protected sealed override Expression VisitExtension(Expression node)
    {
        if (node is FormulaExpression formulaExpression)
        {
            switch (formulaExpression)
            {
                case FormulaConstantExpression constantExpression:
                    return VisitConstant(constantExpression);
                case FormulaBinaryExpression binaryExpression:
                    return VisitBinary(binaryExpression);
                case FormulaCallExpression callExpression:
                    return VisitFunction(callExpression);
                case TablePropertyReferenceExpression tableProperty:
                    return VisitTableProperty(tableProperty);
                default:
                    break;
            }
        }

        return base.VisitExtension(node);
    }

    protected abstract Expression VisitTableProperty(TablePropertyReferenceExpression tableProperty);
    protected abstract Expression VisitFunction(FormulaCallExpression callExpression);
    protected abstract Expression VisitBinary(FormulaBinaryExpression binaryExpression);
    protected abstract Expression VisitConstant(FormulaConstantExpression constantExpression);
}
