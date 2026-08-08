using FileMappingEngine.Lib.Models;
using FileMappingEngine.Lib.Sessions;
using System;
using System.Collections.Generic;
using System.Text;
using static FileMappingEngine.Lib.Models.Enums;

namespace FileMappingEngine.Lib.Interfaces
{
    public interface IWorkbookActionExecutor : IMappingActionExecutor
    {
        void AddColumn(DataState dataState, ColumnDirection direction, string anchorId, string? newName);
        void MergeColumns(DataSession session, ColumnReference first, ColumnReference second, string separator, string? resultColumnName);
        void SetColumnDataType(DataState dataState, string columnName, Type dataType);
    }
}
