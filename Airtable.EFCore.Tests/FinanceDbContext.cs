using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

public sealed class FinanceDbContext : DbContext
{
    public FinanceDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<DbTransaction> Transactions { get; set; } = null!;
    public DbSet<DbArticleInRequest> ArticleInRequests { get; set; } = null!;
}

[Table("Articles in Requests")]
public class DbArticleInRequest
{
    public string Id { get; set; }
}

public class DbTransaction
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Currency { get; set; }
    public DateTime Timestamp { get; set; }
    [Column("PLN Exchange rate")]
    public decimal? PlnRate { get; set; }
    [Column("EUR Exchange rate")]
    public decimal? EurRate { get; set; }
}