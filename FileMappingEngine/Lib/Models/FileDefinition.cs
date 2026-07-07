using System;
using System.Collections.Generic;
using System.Text;

namespace FileMappingEngine.Lib.Models
{
    public class FileDefinition
    {
        public long? Id { get; set; }
        public string? Fingerprint { get; set; }
        public List<ColumnReference> Columns { get; set; } = new();
    }
}
