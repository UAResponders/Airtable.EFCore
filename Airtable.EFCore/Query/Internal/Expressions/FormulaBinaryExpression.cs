using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace Airtable.EFCore.Query.Internal;

internal sealed class FormulaBinaryExpression : FormulaExpression, IEquatable<FormulaBinaryExpression?>
{
    private static readonly ISet<ExpressionType> _allowedOperators = new HashSet<ExpressionType>
    {
        ExpressionType.Add,
        ExpressionType.Subtract,
        ExpressionType.Multiply,
        ExpressionType.Divide,
        ExpressionType.LessThan,
        ExpressionType.LessThanOrEqual,
        ExpressionType.GreaterThan,
        ExpressionType.GreaterThanOrEqual,
        ExpressionType.Equal,
        ExpressionType.NotEqual,
    };

    private static ExpressionType VerifyOperator(ExpressionType operatorType)
        => _allowedOperators.Contains(operatorType)
            ? operatorType
            : throw new InvalidOperationException($"Unsupported operation {operatorType}");

    public ExpressionType OperatorType { get; }
    public FormulaExpression Left { get; }
    public FormulaExpression Right { get; }

    public FormulaBinaryExpression(
        ExpressionType operatorType,
        FormulaExpression left,
        FormulaExpression right,
        Type type)
        : base(type)
    {
        OperatorType = VerifyOperator(operatorType);

        Left = left;
        Right = right;
    }

    protected override Expression VisitChildren(ExpressionVisitor visitor)
    {
        var left = (FormulaExpression)visitor.Visit(Left);
        var right = (FormulaExpression)visitor.Visit(Right);

        return Update(left, right);
    }

    public FormulaExpression Update(FormulaExpression left, FormulaExpression right)
        => left != Left || right != Right
            ? new FormulaBinaryExpression(OperatorType, left, right, Type)
            : this;

    protected override void Print(ExpressionPrinter expressionPrinter)
    {
        var requiresBrackets = RequiresBrackets(Left);

        if (requiresBrackets)
        {
            expressionPrinter.Append("(");
        }

        expressionPrinter.Visit(Left);

        if (requiresBrackets)
        {
            expressionPrinter.Append(")");
        }

        expressionPrinter.Append(expressionPrinter.GenerateBinaryOperator(OperatorType));

        requiresBrackets = RequiresBrackets(Right);

        if (requiresBrackets)
        {
            expressionPrinter.Append("(");
        }

        expressionPrinter.Visit(Right);

        if (requiresBrackets)
        {
            expressionPrinter.Append(")");
        }

        static bool RequiresBrackets(FormulaExpression expression)
            => expression is FormulaBinaryExpression;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as FormulaBinaryExpression);
    }

    public bool Equals(FormulaBinaryExpression? other)
    {
        return other != null &&
               base.Equals(other) &&
               OperatorType == other.OperatorType &&
               EqualityComparer<FormulaExpression>.Default.Equals(Left, other.Left) &&
               EqualityComparer<FormulaExpression>.Default.Equals(Right, other.Right);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), OperatorType, Left, Right);
    }
}
