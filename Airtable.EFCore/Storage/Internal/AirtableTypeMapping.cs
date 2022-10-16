using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Airtable.EFCore.Storage.Internal;

internal sealed class AirtableTypeMapping : CoreTypeMapping
{
    public AirtableTypeMapping(Type clrType) : base(new CoreTypeMappingParameters(clrType))
    {
    }

    private AirtableTypeMapping(CoreTypeMappingParameters parameters) : base(parameters)
    {
    }

    public override CoreTypeMapping Clone(ValueConverter? converter)
    {
        return new AirtableTypeMapping(Parameters.WithComposedConverter(converter));
    }
}
