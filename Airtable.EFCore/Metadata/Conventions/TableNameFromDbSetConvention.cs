using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;

namespace Airtable.EFCore.Metadata.Conventions;

internal sealed class TableNameFromDbSetConvention :
    IEntityTypeAddedConvention,
    IEntityTypeBaseTypeChangedConvention
{
    private readonly IDictionary<Type, string> _sets;

    /// <summary>
    ///     Creates a new instance of <see cref="TableNameFromDbSetConvention" />.
    /// </summary>
    /// <param name="dependencies">Parameter object containing dependencies for this convention.</param>
    /// <param name="relationalDependencies"> Parameter object containing relational dependencies for this convention.</param>
    public TableNameFromDbSetConvention(ProviderConventionSetBuilderDependencies dependencies)
    {
        _sets = new Dictionary<Type, string>();
        List<Type>? ambiguousTypes = null;
        foreach (var set in dependencies.SetFinder.FindSets(dependencies.ContextType))
        {
            if (!_sets.ContainsKey(set.Type))
            {
                _sets.Add(set.Type, set.Name);
            }
            else
            {
                ambiguousTypes ??= new List<Type>();

                ambiguousTypes.Add(set.Type);
            }
        }

        if (ambiguousTypes != null)
        {
            foreach (var type in ambiguousTypes)
            {
                _sets.Remove(type);
            }
        }
    }


    /// <inheritdoc />
    public void ProcessEntityTypeBaseTypeChanged(
        IConventionEntityTypeBuilder entityTypeBuilder,
        IConventionEntityType? newBaseType,
        IConventionEntityType? oldBaseType,
        IConventionContext<IConventionEntityType> context)
    {
        var entityType = entityTypeBuilder.Metadata;

        if (oldBaseType == null
            && newBaseType != null)
        {
            entityTypeBuilder.HasNoAnnotation(AirtableAnnotationNames.TableName);
        }
        else if (oldBaseType != null
                 && newBaseType == null
                 && !entityType.HasSharedClrType
                 && _sets.TryGetValue(entityType.ClrType, out var setName))
        {
            entityTypeBuilder.ToTable(setName);
        }
    }

    /// <inheritdoc />
    public void ProcessEntityTypeAdded(
        IConventionEntityTypeBuilder entityTypeBuilder,
        IConventionContext<IConventionEntityTypeBuilder> context)
    {
        var entityType = entityTypeBuilder.Metadata;
        if (!entityType.HasSharedClrType
            && (entityType.BaseType == null)
            && _sets.TryGetValue(entityType.ClrType, out var setName))
        {
            entityTypeBuilder.ToTable(setName);
        }
    }
}
