using System;
using System.Collections.Generic;
using System.Text;
using static FileMappingEngine.Lib.Models.Enums;

namespace FileMappingEngine.Lib.Models
{
    public class FormulaStep
    {
        public FormulaStepType StepType { get; set; }

        public ColumnReference? SelectedColumn { get; set; }

        public string? Value { get; set; }

        public MathOperator? Operator { get; set; }

        public FormulaFunction? Function { get; set; }
    }
}
