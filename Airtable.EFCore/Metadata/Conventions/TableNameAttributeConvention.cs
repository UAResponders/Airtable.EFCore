using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using System.ComponentModel.DataAnnotations.Schema;

namespace Airtable.EFCore.Metadata.Conventions;

internal sealed class TableNameAttributeConvention : TypeAttributeConventionBase<TableAttribute>
{
    public TableNameAttributeConvention(ProviderConventionSetBuilderDependencies dependencies) : base(dependencies)
    {
    }

    protected override void ProcessEntityTypeAdded(
        IConventionEntityTypeBuilder entityTypeBuilder,
        TableAttribute attribute,
        IConventionContext<IConventionEntityTypeBuilder> context)
    {
        if (!String.IsNullOrWhiteSpace(attribute.Name))
        {
            entityTypeBuilder.ToTable(attribute.Name, fromDataAnnotation: true);
        }
    }
}