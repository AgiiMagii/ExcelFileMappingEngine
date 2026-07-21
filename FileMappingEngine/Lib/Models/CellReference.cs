using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace FileMappingEngine.Lib.Models
{
    public class CellReference
    {
        public int RowIndex { get; set; }

        public string ColumnId { get; set; } = "";

        public string? Hyperlink { get; set; }

    }
}
