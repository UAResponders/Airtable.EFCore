// See https://aka.ms/new-console-template for more information
using System.ComponentModel.DataAnnotations.Schema;
using Airtable.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

Console.WriteLine("Hello, World!");


var services = new ServiceCollection();

var config = new ConfigurationBuilder().AddUserSecrets<TestDbContext>().Build();

services.AddDbContext<TestDbContext>(o => o.UseAirtable(config.GetSection("BaseId").Value, config.GetSection("ApiKey").Value));
var sp = services.BuildServiceProvider();

var scope = sp.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();

var manufacturerExists = await db.Manufacturers.FirstOrDefaultAsync();
var manufacturerExistsName = await db.Manufacturers.Select(i => i.ContactName).FirstOrDefaultAsync();
var manufacturerExistsNameAndId = await db.Manufacturers.Select(i => new { i.ContactName, Record = i.Id }).FirstOrDefaultAsync();

var newItem = new DbManufacturer { ContactName = "AAAAAA" };

db.Manufacturers.Add(newItem);

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
        modelBuilder.Entity<DbProductInventory>().ToTable("Product Inventory");
        var mdb = modelBuilder.Entity<DbManufacturer>().Property(i => i.Id);
        base.OnModelCreating(modelBuilder);
    }
}

public class DbManufacturer
{
    public string Id { get; set; }
    public string Name { get; set; }
    [Column("Contact Name")]
    public string ContactName { get; set; }
}

public class DbProductInventory
{
    public string Id { get; set; }

    [Column("Units Ordered")]
    public int Ordered { get; set; }

    [Column("Product Name")]
    public string ProductName { get; set; }
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