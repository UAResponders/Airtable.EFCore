using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Airtable.EFCore.Storage.Internal;
using AirtableApiClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Airtable.EFCore.Tests;

[TestClass]
public class FormulaTranslationTests
{
    private static readonly AirtableRecordList _emptyList = JsonSerializer.Deserialize<AirtableRecordList>(@"{""records"":[]}");

    [TestMethod]
    public async Task TranslateNoExchangeRateQuery()
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
            .Returns(new AirtableListRecordsResponse(_emptyList));

        services.AddEntityFrameworkAirtableDatabase();

        services.AddSingleton(s => clientMoq.Object);

        services.AddDbContext<FinanceDbContext>(o=>o.UseAirtable("dummy", "dummy").UseInternalServiceProvider(services.BuildServiceProvider()));

        var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();

        await db.Transactions
                .Where(i =>
                    i.Currency != null
                        && (
                            i.EurRate == null ||
                            i.PlnRate == null))
                .ToListAsync();

        var formula = clientMoq.Invocations.First().Arguments[3];

        Assert.AreEqual("AND({Currency},OR(NOT({EUR Exchange rate}),NOT({PLN Exchange rate})))", formula);
    }
}

