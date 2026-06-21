using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace FileMappingEngine.Lib.Models
{
    public class RawExcelData
    {
        public DataTable? Data { get; set; }

        public List<ColumnReference> Columns { get; set; } = new();
    }
}
