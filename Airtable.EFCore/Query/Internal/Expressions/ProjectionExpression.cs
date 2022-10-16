using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace Airtable.EFCore.Query.Internal;

internal sealed class ProjectionExpression : Expression, IPrintableExpression
{
    public ProjectionExpression(Expression expression, string alias)
    {
        Expression = expression;
        Alias = alias;
    }

    public string Alias { get; }

    public Expression Expression { get; }

    public string Name
        => (Expression as IAccessExpression)?.Name;

    public override Type Type
        => Expression.Type;

    public sealed override ExpressionType NodeType
        => ExpressionType.Extension;

    protected override Expression VisitChildren(ExpressionVisitor visitor)
        => Update(visitor.Visit(Expression));

    public ProjectionExpression Update(Expression expression)
        => expression != Expression
            ? new ProjectionExpression(expression, Alias)
            : this;

    void IPrintableExpression.Print(ExpressionPrinter expressionPrinter)
    {
        expressionPrinter.Visit(Expression);
        if (Alias != string.Empty && Alias != Name)
        {
            expressionPrinter.Append(" AS " + Alias);
        }
    }

    public override bool Equals(object? obj)
        => obj != null
            && (ReferenceEquals(this, obj)
                || obj is ProjectionExpression projectionExpression
                && Equals(projectionExpression));

    private bool Equals(ProjectionExpression projectionExpression)
        => Alias == projectionExpression.Alias
            && Expression.Equals(projectionExpression.Expression);

    public override int GetHashCode()
        => HashCode.Combine(Alias, Expression);
}
