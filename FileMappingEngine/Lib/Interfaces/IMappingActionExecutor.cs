using FileMappingEngine.Lib.Models;
using FileMappingEngine.Lib.Sessions;
using System;
using System.Collections.Generic;
using System.Text;
using static FileMappingEngine.Lib.Models.Enums;


namespace FileMappingEngine.Lib.Interfaces
{
    public interface IMappingActionExecutor
    {
        void RemoveColumn(DataState dataState, string columnName);
        void RemoveColumns(DataState dataState, IEnumerable<string> columnNames);
        string AddColumn(DataState dataState, ColumnDirection direction, string anchorId, string? newName);
        void RenameColumn(DataState dataState, string oldName, string newName);
        string MergeColumns(DataSession session, ColumnReference first, ColumnReference second, string separator, string? resultColumnName);
        void SortData(DataState dataState, string columnName, bool ascending);
        void ApplyFormulaToColumn(DataState dataState, string columnName, string formula);
        void ApplyFormula(DataState dataState, string targetColumn, FormulaNode formulaTree);
    }
}
