﻿using System.Linq.Expressions;
using Airtable.EFCore.Query.Internal.MethodTranslators;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;

namespace Airtable.EFCore.Query.Internal;

internal sealed class AirtableQueryableMethodTranslatingExpressionVisitor : QueryableMethodTranslatingExpressionVisitor
{
    private readonly IFormulaExpressionFactory _formulaExpressionFactory;
    private readonly AirtableProjectionBindingExpressionVisitor _projectionBindingExpressionVisitor;
    private readonly IMethodCallTranslatorProvider _methodCallTranslator;

    public AirtableQueryableMethodTranslatingExpressionVisitor(
        QueryableMethodTranslatingExpressionVisitorDependencies dependencies,
        QueryCompilationContext queryCompilationContext,
        IFormulaExpressionFactory formulaExpressionFactory,
        IMethodCallTranslatorProvider methodCallTranslator)
        : base(dependencies, queryCompilationContext, subquery: false)
    {
        _formulaExpressionFactory = formulaExpressionFactory;
        _projectionBindingExpressionVisitor = new AirtableProjectionBindingExpressionVisitor(formulaExpressionFactory, methodCallTranslator);
        _methodCallTranslator = methodCallTranslator;
    }

    protected override ShapedQueryExpression CreateShapedQueryExpression(IEntityType entityType)
    {
        var selectExpression = new SelectExpression(entityType);

        return CreateShapedQueryExpression(entityType, selectExpression);
    }

    protected override Expression VisitExtension(Expression extensionExpression)
    {
        if (extensionExpression is FromViewQueryRootExpression view)
        {
            return CreateShapedQueryExpression(view.EntityType, new SelectExpression(view.EntityType) { View = view.View });
        }

        return base.VisitExtension(extensionExpression);
    }

    private static ShapedQueryExpression CreateShapedQueryExpression(IEntityType entityType, Expression queryExpression)
        => new(
            queryExpression,
            new StructuralTypeShaperExpression(
                entityType,
                new ProjectionBindingExpression(queryExpression, new ProjectionMember(), typeof(ValueBuffer)),
                false));

    protected override QueryableMethodTranslatingExpressionVisitor CreateSubqueryVisitor()
    {
        throw new NotImplementedException();
    }

    protected override ShapedQueryExpression? TranslateAll(ShapedQueryExpression source, LambdaExpression predicate)
    {
        throw new NotImplementedException();
    }

    protected override ShapedQueryExpression? TranslateAny(ShapedQueryExpression source, LambdaExpression? predicate)
    {
        throw new NotImplementedException();
    }

    protected override ShapedQueryExpression? TranslateAverage(ShapedQueryExpression source, LambdaExpression? selector, Type resultType)
    {
        throw new NotImplementedException();
    }

    protected override ShapedQueryExpression? TranslateCast(ShapedQueryExpression source, Type castType)
    {
        throw new NotImplementedException();
    }

    protected override ShapedQueryExpression? TranslateConcat(ShapedQueryExpression source1, ShapedQueryExpression source2)
    {
        throw new NotImplementedException();
    }

    protected override ShapedQueryExpression? TranslateContains(ShapedQueryExpression source, Expression item)
    {
        throw new NotImplementedException();
    }

    protected override ShapedQueryExpression? TranslateCount(ShapedQueryExpression source, LambdaExpression? predicate)
    {
        throw new NotImplementedException();
    }

    protected override ShapedQueryExpression? TranslateDefaultIfEmpty(ShapedQueryExpression source, Expression? defaultValue)
    {
        throw new NotImplementedException();
    }

    protected override ShapedQueryExpression? TranslateDistinct(ShapedQueryExpression source)
    {
        throw new NotImplementedException();
    }

    protected override ShapedQueryExpression? TranslateElementAtOrDefault(ShapedQueryExpression source, Expression index, bool returnDefault)
    {
        throw new NotImplementedException();
    }

