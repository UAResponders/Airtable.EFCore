using AirtableApiClient;
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

        if (clr == null) return base.FindMapping(mappingInfo);

        if (clr == typeof(string))
        {
            return new AirtableTypeMapping(clr);
        }

        if (clr == typeof(DateTimeOffset))
        {
            return new AirtableTypeMapping(clr);
        }

        if (clr == typeof(AirtableAttachment))
        {
            return new AirtableTypeMapping(clr);
        }

        if (clr.IsGenericType && clr.GenericTypeArguments[0] == typeof(AirtableAttachment))
        {
            return new AirtableTypeMapping(clr);
        }

        if(clr == typeof(string[]))
        {
            return new AirtableTypeMapping(clr);
        }

        return base.FindMapping(mappingInfo);
    }
}
