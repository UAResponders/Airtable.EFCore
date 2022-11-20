using Microsoft.EntityFrameworkCore.Query;

namespace Airtable.EFCore.Query.Internal;

internal sealed class RecordIdPropertyReferenceExpression :
    FormulaExpression,
    IEquatable<RecordIdPropertyReferenceExpression?>,
    IAccessExpression
{
    public static RecordIdPropertyReferenceExpression Instance { get; } = new RecordIdPropertyReferenceExpression();

    public RecordIdPropertyReferenceExpression() : base(typeof(string))
    {
    }

    public string Name => "RecordId";

    public override bool Equals(object? obj) => Equals(obj as RecordIdPropertyReferenceExpression);
    public bool Equals(RecordIdPropertyReferenceExpression? other) => other != null;
    public override int GetHashCode() => HashCode.Combine(GetType());

    protected override void Print(ExpressionPrinter expressionPrinter)
    {
        expressionPrinter.AppendLine($"RecordId");
    }
}
