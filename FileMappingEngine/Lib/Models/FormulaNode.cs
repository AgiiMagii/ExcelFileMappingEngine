
using static FileMappingEngine.Lib.Models.Enums;

namespace FileMappingEngine.Lib.Models
{
    public class FormulaNode
    {
        public FormulaNodeType Type { get; set; }

        public string? Value { get; set; }

        public MathOperator? Operator { get; set; }

        public FormulaFunction? Function { get; set; }


        public FormulaNode? Left { get; set; }

        public FormulaNode? Right { get; set; }


        public List<FormulaNode> Arguments { get; set; } = new();
    }
}
