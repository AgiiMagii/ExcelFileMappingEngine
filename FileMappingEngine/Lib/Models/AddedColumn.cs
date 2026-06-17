using System;
using System.Collections.Generic;
using System.Text;

namespace FileMappingEngine.Lib.Models
{
    public class AddedColumn
    {
        public string? HeaderName { get; set; }
        public string? AnchorColumnId { get; set; }
        public string? Direction { get; set; }
        public Type? DataType { get; set; }
        public string? DefaultValue { get; set; }
        public string? Formula { get; set; }
    }
}
