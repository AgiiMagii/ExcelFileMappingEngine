using FileMappingEngine.Lib.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace FileMappingEngine.Lib
{
    public class FileTemplate
    {
        public int DisplayIndex { get; set; }
        public string? TemplateName { get; set; }
        public int? HeaderRowIndex { get; set; }
        public List<string>? RemovedColumns { get; set; }
        public List<AddedColumn>? AddedColumns { get; set; }
        public List<(string ColumnName, ListSortDirection Direction)>? OrderBy { get; set; }
        public Dictionary<string, string>? ColumnMappings { get; set; }
    }
}
