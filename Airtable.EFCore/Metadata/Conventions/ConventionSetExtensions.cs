using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace Airtable.EFCore.Metadata.Conventions;

internal static class ConventionSetExtensions
{
    //EF 7 will have this method on ConventionSet
    
    public static void Add(this ConventionSet conventionSet, IConvention convention)
    {
        if (convention is IModelInitializedConvention modelInitializedConvention)
        {
            conventionSet.ModelInitializedConventions.Add(modelInitializedConvention);
        }

        if (convention is IModelFinalizingConvention modelFinalizingConvention)
        {
            conventionSet.ModelFinalizingConventions.Add(modelFinalizingConvention);
        }

        if (convention is IModelFinalizedConvention modelFinalizedConvention)
        {
            conventionSet.ModelFinalizedConventions.Add(modelFinalizedConvention);
        }

        if (convention is IModelAnnotationChangedConvention modelAnnotationChangedConvention)
        {
            conventionSet.ModelAnnotationChangedConventions.Add(modelAnnotationChangedConvention);
        }

        if (convention is IEntityTypeAddedConvention entityTypeAddedConvention)
        {
            conventionSet.EntityTypeAddedConventions.Add(entityTypeAddedConvention);
        }

        if (convention is IEntityTypeIgnoredConvention entityTypeIgnoredConvention)
        {
            conventionSet.EntityTypeIgnoredConventions.Add(entityTypeIgnoredConvention);
        }

        if (convention is IEntityTypeRemovedConvention entityTypeRemovedConvention)
        {
            conventionSet.EntityTypeRemovedConventions.Add(entityTypeRemovedConvention);
        }

        if (convention is IEntityTypeMemberIgnoredConvention entityTypeMemberIgnoredConvention)
        {
            conventionSet.EntityTypeMemberIgnoredConventions.Add(entityTypeMemberIgnoredConvention);
        }

        if (convention is IEntityTypeBaseTypeChangedConvention entityTypeBaseTypeChangedConvention)
        {
            conventionSet.EntityTypeBaseTypeChangedConventions.Add(entityTypeBaseTypeChangedConvention);
        }

        if (convention is IEntityTypePrimaryKeyChangedConvention entityTypePrimaryKeyChangedConvention)
        {
            conventionSet.EntityTypePrimaryKeyChangedConventions.Add(entityTypePrimaryKeyChangedConvention);
        }

        if (convention is IEntityTypeAnnotationChangedConvention entityTypeAnnotationChangedConvention)
        {
            conventionSet.EntityTypeAnnotationChangedConventions.Add(entityTypeAnnotationChangedConvention);
        }

        if (convention is IForeignKeyAddedConvention foreignKeyAddedConvention)
        {
            conventionSet.ForeignKeyAddedConventions.Add(foreignKeyAddedConvention);
        }

        if (convention is IForeignKeyRemovedConvention foreignKeyRemovedConvention)
        {
            conventionSet.ForeignKeyRemovedConventions.Add(foreignKeyRemovedConvention);
        }

        if (convention is IForeignKeyPrincipalEndChangedConvention foreignKeyPrincipalEndChangedConvention)
        {
            conventionSet.ForeignKeyPrincipalEndChangedConventions.Add(foreignKeyPrincipalEndChangedConvention);
        }

        if (convention is IForeignKeyPropertiesChangedConvention foreignKeyPropertiesChangedConvention)
        {
            conventionSet.ForeignKeyPropertiesChangedConventions.Add(foreignKeyPropertiesChangedConvention);
        }

        if (convention is IForeignKeyUniquenessChangedConvention foreignKeyUniquenessChangedConvention)
        {
            conventionSet.ForeignKeyUniquenessChangedConventions.Add(foreignKeyUniquenessChangedConvention);
        }

        if (convention is IForeignKeyRequirednessChangedConvention foreignKeyRequirednessChangedConvention)
        {
            conventionSet.ForeignKeyRequirednessChangedConventions.Add(foreignKeyRequirednessChangedConvention);
        }

        if (convention is IForeignKeyDependentRequirednessChangedConvention foreignKeyDependentRequirednessChangedConvention)
        {
            conventionSet.ForeignKeyDependentRequirednessChangedConventions.Add(foreignKeyDependentRequirednessChangedConvention);
        }

        if (convention is IForeignKeyOwnershipChangedConvention foreignKeyOwnershipChangedConvention)
        {
            conventionSet.ForeignKeyOwnershipChangedConventions.Add(foreignKeyOwnershipChangedConvention);
        }

        if (convention is IForeignKeyAnnotationChangedConvention foreignKeyAnnotationChangedConvention)
        {
            conventionSet.ForeignKeyAnnotationChangedConventions.Add(foreignKeyAnnotationChangedConvention);
        }

        if (convention is IForeignKeyNullNavigationSetConvention foreignKeyNullNavigationSetConvention)
        {
            conventionSet.ForeignKeyNullNavigationSetConventions.Add(foreignKeyNullNavigationSetConvention);
        }

        if (convention is INavigationAddedConvention navigationAddedConvention)
        {
            conventionSet.NavigationAddedConventions.Add(navigationAddedConvention);
        }

        if (convention is INavigationAnnotationChangedConvention navigationAnnotationChangedConvention)
        {
            conventionSet.NavigationAnnotationChangedConventions.Add(navigationAnnotationChangedConvention);
        }

        if (convention is INavigationRemovedConvention navigationRemovedConvention)
        {
            conventionSet.NavigationRemovedConventions.Add(navigationRemovedConvention);
        }

        if (convention is ISkipNavigationAddedConvention skipNavigationAddedConvention)
        {
            conventionSet.SkipNavigationAddedConventions.Add(skipNavigationAddedConvention);
        }

        if (convention is ISkipNavigationAnnotationChangedConvention skipNavigationAnnotationChangedConvention)
        {
            conventionSet.SkipNavigationAnnotationChangedConventions.Add(skipNavigationAnnotationChangedConvention);
        }

        if (convention is ISkipNavigationForeignKeyChangedConvention skipNavigationForeignKeyChangedConvention)
        {
            conventionSet.SkipNavigationForeignKeyChangedConventions.Add(skipNavigationForeignKeyChangedConvention);
        }

        if (convention is ISkipNavigationInverseChangedConvention skipNavigationInverseChangedConvention)
        {
            conventionSet.SkipNavigationInverseChangedConventions.Add(skipNavigationInverseChangedConvention);
        }

        if (convention is ISkipNavigationRemovedConvention skipNavigationRemovedConvention)
        {
            conventionSet.SkipNavigationRemovedConventions.Add(skipNavigationRemovedConvention);
        }

        if (convention is IKeyAddedConvention keyAddedConvention)
        {
            conventionSet.KeyAddedConventions.Add(keyAddedConvention);
        }

        if (convention is IKeyRemovedConvention keyRemovedConvention)
        {
            conventionSet.KeyRemovedConventions.Add(keyRemovedConvention);
        }

        if (convention is IKeyAnnotationChangedConvention keyAnnotationChangedConvention)
        {
            conventionSet.KeyAnnotationChangedConventions.Add(keyAnnotationChangedConvention);
        }

        if (convention is IIndexAddedConvention indexAddedConvention)
        {
            conventionSet.IndexAddedConventions.Add(indexAddedConvention);
        }

        if (convention is IIndexRemovedConvention indexRemovedConvention)
        {
            conventionSet.IndexRemovedConventions.Add(indexRemovedConvention);
        }

        if (convention is IIndexUniquenessChangedConvention indexUniquenessChangedConvention)
        {
            conventionSet.IndexUniquenessChangedConventions.Add(indexUniquenessChangedConvention);
        }

        if (convention is IIndexAnnotationChangedConvention indexAnnotationChangedConvention)
        {
            conventionSet.IndexAnnotationChangedConventions.Add(indexAnnotationChangedConvention);
        }

        if (convention is IPropertyAddedConvention propertyAddedConvention)
        {
            conventionSet.PropertyAddedConventions.Add(propertyAddedConvention);
        }

        if (convention is IPropertyNullabilityChangedConvention propertyNullabilityChangedConvention)
        {
            conventionSet.PropertyNullabilityChangedConventions.Add(propertyNullabilityChangedConvention);
        }

        if (convention is IPropertyFieldChangedConvention propertyFieldChangedConvention)
        {
            conventionSet.PropertyFieldChangedConventions.Add(propertyFieldChangedConvention);
        }

        if (convention is IPropertyAnnotationChangedConvention propertyAnnotationChangedConvention)
        {
            conventionSet.PropertyAnnotationChangedConventions.Add(propertyAnnotationChangedConvention);
        }

        if (convention is IPropertyRemovedConvention propertyRemovedConvention)
        {
            conventionSet.PropertyRemovedConventions.Add(propertyRemovedConvention);
        }
    }
}