    protected override ShapedQueryExpression? TranslateExcept(ShapedQueryExpression source1, ShapedQueryExpression source2)
    {
        throw new NotImplementedException();
    }

    protected override ShapedQueryExpression? TranslateFirstOrDefault(ShapedQueryExpression source, LambdaExpression? predicate, Type returnType, bool returnDefault)
    {
        var rootExpression = (SelectExpression)source.QueryExpression;

        if (predicate != null)
        {
            var newSource = TranslateWhere(source, predicate);
            if (newSource == null)
            {
                return null;
            }

            source = newSource;
        }

        rootExpression.Limit = Expression.Constant(1);

        return source.ShaperExpression.Type != returnType
            ? source.UpdateShaperExpression(Expression.Convert(source.ShaperExpression, returnType))
            : source;
    }

    protected override ShapedQueryExpression? TranslateGroupBy(ShapedQueryExpression source, LambdaExpression keySelector, LambdaExpression? elementSelector, LambdaExpression? resultSelector)
    {
        throw new NotImplementedException();
    }

    protected override ShapedQueryExpression? TranslateGroupJoin(ShapedQueryExpression outer, ShapedQueryExpression inner, LambdaExpression outerKeySelector, LambdaExpression innerKeySelector, LambdaExpression resultSelector)
    {
        throw new NotImplementedException();
    }

    protected override ShapedQueryExpression? TranslateIntersect(ShapedQueryExpression source1, ShapedQueryExpression source2)
    {
        throw new NotImplementedException();
    }

    protected override ShapedQueryExpression? TranslateJoin(ShapedQueryExpression outer, ShapedQueryExpression inner, LambdaExpression outerKeySelector, LambdaExpression innerKeySelector, LambdaExpression resultSelector)
    {
        throw new NotImplementedException();
    }

    protected override ShapedQueryExpression? TranslateLastOrDefault(ShapedQueryExpression source, LambdaExpression? predicate, Type returnType, bool returnDefault)
    {
        throw new NotImplementedException();
    }

    protected override ShapedQueryExpression? TranslateLeftJoin(ShapedQueryExpression outer, ShapedQueryExpression inner, LambdaExpression outerKeySelector, LambdaExpression innerKeySelector, LambdaExpression resultSelector)
    {
        throw new NotImplementedException();
    }

    protected override ShapedQueryExpression? TranslateLongCount(ShapedQueryExpression source, LambdaExpression? predicate)
    {
        throw new NotImplementedException();
    }

    protected override ShapedQueryExpression? TranslateMax(ShapedQueryExpression source, LambdaExpression? selector, Type resultType)
    {
        throw new NotImplementedException();
    }

    protected override ShapedQueryExpression? TranslateMin(ShapedQueryExpression source, LambdaExpression? selector, Type resultType)
    {
        throw new NotImplementedException();
    }

    protected override ShapedQueryExpression? TranslateOfType(ShapedQueryExpression source, Type resultType)
    {
        throw new NotImplementedException();
    }

    protected override ShapedQueryExpression? TranslateOrderBy(ShapedQueryExpression source, LambdaExpression keySelector, bool ascending)
    {
        var translation = TranslateLambdaExpression(source, keySelector);
        if (translation != null)
        {
            if (translation is not TablePropertyReferenceExpression tablePropertyReferenceExpression)
            {
                throw new InvalidOperationException("Only direct column references are allowed for ordering");
            }

            ((SelectExpression)source.QueryExpression).ApplyOrdering(tablePropertyReferenceExpression, !ascending);

            return source;
        }

        return null;
    }

    protected override ShapedQueryExpression? TranslateReverse(ShapedQueryExpression source)
    {
        throw new NotImplementedException();
    }

