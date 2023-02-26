using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Airtable.EFCore.Storage.Internal;
using AirtableApiClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;

namespace Airtable.EFCore.Query.Internal;

internal sealed class AirtableShapedQueryCompilingExpressionVisitor : ShapedQueryCompilingExpressionVisitor
{
    private abstract class AirtableProjectionBindingRemovingVisitorBase : ExpressionVisitor
    {
        private static readonly MethodInfo _dictionaryTryGetValueMethod =
            typeof(Dictionary<string, object>)
                .GetMethod(
                    nameof(Dictionary<string, object>.TryGetValue))
                ?? throw new InvalidOperationException("Could not find method TryGetValue");

        private static readonly MethodInfo _visitorReadSingleValueMethod =
            typeof(AirtableProjectionBindingRemovingVisitorBase)
                .GetMethod(
                    nameof(AirtableProjectionBindingRemovingVisitorBase.ReadSingleValue),
                    BindingFlags.NonPublic | BindingFlags.Static,
                    new[] { typeof(JsonElement), typeof(JsonSerializerOptions) })
                ?? throw new InvalidOperationException("Could not find method ReadSingleValue");

        private static readonly MethodInfo _visitorReadRawMethod =
            typeof(AirtableProjectionBindingRemovingVisitorBase)
                .GetMethod(
                    nameof(AirtableProjectionBindingRemovingVisitorBase.ReadRaw),
                    BindingFlags.NonPublic | BindingFlags.Static,
                    new[] { typeof(JsonElement), typeof(JsonSerializerOptions) })
                ?? throw new InvalidOperationException("Could not find method ReadRaw");

        private readonly ParameterExpression _recordParameter;
        private readonly bool _trackQueryResults;
        private readonly IDictionary<ParameterExpression, Expression> _materializationContextBindings
            = new Dictionary<ParameterExpression, Expression>();

        private static readonly Expression _jsonOptionsExpression = Expression.Constant(CreateOptions());

        private static JsonSerializerOptions CreateOptions()
        {
            return new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                Converters = {
                    new JsonStringEnumConverter()
                }
            };
        }

        protected AirtableProjectionBindingRemovingVisitorBase(
            ParameterExpression recordParameter,
            bool trackQueryResults)
        {
            _recordParameter = recordParameter;
            _trackQueryResults = trackQueryResults;
        }

        protected override Expression VisitBinary(BinaryExpression binaryExpression)
        {
            if (binaryExpression.NodeType == ExpressionType.Assign)
            {
                if (binaryExpression.Left is ParameterExpression parameterExpression)
                {
                    if (parameterExpression.Type == typeof(AirtableRecord))
                    {
                        var projectionExpression = ((UnaryExpression)binaryExpression.Right).Operand;
                        if (projectionExpression is ProjectionBindingExpression projectionBindingExpression)
                        {
                            var projection = GetProjection(projectionBindingExpression);
                            projectionExpression = projection.Expression;
                        }
                        else if (projectionExpression is UnaryExpression convertExpression
                                 && convertExpression.NodeType == ExpressionType.Convert)
                        {
                            // Unwrap EntityProjectionExpression when the root entity is not projected
                            projectionExpression = ((UnaryExpression)convertExpression.Operand).Operand;
                        }

                        if (projectionExpression is EntityProjectionExpression entityProjectionExpression)
                        {
                            if (entityProjectionExpression.AccessExpression is RootReferenceExpression)
                            {
                                projectionExpression = _recordParameter;
                            }
                        }

                        return Expression.MakeBinary(ExpressionType.Assign, binaryExpression.Left, projectionExpression);
                    }

                    if (parameterExpression.Type == typeof(MaterializationContext))
                    {
                        var newExpression = (NewExpression)binaryExpression.Right;

                        EntityProjectionExpression entityProjectionExpression;
                        if (newExpression.Arguments[0] is ProjectionBindingExpression projectionBindingExpression)
                        {
                            var projection = GetProjection(projectionBindingExpression);
                            entityProjectionExpression = (EntityProjectionExpression)projection.Expression;
                        }
                        else
                        {
                            var projection = ((UnaryExpression)((UnaryExpression)newExpression.Arguments[0]).Operand).Operand;
                            entityProjectionExpression = (EntityProjectionExpression)projection;
                        }

                        _materializationContextBindings[parameterExpression] = entityProjectionExpression.AccessExpression;

                        var updatedExpression = Expression.New(
                            newExpression.Constructor ?? throw new InvalidOperationException("Expression has no constructor"),
                            Expression.Constant(ValueBuffer.Empty),
                            newExpression.Arguments[1]);

                        return Expression.MakeBinary(ExpressionType.Assign, binaryExpression.Left, updatedExpression);
                    }
                }
            }
            return base.VisitBinary(binaryExpression);
        }

