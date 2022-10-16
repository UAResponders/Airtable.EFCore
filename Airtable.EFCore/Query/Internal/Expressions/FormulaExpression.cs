using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace Airtable.EFCore.Query.Internal;

internal abstract class FormulaExpression : Expression, IPrintableExpression, IEquatable<FormulaExpression?>
{
    private readonly Type _type;

    protected FormulaExpression(Type type)
    {
        _type = type;
    }

    public override ExpressionType NodeType => ExpressionType.Extension;
    public override Type Type => _type;

    public override bool Equals(object? obj)
    {
        return Equals(obj as FormulaExpression);
    }

    public bool Equals(FormulaExpression? other)
    {
        return other != null &&
               EqualityComparer<Type>.Default.Equals(Type, other.Type);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Type);
    }

    protected abstract void Print(ExpressionPrinter expressionPrinter);
    void IPrintableExpression.Print(ExpressionPrinter expressionPrinter)
        => Print(expressionPrinter);
}
