using Microsoft.EntityFrameworkCore.Storage;

namespace Airtable.EFCore.Storage.Internal;

internal sealed class AirtableTypeMappingSource : TypeMappingSource
{
    public AirtableTypeMappingSource(
        TypeMappingSourceDependencies dependencies
        )
        : base(dependencies)
    {
    }

    protected override CoreTypeMapping? FindMapping(in TypeMappingInfo mappingInfo)
    {
        var clr = mappingInfo.ClrType;

        if (clr == typeof(string))
        {
            return new AirtableTypeMapping(clr);
        }

        return base.FindMapping(mappingInfo);
    }
}
