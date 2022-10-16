using Airtable.EFCore.Diagnostics.Internal;
using Airtable.EFCore.Infrastructure;
using Airtable.EFCore.Metadata.Conventions;
using Airtable.EFCore.Query.Internal;
using Airtable.EFCore.Storage.Internal;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;

namespace Microsoft.EntityFrameworkCore;

public static class AirtableServiceCollectionExtensions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IServiceCollection AddEntityFrameworkAirtableDatabase(this IServiceCollection serviceCollection)
    {
        var builder = new EntityFrameworkServicesBuilder(serviceCollection)
            .TryAdd<LoggingDefinitions, AirtableLoggingDefinitions>()
            .TryAdd<IDatabaseProvider, DatabaseProvider<AirtableOptionsExtension>>()
            .TryAdd<IDatabase, AirtableDatabase>()
            .TryAdd<IQueryContextFactory, AirtableQueryContextFactory>()
            .TryAdd<ITypeMappingSource, AirtableTypeMappingSource>()
            .TryAdd<IShapedQueryCompilingExpressionVisitorFactory, AirtableShapedQueryCompilingExpressionVisitorFactory>()
            .TryAdd<IQueryableMethodTranslatingExpressionVisitorFactory, AirtableQueryableMethodTranslatingExpressionVisitorFactory>()
            .TryAdd<IProviderConventionSetBuilder, AirtableConventionSetBuilder>()
            .TryAddProviderSpecificServices(
                b => b.TryAddScoped<AirtableBaseWrapper, AirtableBaseWrapper>()
                      .TryAddScoped<IFormulaExpressionFactory, FormulaExpressionFactory>()
            )
            ;

        builder.TryAddCoreServices();

        return serviceCollection;
    }
}
