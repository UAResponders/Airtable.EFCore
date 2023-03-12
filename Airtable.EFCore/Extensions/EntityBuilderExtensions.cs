using Airtable.EFCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Airtable.EFCore;

public static class EntityBuilderExtensions
{
    public static void ToTable(this IConventionEntityTypeBuilder builder, string table, bool fromDataAnnotation)
    {
        builder.Metadata.SetAnnotation(AirtableAnnotationNames.TableName, table, fromDataAnnotation);
    }

    public static void ToTable(this IConventionEntityTypeBuilder builder, string table)
        => builder.ToTable(table, fromDataAnnotation: false);

    public static void ToTable(this EntityTypeBuilder builder, string table)
    {
        builder.Metadata.SetAnnotation(AirtableAnnotationNames.TableName, table);
    }

    public static string? GetTableName(this IReadOnlyEntityType type)
    {
        return type.FindAnnotation(AirtableAnnotationNames.TableName)?.Value?.ToString();
    }

    public static void SetColumnName(this IConventionPropertyBuilder propertyBuilder, string name)
    {
        propertyBuilder.Metadata.SetAnnotation(AirtableAnnotationNames.ColumnName, name);
    }

    public static string? GetColumnName(this IReadOnlyProperty property)
    {
        return property.FindAnnotation(AirtableAnnotationNames.ColumnName)?.Value?.ToString();
    }
}
