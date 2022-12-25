using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Airtable.EFCore.Query.Internal;
using Microsoft.EntityFrameworkCore;

namespace Airtable.EFCore;

public static class ViewExtensions
{
    private static readonly MethodInfo _fromViewMethodInfo = typeof(ViewExtensions).GetMethod(nameof(FromView))!;

    public static IQueryable<T> FromView<T>(this DbSet<T> set, string viewName)
        where T : class
    {
        if (set is null)
            throw new ArgumentNullException(nameof(set));

        if (String.IsNullOrWhiteSpace(viewName))
            throw new ArgumentException($"'{nameof(viewName)}' cannot be null or whitespace.", nameof(viewName));

        var query = set.AsQueryable();

        var select = new FromViewQueryRootExpression(set.EntityType, viewName);

        return set.AsQueryable().Provider.CreateQuery<T>(select);
    }
}