        protected override Expression VisitExtension(Expression node)
        {
            if (node is ProjectionBindingExpression projectionBindingExpression)
            {
                var projection = GetProjection(projectionBindingExpression);

                if (projection.Expression is TablePropertyReferenceExpression tableProperty)
                {
                    return CreateGetValueExpression(
                        _recordParameter,
                        tableProperty.Name,
                        projectionBindingExpression.Type);
                }
                else if (projection.Expression is RecordIdPropertyReferenceExpression)
                {
                    return CreateGetRecordIdExpression();
                }

                throw new InvalidOperationException();
            }

            return base.VisitExtension(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
        {
            var method = methodCallExpression.Method;
            var genericMethod = method.IsGenericMethod ? method.GetGenericMethodDefinition() : null;
            if (genericMethod == Microsoft.EntityFrameworkCore.Infrastructure.ExpressionExtensions.ValueBufferTryReadValueMethod)
            {
                var property = methodCallExpression.Arguments[2].GetConstantValue<IProperty>();
                Expression innerExpression;
                if (methodCallExpression.Arguments[0] is ProjectionBindingExpression projectionBindingExpression)
                {
                    var projection = GetProjection(projectionBindingExpression);

                    innerExpression = Expression.Convert(
                        CreateReadRecordExpression(_recordParameter, projection.Alias),
                        typeof(AirtableRecord));
                }
                else
                {
                    innerExpression = _materializationContextBindings[
                        (ParameterExpression)((MethodCallExpression)methodCallExpression.Arguments[0]).Object];
                }

                return CreateGetValueExpression(innerExpression, property);
            }

            return base.VisitMethodCall(methodCallExpression);
        }

        private Expression CreateGetValueExpression(Expression innerExpression, string name, Type type)
        {
            var resultVariable = Expression.Variable(type);
            var jsonElementObj = Expression.Variable(typeof(object));
            var fields = Expression.Variable(typeof(Dictionary<string, object>));
            var isArray = type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);
            var readMethod = isArray ? _visitorReadRawMethod : _visitorReadSingleValueMethod;

            var block = new Expression[]
            {
                Expression.Assign(
                    fields,
                    Expression.Property(
                        _recordParameter,
                        nameof(AirtableRecord.Fields))),
                Expression.IfThenElse(
                    Expression.Call(
                        fields,
                        _dictionaryTryGetValueMethod,
                        Expression.Constant(name),
                        jsonElementObj
                        ),
                    Expression.Assign(
                        resultVariable,
                        Expression.Call(
                            readMethod.MakeGenericMethod(type),
                            Expression.Convert(
                                jsonElementObj,
                                typeof(JsonElement)),
                            _jsonOptionsExpression)),
                    Expression.Assign(resultVariable, Expression.Default(type))),

                resultVariable
            };

            return Expression.Block(
                type,
                new[]
                {
                    jsonElementObj,
                    fields,
                    resultVariable
                },
                block);
        }

        private Expression CreateGetValueExpression(Expression innerExpression, IProperty property)
        {
            if (property.IsPrimaryKey())
            {
                return CreateGetRecordIdExpression();
            }

            return CreateGetValueExpression(innerExpression, property.GetColumnName() ?? property.Name, property.ClrType);
        }

        private Expression CreateGetRecordIdExpression() => Expression.Property(_recordParameter, nameof(AirtableRecord.Id));

        [return: MaybeNull]
        private static T ReadRaw<T>(JsonElement jsonElement, JsonSerializerOptions jsonSerializerOptions)
        {
            return jsonElement.Deserialize<T>(jsonSerializerOptions);
        }

        [return: MaybeNull]
        private static T ReadSingleValue<T>(JsonElement jsonElement, JsonSerializerOptions jsonSerializerOptions)
        {
            if (jsonElement.ValueKind == JsonValueKind.Array)
            {
                return jsonElement.EnumerateArray().FirstOrDefault().Deserialize<T>(jsonSerializerOptions);
            }

            return jsonElement.Deserialize<T>(jsonSerializerOptions);
        }

        private Expression CreateReadRecordExpression(ParameterExpression recordParameter, string alias)
        {
            throw new NotImplementedException();
        }

        protected abstract ProjectionExpression? GetProjection(ProjectionBindingExpression projectionBindingExpression);

    }

