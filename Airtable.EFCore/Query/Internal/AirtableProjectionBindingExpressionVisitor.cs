using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Airtable.EFCore.Query.Internal.MethodTranslators;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;

namespace Airtable.EFCore.Query.Internal;

internal sealed class AirtableProjectionBindingExpressionVisitor : ExpressionVisitor
{
    private AirtableFormulaTranslatorExpressionVisitor? _airtableFormulaTranslator;
    private SelectExpression? _selectExpression;
    private bool _clientEval;
    private readonly Dictionary<ProjectionMember, Expression> _projectionMapping = new();
    private readonly Stack<ProjectionMember> _projectionMembers = new();
    private readonly IFormulaExpressionFactory _formulaExpressionFactory;
    private readonly IMethodCallTranslatorProvider _methodCallTranslatorProvider;

    public AirtableProjectionBindingExpressionVisitor(
        IFormulaExpressionFactory formulaExpressionFactory,
        IMethodCallTranslatorProvider methodCallTranslatorProvider)
    {
        _formulaExpressionFactory = formulaExpressionFactory;
        _methodCallTranslatorProvider = methodCallTranslatorProvider;
    }

    public Expression Translate(SelectExpression selectExpression, Expression expression)
    {
        _airtableFormulaTranslator = new AirtableFormulaTranslatorExpressionVisitor(
            _formulaExpressionFactory,
            selectExpression.EntityType,
            _methodCallTranslatorProvider
            );
        _selectExpression = selectExpression;
        _clientEval = false;

        _projectionMembers.Push(new ProjectionMember());

        var result = Visit(expression);
        if (result == null)
        {
            _clientEval = true;

            result = Visit(expression);

            _projectionMapping.Clear();
        }

        _selectExpression.ReplaceProjectionMapping(_projectionMapping);
        _selectExpression = null;
        _projectionMembers.Clear();
        _projectionMapping.Clear();

        result = MatchTypes(result, expression.Type) ?? throw new InvalidOperationException("Failed to translate projection bindings");

        return result;
    }

    public override Expression? Visit(Expression? expression)
    {
        if (_airtableFormulaTranslator is null)
            throw new InvalidOperationException("_airtableFormulaTranslator is null. Call this method through Translate");
        if (_selectExpression is null)
            throw new InvalidOperationException("_selectExpression is null. Call this method through Translate");

        if (expression == null)
        {
            return null;
        }

        if (expression
            is NewExpression
            or MemberInitExpression
            or StructuralTypeShaperExpression)
        {
            return base.Visit(expression);
        }

        if (_clientEval)
        {
            switch (expression)
            {
                case ConstantExpression:
                    return expression;

                //case ParameterExpression parameterExpression:
                //    if (_collectionShaperMapping.ContainsKey(parameterExpression))
                //    {
                //        return parameterExpression;
                //    }

                //    if (parameterExpression.Name?.StartsWith(QueryCompilationContext.QueryParameterPrefix, StringComparison.Ordinal)
                //        == true)
                //    {
                //        return Expression.Call(
                //            GetParameterValueMethodInfo.MakeGenericMethod(parameterExpression.Type),
                //            QueryCompilationContext.QueryContextParameter,
                //            Expression.Constant(parameterExpression.Name));
                //    }

                //    throw new InvalidOperationException(CoreStrings.TranslationFailed(parameterExpression.Print()));

                case MaterializeCollectionNavigationExpression:
                    return base.Visit(expression);
            }

            var translation = _airtableFormulaTranslator.Translate(expression);
            if (translation == null)
            {
                return base.Visit(expression);
            }

            return new ProjectionBindingExpression(
                _selectExpression,
                _selectExpression.AddToProjection(translation),
                expression.Type);
        }
        else
        {
            var translation = _airtableFormulaTranslator.Translate(expression);
            if (translation == null)
            {
                return null;
            }

            _projectionMapping[_projectionMembers.Peek()] = translation;

            return new ProjectionBindingExpression(_selectExpression, _projectionMembers.Peek(), expression.Type.MakeNullable());
        }
    }

    protected override Expression VisitNew(NewExpression newExpression)
    {
        if (newExpression.Arguments.Count == 0)
        {
            return newExpression;
        }

        if (!_clientEval
            && newExpression.Members == null)
        {
            return null;
        }

        var newArguments = new Expression[newExpression.Arguments.Count];
        for (var i = 0; i < newArguments.Length; i++)
        {
            var argument = newExpression.Arguments[i];
            Expression visitedArgument;
            if (_clientEval)
            {
                visitedArgument = Visit(argument);
            }
            else
            {
                var projectionMember = _projectionMembers.Peek().Append(newExpression.Members[i]);
                _projectionMembers.Push(projectionMember);
                visitedArgument = Visit(argument);
                if (visitedArgument == null)
                {
                    return null;
                }

                _projectionMembers.Pop();
            }

            newArguments[i] = MatchTypes(visitedArgument, argument.Type);
        }

        return newExpression.Update(newArguments);
    }

    protected override Expression VisitExtension(Expression extensionExpression)
    {
        switch (extensionExpression)
        {
            case StructuralTypeShaperExpression entityShaperExpression:
            {
                var projectionBindingExpression = (ProjectionBindingExpression)entityShaperExpression.ValueBufferExpression;
                //VerifySelectExpression(projectionBindingExpression);

                if (_clientEval)
                {
                    var entityProjection = (EntityProjectionExpression)_selectExpression.GetMappedProjection(
                        projectionBindingExpression.ProjectionMember);

                    return entityShaperExpression.Update(
                        new ProjectionBindingExpression(
                            _selectExpression, _selectExpression.AddToProjection(entityProjection), typeof(ValueBuffer)));
                }

                _projectionMapping[_projectionMembers.Peek()]
                    = _selectExpression.GetMappedProjection(projectionBindingExpression.ProjectionMember);

                return entityShaperExpression.Update(
                    new ProjectionBindingExpression(_selectExpression, _projectionMembers.Peek(), typeof(ValueBuffer)));
            }

            //case MaterializeCollectionNavigationExpression materializeCollectionNavigationExpression:
            //    return materializeCollectionNavigationExpression.Navigation is INavigation embeddableNavigation
            //        && embeddableNavigation.IsEmbedded()
            //            ? base.Visit(materializeCollectionNavigationExpression.Subquery)
            //            : base.VisitExtension(materializeCollectionNavigationExpression);

            //case IncludeExpression includeExpression:
            //    if (!_clientEval)
            //    {
            //        return null;
            //    }

            //    if (!(includeExpression.Navigation is INavigation includableNavigation
            //            && includableNavigation.IsEmbedded()))
            //    {
            //        throw new InvalidOperationException(
            //            CosmosStrings.NonEmbeddedIncludeNotSupported(includeExpression.Navigation));
            //    }

            //    _includedNavigations.Push(includableNavigation);

            //    var newIncludeExpression = base.VisitExtension(includeExpression);

            //    _includedNavigations.Pop();

            //    return newIncludeExpression;

            default:
                throw new InvalidOperationException(CoreStrings.TranslationFailed(extensionExpression.Print()));
        }
    }



    [return: NotNullIfNotNull(nameof(expression))]
    private static Expression? MatchTypes(Expression? expression, Type targetType)
    {
        if (expression == null) return null;

        if (targetType != expression.Type
            && targetType.TryGetSequenceType() == null)
        {
            Debug.Assert(targetType.MakeNullable() == expression.Type, "expression.Type must be nullable of targetType");

            expression = Expression.Convert(expression, targetType);
        }

        return expression;
    }
}
