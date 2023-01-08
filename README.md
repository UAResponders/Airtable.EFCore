# Airtable.EFCore

Entity Framework Core provider for [Airtable](https://airtable.com/).

Uses [Airtable.Net](https://github.com/ngocnicholas/airtable.net) library by [ngocnicholas](https://github.com/ngocnicholas).

Based and inspired mostly by [Cosmos DB provider](https://github.com/dotnet/efcore/tree/main/src/EFCore.Cosmos) by Microsoft.

## State of the project and motivation

This project was created to support charity operations of [UA Responders](https://uaresponders.org/) charity that runs it's internal operations on Airtable and Azure Functions. 

Because Airtable is a good simple UI and database, but lacks good automation facilities, we run some of our automations on Azure Functions in C#.

Currently all operations on Azure Functions are ported to work on top of this provider, and features added are based on needs to support those operations.

Only small subset of LINQ is translated, but there is a foundation to add features as needed.

Changetracking and `SaveChangesAsync()` works, so you can do full CRUD cycle.

## Usage

Create Airtable base

Get Airtable base ID and API key

Install `Airtable.EFCore` package.

```csharp
//Register airtable as EF provider
services.AddDbContext<TestDbContext>(o => o.UseAirtable(config.GetValue<string>("BaseId"), config.GetValue<string>("ApiKey")));
```

# Example model:

Create your model. Example uses [Inventory Tracking](https://www.airtable.com/templates/inventory-tracker/expDrHGuyjSQlrKTq) template.

```csharp

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

public class DbProductInventory
{
    public string Id { get; set; }

    [Column("Units Ordered")]
    public int Ordered { get; set; }

    [Column("Product Name")]
    public string ProductName { get; set; }
}
```

And you can work with it as with regular EF model!

# Translated features

## Airtable Views

`FromView` extension limit querying to specific view.

```csharp
await db.Manufacturers.FromView("San").FirstOrDefaultAsync(i=>i.Name == "Something");
```

## Airtable formulas translated

| C# | Airtable | Comment |
| - | - | - |
| `=` | `=` | Simple equality operator |
| `Regex.IsMatch` | `REGEX_MATCH()` | No options additional supported by airtable |
| `String.Equals` | Equality operator `=` | Case insensitivity achieved via `UPPER` function  |
| `String.Contains` | `FIND()` | Case insensitivity achieved via `UPPER` function  |
| `&&` | `AND`| |
| `\|\|` | `OR` | |
| `DateTimeOffset` comparison operators | Combination of `NOT` and `IS_AFTER` and `IS_BEFORE`| |
| Entity property reference | Record property reference encoded in `{}` | |
| Entity primary key reference | `RECORD_ID()` | With special optimization for query by primary key only |

## Attachment support

Use property of type `AirtableAttachment` or `ICollection<AirtableAttachment>` to work with attachments. 

