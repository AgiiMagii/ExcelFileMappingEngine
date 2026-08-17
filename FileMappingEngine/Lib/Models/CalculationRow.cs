using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileMappingEngine.Lib.Models
{
    public class CalculationRow
    {
        public List<CalculationCell> Cells { get; set; } = new();
    }
}
