using Airtable.EFCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Microsoft.EntityFrameworkCore;

public static class AirtableDbContextOptionsExtensions
{
    public static DbContextOptionsBuilder<TContext> UseAirtable<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder,
        string baseId,
        string apiKey,
        Action<AirtableDbContextOptionsBuilder>? airtableOptionsAction = null)
        where TContext : DbContext
        => (DbContextOptionsBuilder<TContext>)UseAirtable(
            (DbContextOptionsBuilder)optionsBuilder, baseId, apiKey, airtableOptionsAction);

    public static DbContextOptionsBuilder UseAirtable(
        this DbContextOptionsBuilder optionsBuilder,
        string baseId,
        string apiKey,
        Action<AirtableDbContextOptionsBuilder>? airtableOptionsAction = null)
    {
        var extension = optionsBuilder.Options.FindExtension<AirtableOptionsExtension>() ?? new AirtableOptionsExtension();

        extension = extension.WithApiKey(apiKey).WithBaseId(baseId);

        ConfigureWarnings(optionsBuilder);

        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

        airtableOptionsAction?.Invoke(new AirtableDbContextOptionsBuilder(optionsBuilder));

        return optionsBuilder;
    }

    private static void ConfigureWarnings(DbContextOptionsBuilder optionsBuilder)
    {
        var coreOptionsExtension
            = optionsBuilder.Options.FindExtension<CoreOptionsExtension>()
            ?? new CoreOptionsExtension();

        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(coreOptionsExtension);
    }
}
