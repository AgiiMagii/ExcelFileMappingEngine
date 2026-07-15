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
        public bool IsMappingApplied { get; set; } = false;

        public RawExcelData? RawData { get; set; }

        public DataTable? CurrentData { get; set; }

        public DataTable? PreviousData { get; set; }

        public List<string[]>? IgnoredRows { get; set; }

        public FileDefinition? FileDefinition { get; set; }
    }
}
