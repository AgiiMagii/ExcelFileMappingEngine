using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using FileMappingEngine.Lib.Helpers;
using FileMappingEngine.Lib.Interfaces;
using FileMappingEngine.Lib.Models;
using FileMappingEngine.Lib.Sessions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using static FileMappingEngine.Lib.Models.Enums;


namespace FileMappingEngine.Lib.Services
{
    public class WorkbookActionExecutor : IWorkbookActionExecutor
    {
        public void RemoveColumn(DataState dataState, string columnName)
        {
            var columnAddress = ExcelHelper.GetColumnAddressByHeaderRow(dataState.Workbook!.Worksheet(1), dataState.HeaderRowIndex, columnName);
            if (columnAddress != null)
            {
                dataState.Workbook.Worksheet(1).Column(columnAddress.ColumnNumber).Delete();
            }
        }

        public void RemoveColumns(DataState dataState, IEnumerable<string> columnNames)
        {
            var worksheet = dataState.Workbook!.Worksheet(1);

            var addresses = columnNames
                .Select(name => ExcelHelper.GetColumnAddressByHeaderRow(worksheet, dataState.HeaderRowIndex, name).ColumnNumber)
                .OrderByDescending(colNum => colNum)   // delete right-to-left so earlier addresses don't shift
                .ToList();

            foreach (var colNum in addresses)
            {
                worksheet.Column(colNum).Delete();
            }
        }

        public void AddColumn(DataState dataState, ColumnDirection direction, string anchorId, string? newName)
        {
            var columnAddress = ExcelHelper.GetColumnAddressByHeaderRow(dataState.Workbook!.Worksheet(1), dataState.HeaderRowIndex, anchorId);
            var columnFont = dataState.Workbook.Worksheet(1).Cell(dataState.HeaderRowIndex, columnAddress?.ColumnNumber ?? 1).Style.Font;

            if (columnAddress != null)
            {
                int excelColumnIndex = columnAddress.ColumnNumber;
                if (direction == ColumnDirection.Right)
                {
                    excelColumnIndex++;
                }
                dataState.Workbook.Worksheet(1).Column(excelColumnIndex).InsertColumnsBefore(1);
                dataState.Workbook.Worksheet(1).Cell(dataState.HeaderRowIndex, excelColumnIndex).Value = newName;
                dataState.Workbook.Worksheet(1).Cell(dataState.HeaderRowIndex, excelColumnIndex).Style.Font = columnFont;
            }
        }

        public void RenameColumn(DataState dataState, string oldName, string newName)
        {
            var columnAddress = ExcelHelper.GetColumnAddressByHeaderRow(dataState.Workbook!.Worksheet(1), dataState.HeaderRowIndex, oldName);
            if (columnAddress != null)
            {
                dataState.Workbook.Worksheet(1).Cell(dataState.HeaderRowIndex, columnAddress.ColumnNumber).Value = newName;
            }
        }

        public void MergeColumns(DataSession session, ColumnReference first, ColumnReference second, string separator, string? resultColumnName)
        {
            IXLAddress columnAddress1 = ExcelHelper.GetColumnAddressByHeaderRow(session.Data.Workbook!.Worksheet(1), session.Data.HeaderRowIndex, first.Name);
            IXLAddress columnAddress2 = ExcelHelper.GetColumnAddressByHeaderRow(session.Data.Workbook!.Worksheet(1), session.Data.HeaderRowIndex, second.Name);

            string targetColumn =
                string.IsNullOrWhiteSpace(resultColumnName)
                ? first.Name
                : resultColumnName;

            for (int row = session.Data.HeaderRowIndex + 1; row <= session.Data.Workbook.Worksheet(1).LastRowUsed()?.RowNumber(); row++)
            {
                string firstValue = session.Data.Workbook.Worksheet(1).Cell(row, columnAddress1?.ColumnNumber ?? 1).GetString();
                string secondValue = session.Data.Workbook.Worksheet(1).Cell(row, columnAddress2?.ColumnNumber ?? 1).GetString();
                string mergedValue = string.IsNullOrEmpty(secondValue)
                    ? firstValue
                    : $"{firstValue}{separator}{secondValue}";
                session.Data.Workbook.Worksheet(1).Cell(row, columnAddress1?.ColumnNumber ?? 1).Value = mergedValue;
            }

            if (targetColumn != first.Name)
            {
                RenameColumn(session.Data, first.Name, targetColumn);
            }
            RemoveColumn(session.Data, second.Name);
        }

        public void SortData(DataState dataState, string columnName, bool ascending)
        {
            //string direction = ascending ? "ASC" : "DESC";

            var columnAddress = ExcelHelper.GetColumnAddressByHeaderRow(dataState.Workbook!.Worksheet(1), dataState.HeaderRowIndex, columnName);
            var range = ExcelHelper.GetDataRangeAfterHeader(dataState.Workbook.Worksheet(1), dataState.HeaderRowIndex);

            range.Sort(columnAddress?.ColumnNumber ?? 1, ascending ? XLSortOrder.Ascending : XLSortOrder.Descending);
        }

        public void ApplyFormulaToColumn(DataState dataState, string columnName, string formula)
        {
            var tokens = FormulaService.Tokenize(formula);
            var formulaTree = FormulaService.Parse(tokens);

            ApplyFormula(
                dataState,
                columnName,
                formulaTree);

            var columnAddress = ExcelHelper.GetColumnAddressByHeaderRow(dataState.Workbook!.Worksheet(1), dataState.HeaderRowIndex, columnName);
            var excelFormula = FormulaService.ConvertToExcelFormula(formula, dataState);
            var dataRange = ExcelHelper.GetDataRangeForColumn(dataState.Workbook.Worksheet(1), dataState.HeaderRowIndex, columnAddress);
            var rows = dataRange.Columns().FirstOrDefault()?.Cells() ?? Enumerable.Empty<IXLCell>();
            foreach (var cell in rows)
            {
                formula = string.Format(excelFormula.Value, cell.Address.RowNumber);
                cell.FormulaA1 = formula;
            }
        }

        public void ApplyFormula(DataState dataState, string targetColumn, FormulaNode formulaTree)
        {
            IXLAddress targetColumnAddress = ExcelHelper.GetColumnAddressByHeaderRow(dataState.Workbook!.Worksheet(1), dataState.HeaderRowIndex, targetColumn);
            IXLCells targetCells = ExcelHelper.GetDataRangeForColumn(dataState.Workbook!.Worksheet(1), dataState.HeaderRowIndex, targetColumnAddress).Cells();
            string formula = FormulaService.ConvertToExcelFormula(formulaTree.Value, dataState).Value;

            foreach (var cell in targetCells)
            {
                formula = string.Format(formula, cell.Address.RowNumber);
                cell.FormulaA1 = formula;

                dataState.Workbook.Worksheet(1).Cell(cell.Address.RowNumber, targetColumnAddress.ColumnNumber).FormulaA1 = formula;
                SetColumnDataType(dataState, targetColumn, typeof(double));
            }
        }

        public void SetColumnDataType(DataState dataState, string columnName, Type dataType)
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
    }
}
