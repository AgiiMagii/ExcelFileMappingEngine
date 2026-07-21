
using DocumentFormat.OpenXml.Drawing.Diagrams;
using System.Data;

namespace FileMappingEngine.Lib.Models
{
    public class RawExcelData
    {
        public DataTable? Data { get; set; }

        public List<ColumnReference> Columns { get; set; } = new();

        public List<CellReference> Cells { get; set; } = new();
    }
}
