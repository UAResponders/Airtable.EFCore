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
public class FormulaTranslationTests : FinanceTestBase
{

    [TestMethod]
    public async Task TranslateNoExchangeRateQuery()
    {
        var (sp, clientMoq) = SetupServices();
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

