using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace FileMappingEngine.Lib.Models
{
    public class DataState
    {
        public int HeaderRowIndex { get; set; } = 1;
        public string? SortedColumn { get; set; }
        public bool? SortAscending { get; set; }

        public RawExcelData? RawData { get; set; }

        public DataTable? CurrentData { get; set; }

        public DataTable? PreviousData { get; set; }

        public MappingSet? CurrentMapping { get; set; }

        public List<string[]>? IgnoredRows { get; set; }

        public List<ColumnReference> Columns { get; set; } = new();
    }
}
