using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace Airtable.EFCore.Query.Internal;

internal sealed class FormulaParameterExpression : FormulaExpression, IEquatable<FormulaParameterExpression?>
{
    public FormulaParameterExpression(ParameterExpression parameterExpression) : base(parameterExpression.Type)
    {
        ParameterExpression = parameterExpression;
    }

    public ParameterExpression ParameterExpression { get; }

    public override bool Equals(object? obj) => Equals(obj as FormulaParameterExpression);
    public bool Equals(FormulaParameterExpression? other) => other is not null && base.Equals(other) && EqualityComparer<ParameterExpression>.Default.Equals(ParameterExpression, other.ParameterExpression);
    public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), ParameterExpression);

    protected override void Print(ExpressionPrinter expressionPrinter)
    {
        expressionPrinter.Append(".FormulaParameter(");
        expressionPrinter.Visit(ParameterExpression);
        expressionPrinter.Append(")");
    }
}