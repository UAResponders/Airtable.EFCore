using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Airtable.EFCore.Tests;

[TestClass]
public class ConfigurationTests
{
    [TestMethod]
    public void GenerateServiceProviderLeak()
    {
        var services = new ServiceCollection();

        services.AddDbContext<FinanceDbContext>(o=>o.UseAirtable("dummy", "dummy"));

        var sp = services.BuildServiceProvider();

        for (int i = 0; i < 100; i++)
        {
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
        }
    }
}