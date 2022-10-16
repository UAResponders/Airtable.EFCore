using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace Airtable.EFCore.Query.Internal;

internal interface IFormulaExpressionFactory
{
    SelectExpression Select(IEntityType entityType);

    FormulaBinaryExpression MakeBinary(
        ExpressionType operatorType,
        FormulaExpression left,
        FormulaExpression right,
        CoreTypeMapping? typeMapping);

    TablePropertyReferenceExpression MakeTablePropertyReference(IProperty property);

    FormulaConstantExpression MakeConstant(object? value);

    FormulaExpression MakeAnd(params FormulaExpression[] expressions);
}
