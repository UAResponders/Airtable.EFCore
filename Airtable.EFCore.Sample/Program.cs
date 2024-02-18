// See https://aka.ms/new-console-template for more information
using System.ComponentModel.DataAnnotations.Schema;
using Airtable.EFCore.Metadata;
using AirtableApiClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

Console.WriteLine("Hello, World!");

var services = new ServiceCollection();

var config = new ConfigurationBuilder().AddUserSecrets<TestDbContext>().Build();

services.AddDbContext<TestDbContext>(o => o.UseAirtable(config.GetSection("BaseId").Value!, config.GetSection("ApiKey").Value!));
var sp = services.BuildServiceProvider();

var scope = sp.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();

var product = await db.ProductInventory.FirstOrDefaultAsync();

product.Manufacturer = "recMH8Vt92AadXO8i";

await db.SaveChangesAsync();

var manufacturerExists = await db.Manufacturers.FirstOrDefaultAsync();
var manufacturerExistsName = await db.Manufacturers.Where(i => String.Equals(i.ContactName, "AAAAAa", StringComparison.OrdinalIgnoreCase)).FirstOrDefaultAsync();

var manufacturerExistsNameAndId = await db.Manufacturers.Select(i => new { i.ContactName, Record = i.Id }).FirstOrDefaultAsync();

var query = db.Manufacturers.Take(5);

var sanManufacturer = await query.Take(3).ToArrayAsync();

var single = await query.Where(i => i.Name == "Satsuma Leather Goods").FirstOrDefaultAsync();

sanManufacturer[0].Name = "_______qrqwrq";
//await db.Entry(sanManufacturer[0]).ReloadAsync();

await db.SaveChangesAsync();

Console.WriteLine("Done");

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<DbProductInventory> ProductInventory { get; set; }
    public DbSet<DbManufacturer> Manufacturers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}

public class DbManufacturer
{
    public string Id { get; set; }
    public string Name { get; set; }
    [Column("Contact Name")]
    public string ContactName { get; set; }

    public AirtableAttachment Image { get; set; }
}

[Table("Product Inventory")]
public class DbProductInventory
{
    public string Id { get; set; }

    [Column("Units Ordered")]
    public int Ordered { get; set; }

    [Column("Product Name")]
    public string ProductName { get; set; }

    [SingleValueArray]
    public string? Manufacturer { get; set; }
}

public enum ProductType
{
    Scarf,
    Box,
    Blanket,
    Bag,
    [Column("Head Accessory")]
    HeadAccessory
}