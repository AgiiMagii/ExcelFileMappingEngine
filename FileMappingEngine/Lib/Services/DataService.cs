using DocumentFormat.OpenXml.Drawing.Diagrams;
using DocumentFormat.OpenXml.Wordprocessing;
using FileMappingEngine.Lib.Helpers;
using FileMappingEngine.Lib.Models;
using FileMappingEngine.Lib.Sessions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace FileMappingEngine.Lib.Services
{
    public class DataService
    {
        private readonly FormulaService formulaService = new();
        private readonly List<ActionStep> previousSteps = [];

        public void ResetTable(DataSession session, DataState dataState)
        {
            if (dataState == null)
                throw new InvalidOperationException("No file loaded.");
            if (dataState.RawData == null)
                throw new InvalidOperationException("Raw data not loaded.");
            if (dataState.RawData.Data == null)
                throw new InvalidOperationException("Raw data table not available.");
            dataState.CurrentData = ExcelHelper.BuildDataTable(dataState.RawData.Data, dataState.HeaderRowIndex);
            ClearSteps(session);
        }
        public void UndoLastAction(DataSession session, DataState dataState)
        {
            if (dataState == null)
                throw new InvalidOperationException("No file loaded.");
            if (dataState.PreviousData == null)
                throw new InvalidOperationException("No previous state available.");
            UndoLastStep(session);
            dataState.CurrentData = dataState.PreviousData.Copy();
        }
        public void UpdateHeaderRow(DataState dataState, int newHeaderRow)
        {
            ArgumentNullException.ThrowIfNull(dataState);

            if (dataState.RawData?.Data == null)
                throw new InvalidOperationException("Raw data not loaded.");

            dataState.HeaderRowIndex = newHeaderRow;
            dataState.CurrentData =
                ExcelHelper.BuildDataTable(
                    dataState.RawData.Data,
                    newHeaderRow);
        }

        public void RemoveColumnCore(DataState dataState, string columnName)
        {
            if (dataState.CurrentData.Columns.Contains(columnName))
            {
                dataState.CurrentData.Columns.Remove(columnName);
            }
        }
        public void RemoveColumn(DataSession session, DataState dataState, string columnName)
        {
            SavePreviousState(session, dataState);

            RemoveColumnCore(dataState, columnName);

            session.MappingSet.Steps.Add(new ActionStep
            {
                ActionType = "DeleteColumn",
                ColumnId = columnName,
                Order = session.MappingSet.Steps.Count + 1
            });
        }

        public string AddColumnCore(DataState dataState, string direction, string anchorId, string? newName, Type? dataType = null)
        {
            string newColumnName = newName ?? GenerateColumnName(dataState);

            int index = CalculateColumnIndex(dataState, anchorId, direction);

            dataState.CurrentData.Columns.Add(newColumnName, dataType ?? typeof(object));
            dataState.CurrentData.Columns[newColumnName]?.SetOrdinal(index);

            return newColumnName;
        }
        public void AddColumn(DataSession session, DataState dataState, string direction, string anchorId, string? newName)
        {
            SavePreviousState(session, dataState);

            string newColumnName = AddColumnCore(dataState, direction, anchorId, newName);

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

        public void RenameColumn(DataSession session, DataState dataState, string oldName, string newName)
        {
            SavePreviousState(session, dataState);

            RenameColumnCore(dataState, oldName, newName);

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
        public void RenameColumnCore(DataState dataState, string oldName, string newName)
        {
            if (!dataState.CurrentData.Columns.Contains(oldName))
                throw new ArgumentException($"Column '{oldName}' does not exist.");
            if (dataState.CurrentData.Columns.Contains(newName))
                throw new ArgumentException($"Column name '{newName}' is already taken.");

            dataState.CurrentData.Columns[oldName]?.ColumnName = newName;
        }

        public void MergeColumns(DataSession session, DataState dataState, ColumnReference first, ColumnReference second, string separator, string? resultColumnName)
        {
            if (dataState == null)
                throw new InvalidOperationException("No file loaded.");

            if (dataState.CurrentData == null)
                throw new InvalidOperationException("Current data not available.");

            SavePreviousState(session, dataState);

            string targetColumn =
                string.IsNullOrWhiteSpace(resultColumnName)
                ? first.Name
                : resultColumnName;


            if (targetColumn != first.Name)
            {
                AddColumnCore(dataState, "right", first.Name, targetColumn);
            }


            foreach (DataRow row in dataState.CurrentData.Rows)
            {
                string firstValue = row[first.Name]?.ToString() ?? "";
                string secondValue = row[second.Name]?.ToString() ?? "";

                row[targetColumn] =
                    string.IsNullOrEmpty(secondValue)
                    ? firstValue
                    : $"{firstValue}{separator}{secondValue}";
            }
            session.MappingSet.Steps.Add(new ActionStep
            {
                ActionType = "RenameColumn",
                ColumnId = first.Name,
                Order = session.MappingSet.Steps.Count + 1,
                Parameters = new Dictionary<string, object>
                {
                    ["NewName"] = targetColumn
                }
            });

            if (targetColumn != first.Name)
            {
                RemoveColumnCore(dataState, first.Name);
            }

            RemoveColumnCore(dataState, second.Name);
        }

        public void SortData(DataSession session, DataState dataState, string columnName, bool ascending)
        {
            if (dataState == null)
                throw new InvalidOperationException("No file loaded.");
            SavePreviousState(session, dataState);

            SortDataCore(dataState, columnName, ascending);

            dataState.SortedColumn = columnName;
            dataState.SortAscending = ascending;
        }
        public void SortDataCore(DataState dataState, string columnName, bool ascending)
        {
            if (!dataState.CurrentData.Columns.Contains(columnName))
                throw new ArgumentException($"Column '{columnName}' does not exist.");

            string direction = ascending ? "ASC" : "DESC";

            DataView view = dataState.CurrentData.DefaultView;
            view.Sort = $"{columnName} {direction}";

            dataState.CurrentData = view.ToTable();
        }

        public void SetColumnDataType(DataSession session, DataState dataState, string columnName, Type dataType)
        {
            if (dataState == null)
                throw new InvalidOperationException("No file loaded.");
            if (dataState.CurrentData == null)
                throw new InvalidOperationException("Current data not available.");
            if (!dataState.CurrentData.Columns.Contains(columnName))
                throw new ArgumentException($"Column '{columnName}' does not exist.");

            SavePreviousState(session, dataState);

            SetColumnDataTypeCore(dataState, columnName, dataType);

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
            if (!dataState.CurrentData.Columns.Contains(columnName))
                throw new ArgumentException($"Column '{columnName}' does not exist.");

            DataColumn oldColumn = dataState.CurrentData.Columns[columnName]!;
            int ordinal = oldColumn.Ordinal;

            // Izveido jaunu kolonnu ar vajadzīgo datu tipu
            DataColumn newColumn = new(columnName + "_tmp", dataType);

            dataState.CurrentData.Columns.Add(newColumn);

            // Pārkopē un konvertē datus
            foreach (DataRow row in dataState.CurrentData.Rows)
            {
                object value = row[oldColumn];

                if (value == DBNull.Value)
                {
                    row[newColumn] = DBNull.Value;
                    continue;
                }

                try
                {
                    row[newColumn] = Convert.ChangeType(value, dataType);
                }
                catch (InvalidCastException)
                {
                    row[newColumn] = DBNull.Value;
                }
                catch (FormatException)
                {
                    row[newColumn] = DBNull.Value;
                }
                catch (OverflowException)
                {
                    row[newColumn] = DBNull.Value;
                }
            }

            // Noņem veco kolonnu
            dataState.CurrentData.Columns.Remove(oldColumn);

            // Pārsauc jauno kolonnu un atgriež sākotnējā vietā
            newColumn.ColumnName = columnName;
            newColumn.SetOrdinal(ordinal);
        }

        private void SavePreviousState(DataSession session, DataState dataState)
        {
            dataState?.PreviousData = dataState.CurrentData.Copy();

            previousSteps.Clear();
            previousSteps.AddRange(session.MappingSet.Steps);
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

        private int CalculateColumnIndex(DataState dataState, string anchorId, string direction)
        {
            int anchorIndex = dataState.CurrentData.Columns.IndexOf(anchorId);
            if (anchorIndex == -1)
                throw new ArgumentException($"Anchor column '{anchorId}' does not exist.");
            return direction.ToLower() switch
            {
                "left" => anchorIndex,
                "right" => anchorIndex + 1,
                _ => throw new ArgumentException("Direction must be 'left' or 'right'.")
            };
        }

        public bool IsColumnNameTaken(DataState dataState, string columnName)
        {
            return dataState.CurrentData.Columns.Contains(columnName);
        }

        public void ApplyFormulaToColumn(DataSession session, DataState dataState, string columnName, string formula)
        {
            if (dataState == null)
                throw new InvalidOperationException("No file loaded.");

            if (!dataState.CurrentData.Columns.Contains(columnName))
                throw new ArgumentException($"Column '{columnName}' does not exist.");

            SavePreviousState(session, dataState);

            ApplyFormulaToColumnCore(dataState, columnName, formula);

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
        public void ApplyFormulaToColumnCore(DataState dataState, string columnName, string formula)
        {
            if (dataState == null)
                throw new InvalidOperationException("No file loaded.");

            if (!dataState.CurrentData.Columns.Contains(columnName))
                throw new ArgumentException(
                    $"Column '{columnName}' does not exist.");


            var tokenizer = new FormulaService();


            var tokens = FormulaService.Tokenize(formula);


            var formulaTree = tokenizer.Parse(tokens);


            ApplyFormula(
                dataState.CurrentData,
                columnName,
                formulaTree);

        }
        private void ApplyFormula(DataTable dataTable, string targetColumn, FormulaNode formulaTree)
        {
            foreach (DataRow row in dataTable.Rows)
            {
                decimal result = formulaService.Evaluate(formulaTree, row);

                row[targetColumn] = result;
            }
        }
    }
}