    protected override ShapedQueryExpression TranslateSelect(ShapedQueryExpression source, LambdaExpression selector)
    {
        if (selector.Body == selector.Parameters[0]) return source;

        var selectExpression = (SelectExpression)source.QueryExpression;

        var newSelectorBody = ReplacingExpressionVisitor.Replace(selector.Parameters.Single(), source.ShaperExpression, selector.Body);

        return source.UpdateShaperExpression(_projectionBindingExpressionVisitor.Translate(selectExpression, newSelectorBody));
    }

    protected override ShapedQueryExpression? TranslateSelectMany(ShapedQueryExpression source, LambdaExpression collectionSelector, LambdaExpression resultSelector)
    {
        throw new NotImplementedException();
    }

    protected override ShapedQueryExpression? TranslateSelectMany(ShapedQueryExpression source, LambdaExpression selector)
    {
        throw new NotImplementedException();
    }

    protected override ShapedQueryExpression? TranslateSingleOrDefault(ShapedQueryExpression source, LambdaExpression? predicate, Type returnType, bool returnDefault)
    {
        throw new NotImplementedException();
    }

    protected override ShapedQueryExpression? TranslateSkip(ShapedQueryExpression source, Expression count)
    {
        throw new NotImplementedException();
    }

    protected override ShapedQueryExpression? TranslateSkipWhile(ShapedQueryExpression source, LambdaExpression predicate)
    {
        throw new NotImplementedException();
    }

    protected override ShapedQueryExpression? TranslateSum(ShapedQueryExpression source, LambdaExpression? selector, Type resultType)
    {
        throw new NotImplementedException();
    }

    protected override ShapedQueryExpression? TranslateTake(ShapedQueryExpression source, Expression count)
    {
        var selectExpression = (SelectExpression)source.QueryExpression;
        selectExpression.Limit = count;
        return source;
    }

    protected override ShapedQueryExpression? TranslateTakeWhile(ShapedQueryExpression source, LambdaExpression predicate)
    {
        throw new NotImplementedException();
    }

    protected override ShapedQueryExpression? TranslateThenBy(ShapedQueryExpression source, LambdaExpression keySelector, bool ascending)
    {
        throw new NotImplementedException();
    }

    protected override ShapedQueryExpression? TranslateUnion(ShapedQueryExpression source1, ShapedQueryExpression source2)
    {
        throw new NotImplementedException();
    }

    private static Expression RemapLambdaBody(Expression shaperBody, LambdaExpression lambdaExpression)
        => ReplacingExpressionVisitor.Replace(lambdaExpression.Parameters.Single(), shaperBody, lambdaExpression.Body);

    protected override ShapedQueryExpression? TranslateWhere(ShapedQueryExpression source, LambdaExpression predicate)
    {
        var queryExpr = (source.QueryExpression as SelectExpression) ?? throw new InvalidOperationException("Query should be used in entity select");
        var query = RemapLambdaBody(source.ShaperExpression, predicate);

        var entity = queryExpr.EntityType;

        var result = new AirtableFormulaTranslatorExpressionVisitor(
            _formulaExpressionFactory,
            entity,
            _methodCallTranslator)
            .Translate(query);

        if (result == null)
            throw new InvalidOperationException("Failed to translate expresison");

        queryExpr.AddPredicate(result);

        return source;
    }

    private FormulaExpression TranslateLambdaExpression(
            ShapedQueryExpression shapedQueryExpression,
            LambdaExpression lambdaExpression)
    {
        var select = (SelectExpression)shapedQueryExpression.QueryExpression;
        var lambdaBody = RemapLambdaBody(shapedQueryExpression.ShaperExpression, lambdaExpression);

        return TranslateExpression(lambdaBody, select.EntityType);
    }

    private FormulaExpression TranslateExpression(Expression expression, IEntityType entityType)
    {
        var translator = new AirtableFormulaTranslatorExpressionVisitor(_formulaExpressionFactory, entityType, _methodCallTranslator);
        var translation = translator.Translate(expression) ?? throw new InvalidOperationException($"Failed to translate {expression}");
        return translation;
    }
}
