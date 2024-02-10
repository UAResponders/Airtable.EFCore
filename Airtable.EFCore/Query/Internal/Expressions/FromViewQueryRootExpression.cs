using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;

namespace Airtable.EFCore.Query.Internal;

public sealed class FromViewQueryRootExpression : QueryRootExpression, IEquatable<FromViewQueryRootExpression?>, IPrintableExpression
{
    public FromViewQueryRootExpression(IEntityType entityType, string view) : base(entityType.ClrType)
    {
        EntityType = entityType;
        View = view;
    }

    public string View { get; }

    public IEntityType EntityType { get; }

    protected override void Print(ExpressionPrinter expressionPrinter)
    {
        expressionPrinter.Append($".FromView({View})");
    }

    public override Expression DetachQueryProvider()
        => new FromViewQueryRootExpression(EntityType, View);

    public override bool Equals(object? obj) => Equals(obj as FromViewQueryRootExpression);
    public bool Equals(FromViewQueryRootExpression? other) => other is not null && base.Equals(other) && View == other.View;
    public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), View);
}