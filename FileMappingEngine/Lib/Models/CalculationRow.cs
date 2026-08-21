using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileMappingEngine.Lib.Models
{
    public class CalculationRow
    {
        public List<CalculationsCell> Cells { get; set; } = new();
    }
}
