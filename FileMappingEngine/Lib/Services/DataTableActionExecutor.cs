using ClosedXML.Excel;
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
    public class DataTableActionExecutor : IDataTableActionExecutor
    {

        public void RemoveColumn(DataState dataState, string columnName)
        {
            if (dataState.CurrentData == null)
                throw new InvalidOperationException("No data loaded.");

            if (!dataState.CurrentData.Columns.Contains(columnName))
                throw new ArgumentException($"Column '{columnName}' does not exist.");

            if (dataState.CurrentData.Columns.Contains(columnName))
            {
                dataState.CurrentData.Columns.Remove(columnName);
            }
        }

        public void RemoveColumns(DataState dataState, IEnumerable<string> columnNames)
        {
            if (dataState.CurrentData == null)
                throw new InvalidOperationException("No data loaded.");

            var missing = columnNames.Where(c => !dataState.CurrentData.Columns.Contains(c)).ToList();
            if (missing.Count > 0)
                throw new ArgumentException($"Column(s) not found: {string.Join(", ", missing)}");

            foreach (var columnName in columnNames)
            {
                dataState.CurrentData.Columns.Remove(columnName);
            }
        }

        public string AddColumn(DataState dataState, ColumnDirection direction, string anchorId, string? newName)
        {
            if (dataState == null || dataState.CurrentData == null)
                throw new InvalidOperationException("No data loaded.");

            string newColumnName = newName ?? GenerateColumnName(dataState);

            int index = CalculateColumnIndex(dataState, anchorId, direction);

            dataState.CurrentData.Columns.Add(newColumnName);
            dataState.CurrentData.Columns[newColumnName]?.SetOrdinal(index);

            return newColumnName;
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

        public void RenameColumn(DataState dataState, string oldName, string newName)
        {
            if (dataState == null || dataState.CurrentData == null)
                throw new InvalidOperationException("No data loaded.");

            if (!dataState.CurrentData.Columns.Contains(oldName))
                throw new ArgumentException($"Column '{oldName}' does not exist.");
            if (dataState.CurrentData.Columns.Contains(newName))
                throw new ArgumentException($"Column name '{newName}' is already taken.");

            dataState.CurrentData.Columns[oldName]?.ColumnName = newName;
        }

        public string MergeColumns(DataSession session, ColumnReference first, ColumnReference second, string separator, string? resultColumnName)
        {
            if (session.Data == null)
                throw new InvalidOperationException("No file loaded.");

            if (session.Data.CurrentData == null)
                throw new InvalidOperationException("Current data not available.");

            string targetColumn =
                string.IsNullOrWhiteSpace(resultColumnName)
                ? first.Name
                : resultColumnName;

            foreach (DataRow row in session.Data.CurrentData.Rows)
            {
                string firstValue = row[first.Name]?.ToString() ?? "";
                string secondValue = row[second.Name]?.ToString() ?? "";

                row[first.Name] =
                    string.IsNullOrEmpty(secondValue)
                    ? firstValue
                    : $"{firstValue}{separator}{secondValue}";
            }

            if (targetColumn != first.Name)
            {
                RenameColumn(session.Data, first.Name, targetColumn);
            }
            RemoveColumn(session.Data, second.Name);
            return targetColumn;
        }

        public void SortData(DataState dataState, string columnName, bool ascending)
        {
            if (dataState.CurrentData == null)
                throw new InvalidOperationException("Current data not available.");

            if (!dataState.CurrentData.Columns.Contains(columnName))
                throw new ArgumentException($"Column '{columnName}' does not exist.");

            string direction = ascending ? "ASC" : "DESC";

            DataView view = dataState.CurrentData.DefaultView;
            view.Sort = $"{columnName} {direction}";

            dataState.CurrentData = view.ToTable();
        }

        public void ApplyFormulaToColumn(DataState dataState, string columnName, string formula)
        {
            if (dataState == null || dataState.CurrentData == null)
                throw new InvalidOperationException("No file loaded.");

            if (!dataState.CurrentData.Columns.Contains(columnName))
                throw new ArgumentException(
                    $"Column '{columnName}' does not exist.");

            var tokens = FormulaService.Tokenize(formula);
            var formulaTree = FormulaService.Parse(tokens);

            ApplyFormula(dataState, columnName, formulaTree);
        }
        public void ApplyFormula(DataState dataState, string targetColumn, FormulaNode formulaTree)
        {
            foreach (DataRow row in dataState.CurrentData.Rows)
            {
                decimal result = FormulaService.Evaluate(formulaTree, row);

                row[targetColumn] = result;
            }
        }

        public void SetColumnDataType(DataState dataState, string columnName, Type dataType)
        {
            throw new NotImplementedException();
        }
    }
}
