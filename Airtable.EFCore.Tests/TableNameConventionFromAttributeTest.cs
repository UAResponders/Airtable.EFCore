using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Airtable.EFCore.Tests;

[TestClass]
public sealed class TableNameConventionFromAttributeTest : FinanceTestBase
{
    [TestMethod]
    public async Task AttributeShouldWorkAsTableName()
    {
        var (services, mock) = base.SetupServices();

        var dbContext = services.GetRequiredService<FinanceDbContext>();

        await dbContext.ArticleInRequests.ToArrayAsync();

        mock.Invocations.Single(i => String.Equals(i.Arguments[0], "Articles in Requests"));
    }
}