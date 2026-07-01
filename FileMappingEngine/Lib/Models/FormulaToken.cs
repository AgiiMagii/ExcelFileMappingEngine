
using static FileMappingEngine.Lib.Models.Enums;

namespace FileMappingEngine.Lib.Models
{
    public class FormulaToken
    {
        public TokenType Type { get; set; }
        public string Value { get; set; } = "";
    }
}