    private sealed class AirtableProjecttionBindingRemovingVisitor : AirtableProjectionBindingRemovingVisitorBase
    {
        private readonly SelectExpression _selectExpression;

        public AirtableProjecttionBindingRemovingVisitor(
            SelectExpression selectExpression,
            ParameterExpression recordParameter,
            bool trackQueryResults) : base(recordParameter, trackQueryResults)
        {
            _selectExpression = selectExpression;
        }

        protected override ProjectionExpression GetProjection(ProjectionBindingExpression projectionBindingExpression)
        => _selectExpression.Projection[GetProjectionIndex(projectionBindingExpression)];

        private int GetProjectionIndex(ProjectionBindingExpression projectionBindingExpression)
            => projectionBindingExpression.ProjectionMember != null
                ? _selectExpression.GetMappedProjection(projectionBindingExpression.ProjectionMember).GetConstantValue<int>()
                : (projectionBindingExpression.Index
                    ?? throw new InvalidOperationException(CoreStrings.TranslationFailed(projectionBindingExpression.Print())));
    }

    private sealed class AirtableRecordInjectingExpressionVisitor : ExpressionVisitor
    {
        private int _currentEntityIndex;

        [return: NotNullIfNotNull("node")]
        public override Expression? Visit(Expression? node)
        {
            if (node is EntityShaperExpression shaperExpression)
            {
                _currentEntityIndex++;

                var valueBufferExpression = shaperExpression.ValueBufferExpression;

                var recordVariable = Expression.Variable(
                    typeof(AirtableRecord),
                    "record" + _currentEntityIndex);
                var variables = new List<ParameterExpression> { recordVariable };

                var expressions = new List<Expression>
                    {
                        Expression.Assign(
                            recordVariable,
                            Expression.TypeAs(
                                valueBufferExpression,
                                typeof(AirtableRecord))),
                        Expression.Condition(
                            Expression.Equal(recordVariable, Expression.Constant(null, recordVariable.Type)),
                            Expression.Constant(null, shaperExpression.Type),
                            shaperExpression)
                    };

                return Expression.Block(
                    shaperExpression.Type,
                    variables,
                    expressions);
            }

            return base.Visit(node);
        }
    }

    public AirtableShapedQueryCompilingExpressionVisitor(
        ShapedQueryCompilingExpressionVisitorDependencies dependencies,
        QueryCompilationContext queryCompilationContext)
        : base(dependencies, queryCompilationContext)
    {
    }

