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
            Trim
        }
        public enum FormulaStepType
        {
            Column,
            Constant,
            Operator,
            Function,
            ParenthesisOpen,
            ParenthesisClose
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
    }
}
