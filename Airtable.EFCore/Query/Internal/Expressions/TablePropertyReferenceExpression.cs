using Microsoft.EntityFrameworkCore.Query;

namespace Airtable.EFCore.Query.Internal;

internal sealed class TablePropertyReferenceExpression :
    FormulaExpression,
    IEquatable<TablePropertyReferenceExpression?>,
    IAccessExpression
{
    public TablePropertyReferenceExpression(string name, Type type) : base(type)
    {
        Name = name;
    }

    public string Name { get; }

    public override bool Equals(object? obj) => Equals(obj as TablePropertyReferenceExpression);
    public bool Equals(TablePropertyReferenceExpression? other) => other != null && base.Equals(other) && Name == other.Name;
    public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Name);

    protected override void Print(ExpressionPrinter expressionPrinter)
    {
        expressionPrinter.AppendLine($"Property ({Name})");
    }
}
