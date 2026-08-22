using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace FileMappingEngine.Lib.Models
{
    public class DataState
    {
        public int HeaderRowIndex { get; set; } = 1;
        public int CalculationStartRowIndex { get; set; }
        public string? SortedColumn { get; set; }
        public bool? SortAscending { get; set; }
        public bool IsMappingApplied { get; set; } = false;

        public RawExcelData? RawData { get; set; }

        public IXLWorkbook? Workbook { get; set; }

        public DataTable? CurrentData { get; set; }

        public List<CalculationRow> CalculationData { get; set; } = new();

        public Stack<UndoState?> UndoStateHistory { get; set; } = new Stack<UndoState?>();

        public List<string[]>? IgnoredRows { get; set; }

        public FileDefinition? FileDefinition { get; set; }
    }
}
