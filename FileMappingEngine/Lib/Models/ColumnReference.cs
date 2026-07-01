
namespace FileMappingEngine.Lib.Models
{
    public class ColumnReference
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";

        public int Index { get; set; } = -1;
        public string? ExcelLetter { get; set; }
    }
}
