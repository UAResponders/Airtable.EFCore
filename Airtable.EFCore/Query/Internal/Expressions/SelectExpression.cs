using System.Collections.Immutable;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;

namespace Airtable.EFCore.Query.Internal;

internal sealed class SelectExpression : Expression
{
    private const string _rootAlias = "root";

    private IDictionary<ProjectionMember, Expression> _projectionMapping = new Dictionary<ProjectionMember, Expression>();
    private readonly List<ProjectionExpression> _projection = new();
    private readonly List<(TablePropertyReferenceExpression ordering, bool descending)> _orderings = new();

    public SelectExpression(IEntityType entityType)
    {
        Table = entityType.GetTableName() ?? entityType.Name;
        EntityType = entityType;
        FromExpression = new RootReferenceExpression(entityType, _rootAlias);
        _projectionMapping[new ProjectionMember()] = new EntityProjectionExpression(entityType, FromExpression);
    }

    public override ExpressionType NodeType => ExpressionType.Extension;

    public string? View { get; set; }
    public string Table { get; }
    public Expression? Limit { get; set; }
    public FormulaExpression? FilterByFormula { get; private set; }
    public override Type Type => typeof(object);
    public IEntityType EntityType { get; }
    public RootReferenceExpression FromExpression { get; }
    public IReadOnlyList<ProjectionExpression> Projection => _projection;
    public IReadOnlyList<(TablePropertyReferenceExpression ordering, bool descending)> Sort => _orderings;

    public IEnumerable<string> GetFields()
    {
        if (_projection.Count == 1 && _projection[0].Expression is EntityProjectionExpression e && e.AccessExpression is RootReferenceExpression)
        {
            foreach (var property in EntityType.GetProperties())
            {
                if (property.IsPrimaryKey()) continue;

                yield return property.GetColumnName() ?? property.Name;
            }
        }
        else
        {
            foreach (var item in _projection)
            {
                if (item.Expression is RecordIdPropertyReferenceExpression) continue;

                yield return item.Name;
            }
        }
    }

    public void AddPredicate(FormulaExpression formula)
    {
        if (FilterByFormula == null)
        {
            FilterByFormula = formula;
            return;
        }

        if (FilterByFormula is FormulaCallExpression callExpression && callExpression.FormulaName == "AND")
        {
            var args =
                formula is FormulaCallExpression callExpressionInner && callExpressionInner.FormulaName == "AND"
                ? callExpression.Arguments.AddRange(callExpressionInner.Arguments)
                : callExpression.Arguments.Add(formula);

            FilterByFormula = new FormulaCallExpression(
                "AND",
                args,
                typeof(bool));

            return;
        }

        FilterByFormula = new FormulaCallExpression(
            "AND",
            ImmutableList.Create(FilterByFormula, formula),
            typeof(bool));
    }

    public Expression GetMappedProjection(ProjectionMember projectionMember)
      => _projectionMapping[projectionMember];

    public void ApplyProjection()
    {
        if (Projection.Any())
        {
            return;
        }

        var result = new Dictionary<ProjectionMember, Expression>();
        foreach (var (projectionMember, expression) in _projectionMapping)
        {
            result[projectionMember] = Constant(
                AddToProjection(
                    expression,
                    projectionMember.Last?.Name));
        }

        _projectionMapping = result;
    }

    public void ReplaceProjectionMapping(IDictionary<ProjectionMember, Expression> projectionMapping)
    {
        _projectionMapping.Clear();
        foreach (var (projectionMember, expression) in projectionMapping)
        {
            _projectionMapping[projectionMember] = expression;
        }
    }

    public int AddToProjection(FormulaExpression formulaExpression)
        => AddToProjection(formulaExpression, null);

    public int AddToProjection(EntityProjectionExpression entityProjection)
        => AddToProjection(entityProjection, null);

    //public int AddToProjection(ObjectArrayProjectionExpression objectArrayProjection)
    //    => AddToProjection(objectArrayProjection, null);

    private int AddToProjection(Expression expression, string? alias)
    {
        var existingIndex = _projection.FindIndex(pe => pe.Expression.Equals(expression));
        if (existingIndex != -1)
        {
            return existingIndex;
        }

        var baseAlias = alias
            ?? (expression as IAccessExpression)?.Name
            ?? "c";

        var currentAlias = baseAlias;
        var counter = 0;
        while (_projection.Any(pe => String.Equals(pe.Alias, currentAlias, StringComparison.OrdinalIgnoreCase)))
        {
            currentAlias = $"{baseAlias}{counter++}";
        }

        _projection.Add(new ProjectionExpression(expression, currentAlias));

        return _projection.Count - 1;
    }

    internal void ApplyOrdering(TablePropertyReferenceExpression selector, bool descending) => _orderings.Add((selector, descending));
}
