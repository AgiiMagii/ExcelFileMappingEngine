using ClosedXML.Excel;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using FileMappingEngine.Lib.Helpers;
using FileMappingEngine.Lib.Interfaces;
using FileMappingEngine.Lib.Models;
using FileMappingEngine.Lib.Sessions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows;
using static FileMappingEngine.Lib.Models.Enums;

namespace FileMappingEngine.Lib.Services
{
    public class DataService
    {
        public void ResetTable(DataState dataState)
        {
            if (dataState == null)
                throw new InvalidOperationException("No file loaded.");

            ExcelHelper.BuildCurrentData(dataState);
        }

        public void UndoLastAction(DataSession session)
        {
            if (session.Data == null)
                throw new InvalidOperationException("No file loaded.");
            if (session.Data.UndoStateHistory == null || session.Data.UndoStateHistory.Count == 0)
                throw new InvalidOperationException("No previous state available.");
            UndoLastStep(session);
            var previousState = session.Data.UndoStateHistory.Pop();
            if (previousState != null)
            {
                session.Data.CurrentData = previousState.PreviousData?.Copy();
                session.Data.SortAscending = previousState.PreviousSortAscending;
                session.Data.SortedColumn = previousState.PreviousSortedColumn;
            }
        }

        public void UpdateHeaderRow(DataState dataState, int newHeaderRow)
        {
            if (dataState == null)
                throw new InvalidOperationException("No file loaded.");

            if (dataState.RawData?.Data == null)
                throw new InvalidOperationException("Raw data not loaded.");

            dataState.HeaderRowIndex = newHeaderRow;
            ExcelHelper.BuildCurrentData(dataState);

            if (dataState.FileDefinition == null || dataState.FileDefinition.Columns == null)
                throw new InvalidOperationException("File definition is not loaded.");

            dataState.FileDefinition.Hash = DataHelper.CreateHash(dataState.FileDefinition.Columns);
        }

        public void RemoveColumn(DataSession session, string columnName, IDataTableActionExecutor actionExecutor)
        {
            if (session.Data == null)
                throw new InvalidOperationException("No data loaded.");
            SavePreviousState(session);

            actionExecutor.RemoveColumn(session.Data, columnName);

            session.MappingSet.Steps.Add(new ActionStep
            {
                ActionType = "DeleteColumn",
                ColumnId = columnName,
                Order = session.MappingSet.Steps.Count + 1
            });
        }
        
        public void RemoveColumns(DataSession session, IEnumerable<string> columnNames, IDataTableActionExecutor actionExecutor)
        {
            if (session.Data == null)
                throw new InvalidOperationException("No data loaded.");

            var columns = columnNames.ToList();

            SavePreviousState(session);

            actionExecutor.RemoveColumns(session.Data, columns);

            session.MappingSet.Steps.Add(new ActionStep
            {
                ActionType = "DeleteColumns",
                Order = session.MappingSet.Steps.Count + 1,

                Parameters = new Dictionary<string, object>
                {
                    ["ColumnIds"] = columns
                }
            });
        }

        public void AddColumn(DataSession session, ColumnDirection direction, string anchorId, string? newName, IDataTableActionExecutor actionExecutor)
        {
            if (session.Data == null)
                throw new InvalidOperationException("No data loaded.");

            SavePreviousState(session);

            string newColumnName = actionExecutor.AddColumn(session.Data, direction, anchorId, newName);

            session.MappingSet.Steps.Add(new ActionStep
            {
                ActionType = "AddColumn",
                ColumnId = newColumnName,
                Order = session.MappingSet.Steps.Count + 1,
                Parameters = new Dictionary<string, object>
                {
                    ["AnchorColumnId"] = anchorId,
                    ["Direction"] = direction
                }
            });
        }

        public void RenameColumn(DataSession session, string oldName, string newName, IDataTableActionExecutor actionExecutor)
        {
            if (session.Data == null)
                throw new InvalidOperationException("No data loaded.");

            SavePreviousState(session);

            actionExecutor.RenameColumn(session.Data, oldName, newName);

            session.MappingSet.Steps.Add(new ActionStep
            {
                ActionType = "RenameColumn",
                ColumnId = oldName,
                Order = session.MappingSet.Steps.Count + 1,
                Parameters = new Dictionary<string, object>
                {
                    ["NewName"] = newName
                }
            });

        }

        public void MergeColumns(DataSession session, ColumnReference first, ColumnReference second, string separator, string? resultColumnName, IDataTableActionExecutor actionExecutor)
        {
            SavePreviousState(session);

            string targetColumn = actionExecutor.MergeColumns(session, first, second, separator, resultColumnName);

            session.MappingSet.Steps.Add(new ActionStep
            {
                ActionType = "MergeColumns",
                ColumnId = first.Name,
                Order = session.MappingSet.Steps.Count + 1,
                Parameters = new Dictionary<string, object>
                {
                    ["SecondColumnId"] = second.Name,
                    ["Separator"] = separator,
                    ["NewName"] = targetColumn
                }
            });
        }

