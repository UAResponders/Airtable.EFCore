using System.Collections.Immutable;
using System.Linq.Expressions;
using Airtable.EFCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;

namespace Airtable.EFCore.Query.Internal;

internal sealed class SelectExpression : Expression
{
    private readonly List<string> _selectProperties = new();
    private FormulaExpression? _filterByFormula;

    public IReadOnlyCollection<string> SelectProperties => _selectProperties;
    public string Table { get; }
    public int? Limit { get; set; }
    public FormulaExpression? FilterByFormula => _filterByFormula;
    public override Type Type => typeof(object);

    public IEntityType EntityType { get; }

    public SelectExpression(IEntityType entityType)
    {
        Table = entityType.GetTableName() ?? entityType.Name;
        //entityType.FindPrimaryKey()?.Properties?.FirstOrDefault()?.Name;

        foreach (var property in entityType.GetDerivedProperties())
        {
            _selectProperties.Add(property.Name);
        }

        EntityType = entityType;
    }

    public void AddPredicate(FormulaExpression formula)
    {
        if (_filterByFormula == null)
        {
            _filterByFormula = formula;
            return;
        }

        if(_filterByFormula is FormulaCallExpression callExpression && callExpression.FormulaName == "AND")
        {
            var args = 
                formula is FormulaCallExpression callExpressionInner && callExpressionInner.FormulaName == "AND"
                ? callExpression.Arguments.AddRange(callExpressionInner.Arguments)
                : callExpression.Arguments.Add(formula);

            _filterByFormula = new FormulaCallExpression(
                "AND",
                args,
                typeof(bool));

            return;
        }

        _filterByFormula = new FormulaCallExpression(
            "AND", 
            ImmutableList.Create(_filterByFormula, formula), 
            typeof(bool));
    }

    public ProjectionExpression? GetProjection(ProjectionMember? member)
    {
        if (member == null || member.Equals(new ProjectionMember()))
            return new ProjectionExpression(
                new EntityProjectionExpression(
                    EntityType,
                    new RootReferenceExpression(EntityType, "root")),
                "root");
        return null;
    }
}
