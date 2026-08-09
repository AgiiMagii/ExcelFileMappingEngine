using FileMappingEngine.Lib.Models;
using FileMappingEngine.Lib.Sessions;
using System;
using System.Collections.Generic;
using System.Text;
using static FileMappingEngine.Lib.Models.Enums;

namespace FileMappingEngine.Lib.Interfaces
{
    public interface IDataTableActionExecutor : IMappingActionExecutor
    {
        string AddColumn(DataState dataState, ColumnDirection direction, string anchorId, string? newName);
        string MergeColumns(DataSession session, ColumnReference first, ColumnReference second, string separator, string? resultColumnName);
        void ApplyFormula(DataState dataState, string targetColumn, FormulaNode formulaTree);
    }
}