        public void SortData(DataSession session, string columnName, bool ascending, IDataTableActionExecutor actionExecutor)
        {
            if (session.Data == null)
                throw new InvalidOperationException("No file loaded.");
            SavePreviousState(session);

            actionExecutor.SortData(session.Data, columnName, ascending);

            session.Data.SortedColumn = columnName;
            session.Data.SortAscending = ascending;
        }

        public void SetColumnDataType(DataSession session, string columnName, Type dataType)
        {
            if (session.Data == null)
                throw new InvalidOperationException("No file loaded.");
            if (session.Data.CurrentData == null)
                throw new InvalidOperationException("Current data not available.");
            if (!session.Data.CurrentData.Columns.Contains(columnName))
                throw new ArgumentException($"Column '{columnName}' does not exist.");

            SavePreviousState(session);

            //SetColumnDataTypeCore(session.Data, columnName, dataType);

            session.MappingSet.Steps.Add(new ActionStep
            {
                ActionType = "SetColumnDataType",
                ColumnId = columnName,
                Order = session.MappingSet.Steps.Count + 1,
                Parameters = new Dictionary<string, object>
                {
                    ["DataType"] = dataType.FullName ?? string.Empty,
                }
            });
        }

        private void SavePreviousState(DataSession session)
        {
            if (session.Data == null)
                throw new InvalidOperationException("No data loaded.");
            if (session.Data.CurrentData == null)
                throw new InvalidOperationException("No current data loaded.");

            session.Data.UndoStateHistory.Push(new UndoState
            {
                PreviousData = session.Data.CurrentData.Copy(),
                PreviousSteps = CloneSteps(session.MappingSet.Steps),
                PreviousSortAscending = session.Data.SortAscending,
                PreviousSortedColumn = session.Data.SortedColumn
            });
        }

        private List<ActionStep> CloneSteps(List<ActionStep> steps)
        {
            return steps.Select(step => new ActionStep
            {
                Order = step.Order,
                ActionType = step.ActionType,
                ColumnId = step.ColumnId,
                Parameters = step.Parameters != null
                    ? new Dictionary<string, object>(step.Parameters)
                    : null
            }).ToList();
        }

        public void ClearSteps(DataSession session)
        {
            session.MappingSet.Steps.Clear();
        }

        public void UndoLastStep(DataSession session)
        {
            if (session.MappingSet.Steps.Count > 0)
            {
                session.MappingSet.Steps.RemoveAt(session.MappingSet.Steps.Count - 1);
            }
        }

        public bool IsColumnNameTaken(DataState dataState, string columnName)
        {
            if (dataState.CurrentData == null)
                throw new InvalidOperationException("No current data loaded.");

            return dataState.CurrentData.Columns.Contains(columnName);
        }

        public void ApplyFormulaToColumn(DataSession session, string columnName, string formula, IDataTableActionExecutor actionExecutor)
        {
            if (session.Data == null || session.Data.CurrentData == null)
                throw new InvalidOperationException("No data loaded.");

            if (!session.Data.CurrentData.Columns.Contains(columnName))
                throw new ArgumentException($"Column '{columnName}' does not exist.");

            SavePreviousState(session);

            bool hasData = session.Data.CurrentData.Rows
            .Cast<DataRow>()
            .Any(row => !row.IsNull(columnName));

            if (hasData)
            {
                actionExecutor.ApplyFormulaToColumn(session.Data, columnName, formula);
            }

            session.MappingSet.Steps.Add(new ActionStep
            {
                ActionType = "Formula",
                ColumnId = columnName,
                Order = session.MappingSet.Steps.Count + 1,
                Parameters = new Dictionary<string, object>
                {
                    ["Formula"] = formula
                }
            });
        }

        public void SetIsAppliedMappingFalse(DataState dataState)
        {
            if (dataState == null)
                throw new InvalidOperationException("No file loaded.");

            dataState.IsMappingApplied = false;
        }

        public void SaveCalculationRowData(DataSession session, int rowIndex, string columnName, string value, IDataTableActionExecutor actionExecutor)
        {
            SavePreviousState(session);

            actionExecutor.ApplyCalculationRowData(session, rowIndex, columnName, value);

            session.MappingSet.Steps.Add(new ActionStep
            {
                ActionType = "CalculationRowData",
                Order = session.MappingSet.Steps.Count + 1,
                Parameters = new Dictionary<string, object>
                {
                    ["ColumnName"] = columnName,
                    ["RowIndex"] = rowIndex,
                    ["Value"] = value,
                }
            });
        }

        public List<CalculationRow> GetCalculationCellData(DataSession session)
        {
            if (session.Data == null || session.Data.CurrentData == null)
                throw new InvalidOperationException("No data loaded.");

            var rows = session.Data.CalculationData;

            return rows;
        }
    }
}
