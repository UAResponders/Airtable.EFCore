using Microsoft.EntityFrameworkCore.Query;

namespace Airtable.EFCore.Query.Internal;

internal sealed class FormulaConstantExpression : FormulaExpression, IEquatable<FormulaConstantExpression?>
{
    public FormulaConstantExpression(object? value, Type type) : base(type)
    {
        Value = value;
    }

    public object? Value { get; }

    public override bool Equals(object? obj) => Equals(obj as FormulaConstantExpression);
    public bool Equals(FormulaConstantExpression? other) => other != null && base.Equals(other) && EqualityComparer<object>.Default.Equals(Value, other.Value);
    public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Value);

    protected override void Print(ExpressionPrinter expressionPrinter)
    {
        expressionPrinter.AppendLine($"Constant ('{Value}' : {Type.Name})");
    }
}
