
namespace FileMappingEngine.Lib.Models
{
    public class ActionStep
    {
        public int Order { get; set; }
        public string? ActionType { get; set; }
        public string? ColumnId { get; set; }

        public Dictionary<string, object>? Parameters { get; set; }
    }
}
