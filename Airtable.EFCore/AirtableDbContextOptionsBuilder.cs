using Microsoft.EntityFrameworkCore;

namespace Airtable.EFCore.Infrastructure;

public class AirtableDbContextOptionsBuilder
{
    public AirtableDbContextOptionsBuilder(DbContextOptionsBuilder optionsBuilder)
    {
        OptionsBuilder = optionsBuilder;
    }

    public DbContextOptionsBuilder OptionsBuilder { get; }
}