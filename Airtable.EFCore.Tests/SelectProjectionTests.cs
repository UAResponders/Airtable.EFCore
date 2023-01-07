using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Airtable.EFCore.Tests;

[TestClass]
public class SelectProjectionTests : FinanceTestBase
{
    [TestMethod]
    public async Task SingleFieldSelectTest()
    {
        var (sp, clientMoq) = SetupServices();
        using var scope = sp.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();

        var name = await db.Transactions.Select(i => i.Name).FirstOrDefaultAsync();

        var fields = clientMoq.Invocations.Single().Arguments[2];

        var fieldsArray = ((IEnumerable<string>)fields).ToArray();

        Assert.AreEqual(1, fieldsArray.Length);
        Assert.AreEqual("Name", fieldsArray[0]);
    }
}
