using System;
using System.Collections.Generic;
using System.Text;
using static FileMappingEngine.Lib.Models.Enums;

namespace FileMappingEngine.Lib.Models
{
    public class ColumnReference
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";

        public int Index { get; set; } = -1;
        public string? ExcelLetter { get; set; }

        public ColumnFormat Format { get; set; } = ColumnFormat.Text;
        public Type DataType { get; set; } = typeof(string);
    }
}
