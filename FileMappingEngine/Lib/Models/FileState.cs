using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace FileMappingEngine.Lib.Models
{
    public class FileState
    {
        public string? FilePath { get; set; }
        public string? FileName { get; set; }

        public int HeaderRowIndex { get; set; } = 1;

        public DataTable? RawData { get; set; }

        public DataTable? CurrentData { get; set; }

        public DataTable? PreviousData { get; set; }

        public MappingSet? CurrentMapping { get; set; }

        public List<string[]>? IgnoredRows { get; set; }
    }
}

