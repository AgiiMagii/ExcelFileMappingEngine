using System;
using System.Collections.Generic;
using System.Text;

namespace FileMappingEngine.Lib.Models
{
    public class ColumnReference
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public int CurrentIndex { get; set; } = -1;
    }
}
