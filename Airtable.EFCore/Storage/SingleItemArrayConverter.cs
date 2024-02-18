using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Airtable.EFCore.Storage;

internal sealed class SingleItemArrayConverter : ValueConverter<string?, string?[]?>
{
    public SingleItemArrayConverter()
        : base(
            v => new[] { v },
            v => v == null ? null : v.FirstOrDefault(),
            null)
    {
    }
}
