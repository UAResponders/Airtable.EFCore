using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;

namespace Airtable.EFCore.Query.Internal;

internal sealed class EntityProjectionExpression : Expression, IPrintableExpression, IAccessExpression
{
    public EntityProjectionExpression(IEntityType entityType, Expression accessExpression)
    {
        EntityType = entityType;
        AccessExpression = accessExpression;
        Name = (accessExpression as IAccessExpression)?.Name;
    }

    public sealed override ExpressionType NodeType
        => ExpressionType.Extension;

    public override Type Type
        => EntityType.ClrType;

    public Expression AccessExpression { get; }

    public IEntityType EntityType { get; }

    public string? Name { get; }

    protected override Expression VisitChildren(ExpressionVisitor visitor)
        => Update(visitor.Visit(AccessExpression));

    public Expression Update(Expression accessExpression)
        => accessExpression != AccessExpression
            ? new EntityProjectionExpression(EntityType, accessExpression)
            : this;

    public EntityProjectionExpression UpdateEntityType(IEntityType derivedType)
    {
        if (!derivedType.GetAllBaseTypes().Contains(EntityType))
        {
            throw new InvalidOperationException("InvalidDerivedTypeInEntityProjection " + derivedType.DisplayName() + EntityType.DisplayName());
        }

        return new EntityProjectionExpression(derivedType, AccessExpression);
    }

    void IPrintableExpression.Print(ExpressionPrinter expressionPrinter)
        => expressionPrinter.Visit(AccessExpression);

    public override bool Equals(object? obj)
        => obj != null
            && (ReferenceEquals(this, obj)
                || obj is EntityProjectionExpression entityProjectionExpression
                && Equals(entityProjectionExpression));

    private bool Equals(EntityProjectionExpression entityProjectionExpression)
        => Equals(EntityType, entityProjectionExpression.EntityType)
            && AccessExpression.Equals(entityProjectionExpression.AccessExpression);

    public override int GetHashCode()
        => HashCode.Combine(EntityType, AccessExpression);
}
