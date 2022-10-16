using System.Collections.Immutable;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace Airtable.EFCore.Query.Internal;

internal sealed class FormulaCallExpression : FormulaExpression, IEquatable<FormulaCallExpression?>
{
    public FormulaCallExpression(
        string formulaName, 
        ImmutableList<FormulaExpression> arguments, 
        Type type) : base(type)
    {
        FormulaName = formulaName;
        Arguments = arguments;
    }

    public string FormulaName { get; }
    public ImmutableList<FormulaExpression> Arguments { get; }

    public override bool Equals(object? obj) => Equals(obj as FormulaCallExpression);
    public bool Equals(FormulaCallExpression? other) => 
        other != null 
        && base.Equals(other) 
        && FormulaName == other.FormulaName 
        && Enumerable.SequenceEqual(Arguments, other.Arguments);

    public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), FormulaName, Arguments);

    protected override void Print(ExpressionPrinter expressionPrinter)
    {
    }

    protected override Expression VisitChildren(ExpressionVisitor visitor)
    {
        var visitedArgs = Arguments.Select(visitor.Visit).Cast<FormulaExpression>().ToImmutableList();

        if(!visitedArgs.SequenceEqual(Arguments))
        {
            return new FormulaCallExpression(FormulaName, visitedArgs, Type);
        }

        return this;
    }
}
