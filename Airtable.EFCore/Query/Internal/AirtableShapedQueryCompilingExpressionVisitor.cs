using Airtable.EFCore.Metadata.Conventions;
using AirtableApiClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace Airtable.EFCore.Query.Internal;

internal sealed class AirtableShapedQueryCompilingExpressionVisitor : ShapedQueryCompilingExpressionVisitor
{
    private abstract class AirtableProjecttionBindingRemovingVisitorBase : ExpressionVisitor
    {
        private static readonly MethodInfo _dictionaryTryGetValueMethod = 
            typeof(Dictionary<string, object>)
                .GetMethod(
                    nameof(Dictionary<string, object>.TryGetValue))
                ?? throw new InvalidOperationException("Could not find method TryGetValue");

        private static readonly MethodInfo _visitorReadSingleValueMethod =
            typeof(AirtableProjecttionBindingRemovingVisitorBase)
                .GetMethod(
                    nameof(AirtableProjecttionBindingRemovingVisitorBase.ReadSingleValue),
                    BindingFlags.NonPublic | BindingFlags.Static,
                    new[] { typeof(JsonElement), typeof(JsonSerializerOptions) })
                ?? throw new InvalidOperationException("Could not find method ReadSingleValue");
        private readonly ParameterExpression _recordParameter;
        private readonly bool _trackQueryResults;
        private readonly IDictionary<ParameterExpression, Expression> _materializationContextBindings
            = new Dictionary<ParameterExpression, Expression>();

        protected AirtableProjecttionBindingRemovingVisitorBase(
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

                        if(projectionExpression is EntityProjectionExpression entityProjectionExpression)
                        {
                            if(entityProjectionExpression.AccessExpression is RootReferenceExpression)
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
            if (node is ProjectionBindingExpression)
            {
                return Expression.Convert(_recordParameter, typeof(AirtableRecord));
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

                return CreateGetValueExpression(innerExpression, property, methodCallExpression.Type);
            }

            return base.VisitMethodCall(methodCallExpression);
        }

        private Expression CreateGetValueExpression(Expression innerExpression, IProperty property, Type type)
        {
            if(property.IsPrimaryKey())
            {
                return Expression.Property(_recordParameter, nameof(AirtableRecord.Id));
            }

            var resultVariable = Expression.Variable(property.ClrType);
            var jsonElementObj = Expression.Variable(typeof(object));
            var fields = Expression.Variable(typeof(Dictionary<string, object>));


            var block = new List<Expression>();
            block.Add(
                Expression.Assign(
                    fields,
                    Expression.Property(
                        _recordParameter,
                        nameof(AirtableRecord.Fields))));
            block.Add(
                Expression.IfThenElse(
                    Expression.Call(
                        fields,
                        _dictionaryTryGetValueMethod,
                        Expression.Constant(property.GetColumnName() ?? property.Name),
                        jsonElementObj
                        ),
                    Expression.Assign(
                        resultVariable,
                        Expression.Call(
                            _visitorReadSingleValueMethod.MakeGenericMethod(property.ClrType),
                            Expression.Convert(
                                jsonElementObj, 
                                typeof(JsonElement)),
                            Expression.Default(typeof(JsonSerializerOptions)))),
                    Expression.Assign(resultVariable, Expression.Default(property.ClrType))));

            block.Add(resultVariable);

            return Expression.Block(
                property.ClrType,
                new[]
                {
                    jsonElementObj,
                    fields,
                    resultVariable
                },
                block);
        }

        [return: MaybeNull]
        private static T ReadSingleValue<T>(JsonElement jsonElement, JsonSerializerOptions jsonSerializerOptions)
        {
            if(jsonElement.ValueKind == JsonValueKind.Array)
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

    private sealed class AirtableProjecttionBindingRemovingVisitor : AirtableProjecttionBindingRemovingVisitorBase
    {
        private readonly SelectExpression _selectExpression;

        public AirtableProjecttionBindingRemovingVisitor(
            SelectExpression selectExpression,
            ParameterExpression recordParameter, 
            bool trackQueryResults) : base(recordParameter, trackQueryResults)
        {
            _selectExpression = selectExpression;
        }

        protected override ProjectionExpression? GetProjection(ProjectionBindingExpression projectionBindingExpression)
        {
            return _selectExpression.GetProjection(projectionBindingExpression.ProjectionMember);
        }
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

    private sealed class EntityMaterializerInjectingExpressionVisitor : ExpressionVisitor
    {
        private int _currentEntityIndex;

        protected override Expression VisitExtension(Expression extensionExpression)
            => extensionExpression is EntityShaperExpression entityShaperExpression
                ? ProcessEntityShaper(entityShaperExpression)
                : base.VisitExtension(extensionExpression);

        private Expression ProcessEntityShaper(EntityShaperExpression entityShaperExpression)
        {
            _currentEntityIndex++;
            var entityType = entityShaperExpression.EntityType;
            var primaryKey = entityType.FindPrimaryKey();

            if (primaryKey != null)
            {
                return Materialize(entityShaperExpression);
            }

            throw null;
        }

        private Expression Materialize(
            EntityShaperExpression entityShaperExpression
            )
        {
            var entityType = entityShaperExpression.EntityType;
            var returnType = entityType.ClrType;
            var expressions = new List<Expression>();

            return
                Expression.New(
                    returnType.GetConstructor(Type.EmptyTypes) ?? throw new InvalidOperationException($"Could not find parameterless constructor for {returnType.FullName}"));

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
                Expression.Constant(new FormulaGenerator()),
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
        private readonly AirtableBase _base;

        public QueryingEnumerable(
            AirtableQueryContext airtableQueryContext,
            SelectExpression selectExpression,
            FormulaGenerator formulaGenerator,
            Func<AirtableQueryContext, AirtableRecord, T> shaper,
            bool standalone)
        {
            _airtableQueryContext = airtableQueryContext;
            _selectExpression = selectExpression;
            _formulaGenerator = formulaGenerator;
            _shaper = shaper;
            _standalone = standalone;
            _base = _airtableQueryContext.AirtableClient;
        }

        public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            AirtableListRecordsResponse? response = null;
            _airtableQueryContext.InitializeStateManager(_standalone);

            string? formula = null;

            if(_selectExpression.FilterByFormula != null)
            {
                formula = _formulaGenerator.GetFormula(_selectExpression.FilterByFormula);
            }

            do
            {
                response = await _base.ListRecords(
                    _selectExpression.Table,
                    fields: _selectExpression.SelectProperties,
                    maxRecords: _selectExpression.Limit,
                    filterByFormula: formula,
                    offset: response?.Offset);

                if (!response.Success)
                    throw new InvalidOperationException("Airtable error", response.AirtableApiError);

                foreach (var item in response.Records)
                {
                    yield return _shaper(_airtableQueryContext, item);
                }
            }
            while (response.Offset != null);
        }
    }
}
