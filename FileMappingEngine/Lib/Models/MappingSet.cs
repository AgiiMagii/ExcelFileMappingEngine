using System;
using System.Collections.Generic;
using System.Text;

namespace FileMappingEngine.Lib.Models
{
    public class MappingSet
    {
        public string? Name { get; set; }
        public int HeaderRow { get; set; }

        public List<ActionStep> Steps { get; set; } = new();
    }
}
