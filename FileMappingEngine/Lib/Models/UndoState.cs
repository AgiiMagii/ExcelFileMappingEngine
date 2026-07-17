using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace FileMappingEngine.Lib.Models
{
    public class UndoState
    {
        public DataTable? PreviousData { get; set; }
        public List<ActionStep> PreviousSteps { get; set; } = new();
    }
}
