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

        //public void RemoveColumnCore(DataState dataState, string columnName)
        //{
        //    if (dataState.CurrentData == null)
        //        throw new InvalidOperationException("No data loaded.");
        //    if (dataState.CurrentData.Columns.Contains(columnName))
        //    {
        //        dataState.CurrentData.Columns.Remove(columnName);

        //        var columnAddress = ExcelHelper.GetColumnAddressByHeaderRow(dataState.Workbook!.Worksheet(1), dataState.HeaderRowIndex, columnName);
        //        if (columnAddress != null)
        //        {
        //            dataState.Workbook.Worksheet(1).Column(columnAddress.ColumnNumber).Delete();
        //        }
        //    }
        //}
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
        //public void RemoveColumnsCore(DataState dataState, IEnumerable<string> columnNames)
        //{
        //    if (dataState.CurrentData == null)
        //        throw new InvalidOperationException("No data loaded.");
        //    try
        //    {
        //        foreach (var columnName in columnNames)
        //        {

        //            if (dataState.CurrentData.Columns.Contains(columnName))
        //            {
        //                var columnAddress = ExcelHelper.GetColumnAddressByHeaderRow(dataState.Workbook!.Worksheet(1), dataState.HeaderRowIndex, columnName);
                        
        //                if (columnAddress != null)
        //                {
        //                    dataState.Workbook.Worksheet(1).Column(columnAddress.ColumnNumber).Delete();
        //                }
        //                dataState.CurrentData.Columns.Remove(columnName);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new InvalidOperationException("Error removing columns: " + ex.Message, ex);
        //    }
        //}
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


        //public string AddColumnCore(DataState dataState, ColumnDirection direction, string anchorId, string? newName/*, Type? dataType = null*/)
        //{
        //    if (dataState == null || dataState.CurrentData == null)
        //        throw new InvalidOperationException("No data loaded.");

        //    string newColumnName = newName ?? GenerateColumnName(dataState);

        //    int index = CalculateColumnIndex(dataState, anchorId, direction);

        //    dataState.CurrentData.Columns.Add(newColumnName/*, dataType ?? typeof(object)*/);
        //    dataState.CurrentData.Columns[newColumnName]?.SetOrdinal(index);

        //    var columnAddress = ExcelHelper.GetColumnAddressByHeaderRow(dataState.Workbook!.Worksheet(1), dataState.HeaderRowIndex, anchorId);
        //    var columnFont = dataState.Workbook.Worksheet(1).Cell(dataState.HeaderRowIndex, columnAddress?.ColumnNumber ?? 1).Style.Font;
            
        //    if (columnAddress != null)
        //    {
        //        int excelColumnIndex = columnAddress.ColumnNumber;
        //        if (direction == ColumnDirection.Right)
        //        {
        //            excelColumnIndex++;
        //        }
        //        dataState.Workbook.Worksheet(1).Column(excelColumnIndex).InsertColumnsBefore(1);
        //        dataState.Workbook.Worksheet(1).Cell(dataState.HeaderRowIndex, excelColumnIndex).Value = newColumnName;
        //        dataState.Workbook.Worksheet(1).Cell(dataState.HeaderRowIndex, excelColumnIndex).Style.Font = columnFont;
        //    }

        //    return newColumnName;
        //}
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
        //public void RenameColumnCore(DataState dataState, string oldName, string newName)
        //{
        //    if (dataState == null || dataState.CurrentData == null)
        //        throw new InvalidOperationException("No data loaded.");

        //    if (!dataState.CurrentData.Columns.Contains(oldName))
        //        throw new ArgumentException($"Column '{oldName}' does not exist.");
        //    if (dataState.CurrentData.Columns.Contains(newName))
        //        throw new ArgumentException($"Column name '{newName}' is already taken.");

        //    dataState.CurrentData.Columns[oldName]?.ColumnName = newName;
        //    var columnAddress = ExcelHelper.GetColumnAddressByHeaderRow(dataState.Workbook!.Worksheet(1), dataState.HeaderRowIndex, oldName);
        //    if (columnAddress != null)
        //    {
        //        dataState.Workbook.Worksheet(1).Cell(dataState.HeaderRowIndex, columnAddress.ColumnNumber).Value = newName;
        //    }
        //}

        public void MergeColumns(DataSession session, ColumnReference first, ColumnReference second, string separator, string? resultColumnName, IDataTableActionExecutor actionExecutor)
        {
            //if (session.Data == null)
            //    throw new InvalidOperationException("No file loaded.");

            //if (session.Data.CurrentData == null)
            //    throw new InvalidOperationException("Current data not available.");

            SavePreviousState(session);

            string targetColumn = actionExecutor.MergeColumns(session, first, second, separator, resultColumnName);

            //IXLAddress columnAddress1 = ExcelHelper.GetColumnAddressByHeaderRow(session.Data.Workbook!.Worksheet(1), session.Data.HeaderRowIndex, first.Name);
            //IXLAddress columnAddress2 = ExcelHelper.GetColumnAddressByHeaderRow(session.Data.Workbook!.Worksheet(1), session.Data.HeaderRowIndex, second.Name);

            //string targetColumn =
            //    string.IsNullOrWhiteSpace(resultColumnName)
            //    ? first.Name
            //    : resultColumnName;

            //foreach (DataRow row in session.Data.CurrentData.Rows)
            //{
            //    string firstValue = row[first.Name]?.ToString() ?? "";
            //    string secondValue = row[second.Name]?.ToString() ?? "";

            //    row[first.Name] =
            //        string.IsNullOrEmpty(secondValue)
            //        ? firstValue
            //        : $"{firstValue}{separator}{secondValue}";
            //}

            //for (int row = session.Data.HeaderRowIndex + 1; row <= session.Data.Workbook.Worksheet(1).LastRowUsed()?.RowNumber(); row++)
            //{
            //    string firstValue = session.Data.Workbook.Worksheet(1).Cell(row, columnAddress1?.ColumnNumber ?? 1).GetString();
            //    string secondValue = session.Data.Workbook.Worksheet(1).Cell(row, columnAddress2?.ColumnNumber ?? 1).GetString();
            //    string mergedValue = string.IsNullOrEmpty(secondValue)
            //        ? firstValue
            //        : $"{firstValue}{separator}{secondValue}";
            //    session.Data.Workbook.Worksheet(1).Cell(row, columnAddress1?.ColumnNumber ?? 1).Value = mergedValue;
            //}

            //if (targetColumn != first.Name)
            //{
            //    RenameColumnCore(session.Data, first.Name, targetColumn);
            //}
            //RemoveColumnCore(session.Data, second.Name);

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
        //public void SortDataCore(DataState dataState, string columnName, bool ascending)
        //{
        //    if (dataState.CurrentData == null)
        //        throw new InvalidOperationException("Current data not available.");

        //    if (!dataState.CurrentData.Columns.Contains(columnName))
        //        throw new ArgumentException($"Column '{columnName}' does not exist.");

        //    string direction = ascending ? "ASC" : "DESC";

        //    DataView view = dataState.CurrentData.DefaultView;
        //    view.Sort = $"{columnName} {direction}";

        //    dataState.CurrentData = view.ToTable();

        //    var columnAddress = ExcelHelper.GetColumnAddressByHeaderRow(dataState.Workbook!.Worksheet(1), dataState.HeaderRowIndex, columnName);
        //    var range = ExcelHelper.GetDataRangeAfterHeader(dataState.Workbook.Worksheet(1), dataState.HeaderRowIndex);

        //    range.Sort(columnAddress?.ColumnNumber ?? 1, ascending ? XLSortOrder.Ascending : XLSortOrder.Descending);
        //}

        public void SetColumnDataType(DataSession session, string columnName, Type dataType)
        {
            if (session.Data == null)
                throw new InvalidOperationException("No file loaded.");
            if (session.Data.CurrentData == null)
                throw new InvalidOperationException("Current data not available.");
            if (!session.Data.CurrentData.Columns.Contains(columnName))
                throw new ArgumentException($"Column '{columnName}' does not exist.");

            SavePreviousState(session);

            SetColumnDataTypeCore(session.Data, columnName, dataType);

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
        public void SetColumnDataTypeCore(DataState dataState, string columnName, Type dataType)
        {
            var columnAddress = ExcelHelper.GetColumnAddressByHeaderRow(dataState.Workbook!.Worksheet(1), dataState.HeaderRowIndex, columnName);
            var worksheet = dataState.Workbook.Worksheet(1);

            var columnCells = worksheet
                .Column(columnAddress.ColumnNumber)
                .CellsUsed()
                .Where(c => c.Address.RowNumber > dataState.HeaderRowIndex);

            foreach (var cell in columnCells)
            {
                if (dataType == typeof(string))
                {
                    cell.Style.NumberFormat.Format = "@";
                }
                else
                {
                    cell.Style.NumberFormat.Format = "General";
                }
            }
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

        private string GenerateColumnName(DataState dataState)
        {
            if (dataState.CurrentData == null)
                throw new InvalidOperationException("No current data loaded.");

            string baseName = "NewColumn";
            string name;

            int suffix = dataState.CurrentData.Columns.Count + 1;

            do
            {
                name = $"{baseName}{suffix}";
                suffix++;
            }
            while (dataState.CurrentData.Columns.Contains(name));

            return name;
        }

        private int CalculateColumnIndex(DataState dataState, string anchorId, ColumnDirection direction)
        {
            if (dataState.CurrentData == null)
                throw new InvalidOperationException("No current data loaded.");

            int anchorIndex = dataState.CurrentData.Columns.IndexOf(anchorId);
            if (anchorIndex == -1)
                throw new ArgumentException($"Anchor column '{anchorId}' does not exist.");
            return direction switch
            {
                ColumnDirection.Left => anchorIndex,
                ColumnDirection.Right => anchorIndex + 1,
                _ => throw new ArgumentException("Direction must be 'left' or 'right'.")
            };
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

            actionExecutor.ApplyFormulaToColumn(session.Data, columnName, formula);

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
        //public void ApplyFormulaToColumnCore(DataState dataState, string columnName, string formula)
        //{
        //    if (dataState == null || dataState.CurrentData == null)
        //        throw new InvalidOperationException("No file loaded.");

        //    if (!dataState.CurrentData.Columns.Contains(columnName))
        //        throw new ArgumentException(
        //            $"Column '{columnName}' does not exist.");

        //    var tokens = FormulaService.Tokenize(formula);
        //    var formulaTree = FormulaService.Parse(tokens);

        //    ApplyFormula(
        //        dataState,
        //        columnName,
        //        formulaTree);

        //    var columnAddress = ExcelHelper.GetColumnAddressByHeaderRow(dataState.Workbook!.Worksheet(1), dataState.HeaderRowIndex, columnName);
        //    var excelFormula = FormulaService.ConvertToExcelFormula(formula, dataState);
        //    var dataRange = ExcelHelper.GetDataRangeForColumn(dataState.Workbook.Worksheet(1), dataState.HeaderRowIndex, columnAddress);
        //    var rows = dataRange.Columns().FirstOrDefault()?.Cells() ?? Enumerable.Empty<IXLCell>();
        //    foreach ( var cell in rows )
        //    {
        //        formula = string.Format(excelFormula.Value, cell.Address.RowNumber);
        //        cell.FormulaA1 = formula;
        //    }
        //}
        private void ApplyFormula(DataState dataState, string targetColumn, FormulaNode formulaTree)
        {
            foreach (DataRow row in dataState.CurrentData.Rows)
            {
                decimal result = FormulaService.Evaluate(formulaTree, row);

                row[targetColumn] = result;
                dataState.Workbook!.Worksheet(1).Cell(row.Table.Rows.IndexOf(row) + dataState.HeaderRowIndex + 1, ExcelHelper.GetColumnAddressByHeaderRow(dataState.Workbook.Worksheet(1), dataState.HeaderRowIndex, targetColumn)?.ColumnNumber ?? 1).Value = result;
                SetColumnDataTypeCore(dataState, targetColumn, typeof(double));
            }
        }

        public void SetIsAppliedMappingFalse(DataState dataState)
        {
            if (dataState == null)
                throw new InvalidOperationException("No file loaded.");

            dataState.IsMappingApplied = false;
        }
    }
}
