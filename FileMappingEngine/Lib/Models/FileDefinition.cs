using System;
using System.Collections.Generic;
using System.Text;

namespace FileMappingEngine.Lib.Models
{
    public class FileDefinition
    {
        public long? Id { get; set; }
        public string? Name { get; set; }

        public List<ColumnData>? Columns { get; set; } = new();
        public List<MappingSet>? MappingSets { get; set; } = new();
    }
}
