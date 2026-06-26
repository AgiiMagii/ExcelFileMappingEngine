using System;
using System.Collections.Generic;
using System.Text;

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
            Percentage,
            Date,
            Text
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
    }
}
