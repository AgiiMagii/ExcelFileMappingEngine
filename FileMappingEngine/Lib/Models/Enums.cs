
namespace FileMappingEngine.Lib.Models
{
    public class Enums
    {
        public enum MathOperator
        {
            Add,
            Subtract,
            Multiply,
            Divide
        }
        public enum FormulaFunction
        {
            Round,
            Concat,
            Upper,
            Lower,
            Trim,
            Sum,
            Average,
            Min,
            Max,
        }
        public enum ColumnFormat
        {
            General,
            Number,
            Currency,
            Accounting,
            Date,
            DateShort,
            DateLong,
            DateTime,
            Time,
            Percentage,
            Fraction,
            Scientific,
            Text,
            Special,
            Custom
        }
        public enum TokenType
        {
            Number,
            Column,
            Operator,
            Function,
            OpenParenthesis,
            CloseParenthesis,
            Comma
        }
        public enum FormulaNodeType
        {
            Constant,
            Column,
            Operator,
            Function
        }
        public enum DataType
        {
            Text,
            Number,
            Date,
            Boolean
        }
    }
}