    protected override Expression VisitShapedQuery(ShapedQueryExpression shapedQueryExpression)
    {
        if (shapedQueryExpression.QueryExpression is SelectExpression selectExpression)
        {
            selectExpression.ApplyProjection();

            var recordParameter = Expression.Parameter(typeof(AirtableRecord), "record");

            var shaper = shapedQueryExpression.ShaperExpression;

            shaper = new AirtableRecordInjectingExpressionVisitor().Visit(shaper);
            shaper = InjectEntityMaterializers(shaper);
            shaper = new AirtableProjecttionBindingRemovingVisitor(
                selectExpression,
                recordParameter,
                QueryCompilationContext.QueryTrackingBehavior == QueryTrackingBehavior.TrackAll)
                    .Visit(shaper);

            var shaperLambda = Expression.Lambda(
                shaper,
                QueryCompilationContext.QueryContextParameter,
                recordParameter);

            return Expression.New(
                typeof(QueryingEnumerable<>).MakeGenericType(shaperLambda.ReturnType).GetConstructors().First(),
                Expression.Convert(QueryCompilationContext.QueryContextParameter, typeof(AirtableQueryContext)),
                Expression.Constant(selectExpression),
                Expression.Constant(shaperLambda.Compile()),
                Expression.Constant(
                        QueryCompilationContext.QueryTrackingBehavior == QueryTrackingBehavior.NoTrackingWithIdentityResolution)
                );
        }

        return shapedQueryExpression.QueryExpression;
    }

    private sealed class QueryingEnumerable<T> : IAsyncEnumerable<T>
    {
        private readonly AirtableQueryContext _airtableQueryContext;
        private readonly SelectExpression _selectExpression;
        private readonly FormulaGenerator _formulaGenerator;
        private readonly Func<AirtableQueryContext, AirtableRecord, T> _shaper;
        private readonly bool _standalone;
        private readonly IAirtableClient _base;

        public QueryingEnumerable(
            AirtableQueryContext airtableQueryContext,
            SelectExpression selectExpression,
            Func<AirtableQueryContext, AirtableRecord, T> shaper,
            bool standalone)
        {
            _airtableQueryContext = airtableQueryContext;
            _selectExpression = selectExpression;
            _formulaGenerator = new FormulaGenerator(airtableQueryContext.ParameterValues);
            _shaper = shaper;
            _standalone = standalone;
            _base = _airtableQueryContext.AirtableClient;
        }

        public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            _airtableQueryContext.InitializeStateManager(_standalone);

            var formulaExpr = _selectExpression.FilterByFormula;

            string? singleRecordId = null;
            string? formula = null;

            if (formulaExpr is not null)
            {
                //User expects that if record is not in the view it should not be selected
                //So recordId optimization will not work for the views, as it will always return record
                if (_selectExpression.View is null)
                {
                    singleRecordId = _formulaGenerator.TryExtractSingleRecordId(formulaExpr);

                    //This optimizes call to single entity retrival operation
                    //This is also used by ReloadAsync call
                    if (singleRecordId is not null)
                    {
                        var record = await _base.GetRecord(_selectExpression.Table, singleRecordId);

                        if (record is null)
                            throw new InvalidOperationException("Airtable response is null");

                        if (!record.Success)
                            throw new InvalidOperationException("Airtable error", record.AirtableApiError);

                        yield return _shaper(_airtableQueryContext, record.Record);
                        yield break;
                    }
                }

                formula = _formulaGenerator.GetFormula(formulaExpr);
            }

            var limit =
                _selectExpression.Limit switch
                {
                    null => default(int?),
                    ParameterExpression param => Convert.ToInt32(_airtableQueryContext.ParameterValues[param.Name!]),
                    ConstantExpression constant => constant.GetConstantValue<int>(),
                    _ => throw new InvalidOperationException("Failed to convert limit expression")
                };

            var returned = 0;
            AirtableListRecordsResponse? response = null;

            do
            {
                var toGet = limit == null
                    ? null
                    : (limit - returned);

                if (toGet == 0) yield break;

                response = await _base.ListRecords(
                    _selectExpression.Table,
                    fields: _selectExpression.GetFields(),
                    maxRecords: toGet,
                    filterByFormula: formula,
                    view: _selectExpression.View,
                    offset: response?.Offset);

                if (response is null)
                    throw new InvalidOperationException("Airtable response is null");

                if (!response.Success)
                    throw new InvalidOperationException("Airtable error", response.AirtableApiError);

                foreach (var item in response.Records)
                {
                    yield return _shaper(_airtableQueryContext, item);
                    returned++;
                }
            }
            while (response.Offset != null);
        }
    }
}
