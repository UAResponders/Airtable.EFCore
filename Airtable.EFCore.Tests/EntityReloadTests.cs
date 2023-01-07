
using System;
using System.Threading.Tasks;
using AirtableApiClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Airtable.EFCore.Tests;

[TestClass]
public class EntityReloadTests : FinanceTestBase
{
    [TestMethod]
    public async Task ReloadShouldCallSingleEntityLoad()
    {
        var recordId = Guid.NewGuid().ToString();

        var (sp, clientMoq) = SetupServices();
        using var scope = sp.CreateScope();

        clientMoq
            .Setup(i => i.GetRecord("Transactions", recordId))
            .ReturnsAsync(new AirtableRetrieveRecordResponse(GetRecordWithId(recordId)));

        var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();

        var entity = new DbTransaction { Id = recordId };

        db.Attach(entity);

        await db.Entry(entity).ReloadAsync();

        Assert.AreEqual(1, clientMoq.Invocations.Count);
        Assert.AreEqual(nameof(clientMoq.Object.GetRecord), clientMoq.Invocations[0].Method.Name);
    }
}