using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace Airtable.EFCore.Metadata.Conventions;

internal sealed class ColumnAttributeConvention : PropertyAttributeConventionBase<ColumnAttribute>
{
    public ColumnAttributeConvention(
      ProviderConventionSetBuilderDependencies dependencies)
      : base(dependencies)
    {
    }

    /// <summary>
    ///     Called after a property is added to the entity type with an attribute on the associated CLR property or field.
    /// </summary>
    /// <param name="propertyBuilder">The builder for the property.</param>
    /// <param name="attribute">The attribute.</param>
    /// <param name="clrMember">The member that has the attribute.</param>
    /// <param name="context">Additional information associated with convention execution.</param>
    protected override void ProcessPropertyAdded(
        IConventionPropertyBuilder propertyBuilder,
        ColumnAttribute attribute,
        MemberInfo clrMember,
        IConventionContext context)
    {
        if (!String.IsNullOrWhiteSpace(attribute.Name))
        {
            propertyBuilder.SetColumnName(attribute.Name);
        }

        if (!String.IsNullOrWhiteSpace(attribute.TypeName))
        {
            propertyBuilder.SetColumnName(attribute.TypeName);
        }
    }
}
