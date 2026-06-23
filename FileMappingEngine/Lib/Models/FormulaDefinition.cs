using System;
using System.Collections.Generic;
using System.Text;

namespace FileMappingEngine.Lib.Models
{
    public class FormulaDefinition
    {
        public string? FormulaText { get; set; }

        public List<FormulaStep> Steps { get; set; } = new();
    }
}
