using Airtable.EFCore.Storage.Internal;
using AirtableApiClient;
using Microsoft.EntityFrameworkCore.Query;

namespace Airtable.EFCore.Query.Internal;

internal sealed class AirtableQueryContextFactory : IQueryContextFactory
{
    private readonly QueryContextDependencies _dependencies;
    private readonly AirtableBase _airtableBase;

    public AirtableQueryContextFactory(
        QueryContextDependencies dependencies,
        AirtableBaseWrapper airtableBase)
    {
        _dependencies = dependencies;
        _airtableBase = airtableBase.Base;
    }

    public QueryContext Create()
    {
        return new AirtableQueryContext(_dependencies, _airtableBase);
    }
}

internal sealed class AirtableQueryContext : QueryContext
{
    public AirtableBase AirtableClient { get; }

    public AirtableQueryContext(
        QueryContextDependencies dependencies, 
        AirtableBase airtableClient) : base(dependencies)
    {
        AirtableClient = airtableClient;
    }

}