using Airtable.EFCore.Storage.Internal;
using AirtableApiClient;
using Microsoft.EntityFrameworkCore.Query;

namespace Airtable.EFCore.Query.Internal;

internal sealed class AirtableQueryContextFactory : IQueryContextFactory
{
    private readonly QueryContextDependencies _dependencies;
    private readonly IAirtableClient _airtableBase;

    public AirtableQueryContextFactory(
        QueryContextDependencies dependencies,
        IAirtableClient airtableBase)
    {
        _dependencies = dependencies;
        _airtableBase = airtableBase;
    }

    public QueryContext Create()
    {
        return new AirtableQueryContext(_dependencies, _airtableBase);
    }
}

internal sealed class AirtableQueryContext : QueryContext
{
    public IAirtableClient AirtableClient { get; }

    public AirtableQueryContext(
        QueryContextDependencies dependencies,
        IAirtableClient airtableClient) : base(dependencies)
    {
        AirtableClient = airtableClient;
    }

}