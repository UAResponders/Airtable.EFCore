using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Microsoft.EntityFrameworkCore;

public static class AirtableDatabaseFacadeExtensions
{
    public static bool IsAirtable(this DatabaseFacade database)
        => database.ProviderName == typeof(AirtableDatabaseFacadeExtensions).Assembly.GetName().Name;
}

