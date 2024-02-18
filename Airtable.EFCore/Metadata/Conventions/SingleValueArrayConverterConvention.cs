using System.Reflection;
using Airtable.EFCore.Storage;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;

namespace Airtable.EFCore.Metadata.Conventions;

internal sealed class SingleValueArrayConverterConvention(ProviderConventionSetBuilderDependencies dependencies)
    : PropertyAttributeConventionBase<SingleValueArrayAttribute>(dependencies)
{
    protected override void ProcessPropertyAdded(
        IConventionPropertyBuilder propertyBuilder,
        SingleValueArrayAttribute attribute,
        MemberInfo clrMember,
        IConventionContext context)
    {
        propertyBuilder.HasConverter(typeof(SingleItemArrayConverter));
    }
}

