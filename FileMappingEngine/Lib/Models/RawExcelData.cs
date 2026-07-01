
using System.Data;

namespace FileMappingEngine.Lib.Models
{
    public class RawExcelData
    {
        public DataTable? Data { get; set; }

        public List<ColumnReference> Columns { get; set; } = new();
    }
}
