using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Json;
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

    public override CoreTypeMapping WithComposedConverter(ValueConverter? converter, ValueComparer? comparer = null, ValueComparer? keyComparer = null, CoreTypeMapping? elementMapping = null, JsonValueReaderWriter? jsonValueReaderWriter = null)
    {
        return new AirtableTypeMapping(
            new CoreTypeMappingParameters(
                ClrType,
                converter ?? Converter,
                comparer ?? Comparer,
                keyComparer ?? KeyComparer,
                ProviderValueComparer,
                null,
                elementMapping ?? ElementTypeMapping,
                jsonValueReaderWriter ?? JsonValueReaderWriter));
    }

    protected override CoreTypeMapping Clone(CoreTypeMappingParameters parameters)
        => new AirtableTypeMapping(parameters);
}
