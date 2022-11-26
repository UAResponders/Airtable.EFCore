using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Airtable.EFCore.Storage.Internal;
using AirtableApiClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Airtable.EFCore.Tests;

public class FinanceTestBase
{
    protected AirtableRecordList EmptyList { get; }  = JsonSerializer.Deserialize<AirtableRecordList>(@"{""records"":[]}")!;

    protected (ServiceProvider serviceProvider, Mock<IAirtableClient> airtableClientMock) SetupServices(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();

        var clientMoq = new Mock<IAirtableClient>();
        clientMoq.Setup(
            i => i.ListRecords(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<IEnumerable<Sort>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>()).Result)
            .Returns(new AirtableListRecordsResponse(EmptyList));

        services.AddEntityFrameworkAirtableDatabase();

        services.AddSingleton(s => clientMoq.Object);

        configure?.Invoke(services);

        services.AddDbContext<FinanceDbContext>(o =>
        {
            o.UseAirtable("dummy", "dummy")
             .UseInternalServiceProvider(services.BuildServiceProvider())
             .LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Trace);
        });

        return (services.BuildServiceProvider(), clientMoq);
    }
}
