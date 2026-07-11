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
using System.Windows;

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
            if (session.Data.PreviousData == null)
                throw new InvalidOperationException("No previous state available.");
            UndoLastStep(session);
            session.Data.CurrentData = session.Data.PreviousData.Copy();
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

        public void RemoveColumnCore(DataState dataState, string columnName)
        {
            if (dataState.CurrentData == null)
                throw new InvalidOperationException("No data loaded.");
            if (dataState.CurrentData.Columns.Contains(columnName))
            {
                dataState.CurrentData.Columns.Remove(columnName);
            }
        }
        public void RemoveColumn(DataSession session, string columnName)
        {
            if (session.Data == null)
                throw new InvalidOperationException("No data loaded.");
            SavePreviousState(session);

            RemoveColumnCore(session.Data, columnName);

            session.MappingSet.Steps.Add(new ActionStep
            {
                ActionType = "DeleteColumn",
                ColumnId = columnName,
                Order = session.MappingSet.Steps.Count + 1
            });
        }

        public string AddColumnCore(DataState dataState, string direction, string anchorId, string? newName, Type? dataType = null)
        {
            if (dataState == null || dataState.CurrentData == null)
                throw new InvalidOperationException("No data loaded.");

            string newColumnName = newName ?? GenerateColumnName(dataState);

            int index = CalculateColumnIndex(dataState, anchorId, direction);

            dataState.CurrentData.Columns.Add(newColumnName, dataType ?? typeof(object));
            dataState.CurrentData.Columns[newColumnName]?.SetOrdinal(index);

            return newColumnName;
        }
        public void AddColumn(DataSession session, string direction, string anchorId, string? newName)
        {
            if (session.Data == null)
                throw new InvalidOperationException("No data loaded.");

            SavePreviousState(session);

            string newColumnName = AddColumnCore(session.Data, direction, anchorId, newName);

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

        public void RenameColumn(DataSession session, string oldName, string newName)
        {
            if (session.Data == null)
                throw new InvalidOperationException("No data loaded.");

            SavePreviousState(session);

            RenameColumnCore(session.Data, oldName, newName);

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
            if (dataState == null || dataState.CurrentData == null)
                throw new InvalidOperationException("No data loaded.");

            if (!dataState.CurrentData.Columns.Contains(oldName))
                throw new ArgumentException($"Column '{oldName}' does not exist.");
            if (dataState.CurrentData.Columns.Contains(newName))
                throw new ArgumentException($"Column name '{newName}' is already taken.");

            dataState.CurrentData.Columns[oldName]?.ColumnName = newName;
        }

        public void MergeColumns(DataSession session, ColumnReference first, ColumnReference second, string separator, string? resultColumnName)
        {
            if (session.Data == null)
                throw new InvalidOperationException("No file loaded.");

            if (session.Data.CurrentData == null)
                throw new InvalidOperationException("Current data not available.");

            SavePreviousState(session);

            string targetColumn =
                string.IsNullOrWhiteSpace(resultColumnName)
                ? first.Name
                : resultColumnName;


            if (targetColumn != first.Name)
            {
                AddColumnCore(session.Data, "right", first.Name, targetColumn);
            }


            foreach (DataRow row in session.Data.CurrentData.Rows)
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

            if (targetColumn != first.Name)
            {
                RemoveColumnCore(session.Data, first.Name);
            }

            RemoveColumnCore(session.Data, second.Name);
        }

        public void SortData(DataSession session, string columnName, bool ascending)
        {
            if (session.Data == null)
                throw new InvalidOperationException("No file loaded.");
            SavePreviousState(session);

            SortDataCore(session.Data, columnName, ascending);

            session.Data.SortedColumn = columnName;
            session.Data.SortAscending = ascending;
        }
        public void SortDataCore(DataState dataState, string columnName, bool ascending)
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
            if (dataState.CurrentData == null)
                throw new InvalidOperationException("Current data not available.");

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

        private void SavePreviousState(DataSession session)
        {
            if (session.Data == null)
                throw new InvalidOperationException("No data loaded.");
            if (session.Data.CurrentData == null)
                throw new InvalidOperationException("No current data loaded.");

            session.Data.PreviousData = session.Data.CurrentData.Copy();
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

        private int CalculateColumnIndex(DataState dataState, string anchorId, string direction)
        {
            if (dataState.CurrentData == null)
                throw new InvalidOperationException("No current data loaded.");

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
            if (dataState.CurrentData == null)
                throw new InvalidOperationException("No current data loaded.");

            return dataState.CurrentData.Columns.Contains(columnName);
        }

        public void ApplyFormulaToColumn(DataSession session, string columnName, string formula)
        {
            if (session.Data == null || session.Data.CurrentData == null)
                throw new InvalidOperationException("No data loaded.");

            if (!session.Data.CurrentData.Columns.Contains(columnName))
                throw new ArgumentException($"Column '{columnName}' does not exist.");

            SavePreviousState(session);

            ApplyFormulaToColumnCore(session.Data, columnName, formula);

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
            if (dataState == null || dataState.CurrentData == null)
                throw new InvalidOperationException("No file loaded.");

            if (!dataState.CurrentData.Columns.Contains(columnName))
                throw new ArgumentException(
                    $"Column '{columnName}' does not exist.");

            var tokens = FormulaService.Tokenize(formula);
            var formulaTree = FormulaService.Parse(tokens);

            ApplyFormula(
                dataState.CurrentData,
                columnName,
                formulaTree);

        }
        private void ApplyFormula(DataTable dataTable, string targetColumn, FormulaNode formulaTree)
        {
            foreach (DataRow row in dataTable.Rows)
            {
                decimal result = FormulaService.Evaluate(formulaTree, row);

                row[targetColumn] = result;
            }
        }
    }
}
