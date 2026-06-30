using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using DocumentFormat.OpenXml.Spreadsheet;
using FileMappingEngine.Lib.Models;
using FileMappingEngine.Lib.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing.Text;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows.Controls;

namespace FileMappingEngine.Lib
{
    public class AppManager
    {
        // Kad veidos validāciju, pievienot pārbaudi uz header row change,
        // lai pārliecinātos, ka nav mainījušās kolonnas, kas jau ir izmantotas mappingos
        // un brīdinātu lietotāju, ka pēc header row maiņas, veiktie mappingi tiks dzēsti.

        // Esošo mappingu pogas jārāda tikai tās, kuras piemērotas konkrētam failam
        // - izveidot pārbaudi, kas nosaka, kuri mappingi atbilst faila struktūrai.
        private readonly MappingService mappingEngine = new();
        private readonly FormulaService formulaService = new();
        public FileState? CurrentFile { get; private set; }
        public DataTable CurrentData => CurrentFile?.CurrentData ?? throw new InvalidOperationException("No file loaded.");

        private bool IsApplyingMapping { get; set; }

        public DataTable OpenFile(string filePath)
        {
            CurrentFile = new FileState
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                RawData = ExcelService.LoadRawData(filePath)
            };

            if (CurrentFile.RawData?.Data == null)
                throw new InvalidOperationException("Failed to load data from file.");

            CurrentFile.CurrentData = ExcelService.BuildDataTable(CurrentFile.RawData.Data, CurrentFile.HeaderRowIndex);
            CurrentFile.Columns = CurrentFile.RawData.Columns;

            return CurrentData;
        }

        public void UpdateHeaderRow(int newHeaderRow)
        {
            if (CurrentFile == null)
                throw new InvalidOperationException("No file loaded.");
            if (CurrentFile.RawData == null)
                throw new InvalidOperationException("Raw data not loaded.");
            if (CurrentFile.RawData.Data == null)
                throw new InvalidOperationException("Raw data table not available.");
            CurrentFile.HeaderRowIndex = newHeaderRow;
            CurrentFile.CurrentData = ExcelService.BuildDataTable(CurrentFile.RawData.Data, CurrentFile.HeaderRowIndex);
        }

        public void CloseCurrentFile()
        {
            CurrentFile = null;
        }

        public static string[] GetExistingMappings()
        {
            try
            {
                return MappingService.GetJsonFiles();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving existing mappings: {ex.Message}");
                return [];
            }
        }

        public void SaveFile(string filePath)
        {
            if (CurrentFile == null)
                throw new InvalidOperationException("No file loaded.");
            if (CurrentFile.CurrentData == null)
                throw new InvalidOperationException("Current data not available.");

            List<List<string>> ignoredRows =
                CurrentFile.IgnoredRows?
                .Select(r => r.ToList())
                .ToList()
                ?? [];

            DataTable dataToSave = CurrentFile.CurrentData.Copy();

            //for (int i = 0; i < CurrentFile.HeaderRowIndex - 1; i++)
            //{
            //    dataToSave.Rows.RemoveAt(0);
            //}

            ExcelService.SaveFile(
                filePath,
                dataToSave,
                ignoredRows);
        }

        public void RemoveColumn(string columnName)
        {
            SavePreviousState();

            RemoveColumnCore(columnName);

            if (!IsApplyingMapping)
            {
                mappingEngine.RemoveColumnStep(columnName);
            }
        }

        private void RemoveColumnCore(string columnName)
        {
            if (CurrentData.Columns.Contains(columnName))
            {
                CurrentData.Columns.Remove(columnName);
            }
        }

        public void AddColumn(string direction, string anchorId, string? newName)
        {
            SavePreviousState();

            string newColumnName = AddColumnCore(direction, anchorId, newName);

            if (!IsApplyingMapping)
            {
                mappingEngine.AddNewColumnStep(
                newColumnName,
                anchorId,
                direction);
            }
        }

        private string AddColumnCore(string direction, string anchorId, string? newName, Type? dataType = null)
        {
            string newColumnName = newName ?? GenerateColumnName();

            int index = CalculateColumnIndex(anchorId, direction);

            CurrentData.Columns.Add(newColumnName, dataType ?? typeof(object));
            CurrentData.Columns[newColumnName]?.SetOrdinal(index);

            return newColumnName;
        }

        private string GenerateColumnName()
        {
            string baseName = "NewColumn";
            string name;

            int suffix = CurrentData.Columns.Count + 1;

            do
            {
                name = $"{baseName}{suffix}";
                suffix++;
            }
            while (CurrentData.Columns.Contains(name));

            return name;
        }

        private int CalculateColumnIndex(string anchorId, string direction)
        {
            int anchorIndex = CurrentData.Columns.IndexOf(anchorId);
            if (anchorIndex == -1)
                throw new ArgumentException($"Anchor column '{anchorId}' does not exist.");
            return direction.ToLower() switch
            {
                "left" => anchorIndex,
                "right" => anchorIndex + 1,
                _ => throw new ArgumentException("Direction must be 'left' or 'right'.")
            };
        }

        public void SaveMappingSet(string filePath)
        {
            if (CurrentFile == null)
                throw new InvalidOperationException("No file loaded.");

            if (CurrentFile.SortedColumn != null && CurrentFile.SortAscending.HasValue)
            {
                mappingEngine.AddSortStep(CurrentFile.SortedColumn, CurrentFile.SortAscending.Value);
            }

            string fileName = Path.GetFileNameWithoutExtension(filePath);
            var mapping = mappingEngine.GenerateMappingSet(
                fileName,
                CurrentFile.HeaderRowIndex);
            
            JsonService.CreateJson(mapping, filePath);
        }

        public bool IsColumnNameTaken(string columnName)
        {
            return CurrentData.Columns.Contains(columnName);
        }

        public void RenameColumn(string oldName, string newName)
        {
            SavePreviousState();

            RenameColumnCore(oldName, newName);

            if (!IsApplyingMapping)
            {
                mappingEngine.RenameColumnStep(oldName, newName);
            }
        }

        private void RenameColumnCore(string oldName, string newName)
        {
            if (!CurrentData.Columns.Contains(oldName))
                throw new ArgumentException($"Column '{oldName}' does not exist.");
            if (CurrentData.Columns.Contains(newName))
                throw new ArgumentException($"Column name '{newName}' is already taken.");

            CurrentData.Columns[oldName]?.ColumnName = newName;
        }

        public void ApplyMappingSet(string filePath)
        {
            if (CurrentFile == null)
                throw new InvalidOperationException("No file loaded.");
            if (CurrentFile.CurrentData == null)
                throw new InvalidOperationException("Current data not available.");

            string json = File.ReadAllText(filePath);
            MappingSet mapping = JsonSerializer.Deserialize<MappingSet>(json) ?? throw new InvalidOperationException("Failed to deserialize mapping set.");
            try
            {
                IsApplyingMapping = true;

                CurrentFile.HeaderRowIndex = mapping.HeaderRow;

                UpdateHeaderRow(CurrentFile.HeaderRowIndex);

                ExecuteMappingSteps(mapping);

            }
            finally
            {
                IsApplyingMapping = false;
            }
        }

        private void ExecuteMappingSteps(MappingSet mapping)
        {
            foreach (var step in mapping.Steps.OrderBy(s => s.Order))
            {
                switch (step.ActionType)
                {
                    case "DeleteColumn":
                        if (step.ColumnId == null)
                            throw new InvalidOperationException("Column ID missing for DeleteColumn action.");
                        RemoveColumnCore(step.ColumnId);
                        break;
                    case "AddColumn":
                        if (step.Parameters == null)
                            throw new InvalidOperationException("Parameters missing for AddColumn action.");
                        string anchorId = step.Parameters["AnchorColumnId"].ToString() ?? throw new InvalidOperationException("Anchor column ID missing.");
                        string direction = step.Parameters["Direction"].ToString() ?? throw new InvalidOperationException("Direction missing.");
                        string? givenName = step.Parameters.TryGetValue("NewName", out object? value) ? value.ToString() : null;
                        Type? type = step.Parameters.TryGetValue("DataType", out object? typeValue) ? Type.GetType(typeValue.ToString() ?? "") : null;

                        AddColumnCore(direction, anchorId, givenName, type);
                        break;
                    case "RenameColumn":
                        if (step.Parameters == null)
                            throw new InvalidOperationException("Parameters missing for RenameColumn action.");
                        if (step.ColumnId == null)
                            throw new InvalidOperationException("Column ID missing for RenameColumn action.");
                        string newName = step.Parameters["NewName"].ToString() ?? throw new InvalidOperationException("New name missing.");
                        RenameColumnCore(step.ColumnId, newName);
                        break;
                    case "MergeColumns":
                        if (step.Parameters == null)
                            throw new InvalidOperationException("Parameters missing for MergeColumns action.");
                        string firstColumnName = step.Parameters["FirstColumn"].ToString() ?? throw new InvalidOperationException("First column name missing.");
                        string secondColumnName = step.Parameters["SecondColumn"].ToString() ?? throw new InvalidOperationException("Second column name missing.");
                        string separator = step.Parameters["Separator"].ToString() ?? throw new InvalidOperationException("Separator missing.");
                        MergeColumns(new ColumnReference { Name = firstColumnName }, new ColumnReference { Name = secondColumnName }, separator, step.ColumnId);
                        break;
                    case "Sort":
                        if (step.Parameters == null)
                            throw new InvalidOperationException("Parameters missing for Sort action.");
                        if (step.ColumnId == null)
                            throw new InvalidOperationException("Column ID missing for Sort action.");
                        bool ascending = ((JsonElement)step.Parameters["Ascending"]).GetBoolean();
                        SortDataCore(step.ColumnId, ascending);
                        break;
                    case "Formula":
                        if (step.Parameters == null)
                            throw new InvalidOperationException("Parameters missing for Formula action.");
                        if (step.ColumnId == null)
                            throw new InvalidOperationException("Column ID missing for Formula action.");
                        string formula = step.Parameters["Formula"].ToString() ?? throw new InvalidOperationException("Formula missing.");
                        ApplyFormulaToColumnCore(step.ColumnId, formula);
                        break;
                    case "SetColumnDataType":
                        if (step.Parameters == null)
                            throw new InvalidOperationException("Parameters missing for SetColumnDataType action.");

                        string typeName = step.Parameters["DataType"] is JsonElement element
                            ? element.GetString() ?? throw new InvalidOperationException("DataType missing.")
                            : step.Parameters["DataType"].ToString()
                                ?? throw new InvalidOperationException("DataType missing.");

                        Type dataType = Type.GetType(typeName)
                            ?? throw new InvalidOperationException($"Unknown type: {typeName}");

                        if (step.ColumnId == null)
                            throw new InvalidOperationException("Column ID missing.");

                        SetColumnDataTypeCore(step.ColumnId, dataType);
                        break;

                    default:
                        throw new InvalidOperationException($"Unknown action type: {step.ActionType}");
                }
            }
        }

        public void ResetTable()
        {
            if (CurrentFile == null)
                throw new InvalidOperationException("No file loaded.");
            if (CurrentFile.RawData == null)
                throw new InvalidOperationException("Raw data not loaded.");
            if (CurrentFile.RawData.Data == null)
                throw new InvalidOperationException("Raw data table not available.");
            CurrentFile.CurrentData = ExcelService.BuildDataTable(CurrentFile.RawData.Data, CurrentFile.HeaderRowIndex);
            mappingEngine.ClearSteps();
        }

        public void SavePreviousState()
        {
            CurrentFile?.PreviousData = CurrentData.Copy();
            mappingEngine.SavePreviousSteps();
        }

        public void UndoLastAction()
        {
            if (CurrentFile == null)
                throw new InvalidOperationException("No file loaded.");
            if (CurrentFile.PreviousData == null)
                throw new InvalidOperationException("No previous state available.");
            mappingEngine.UndoLastStep();
            CurrentFile.CurrentData = CurrentFile.PreviousData.Copy();
        }

        public void MergeColumns(ColumnReference first, ColumnReference second, string separator, string? resultColumnName)
        {
            if (CurrentFile == null)
                throw new InvalidOperationException("No file loaded.");

            if (CurrentData == null)
                throw new InvalidOperationException("Current data not available.");

            SavePreviousState();

            string targetColumn =
                string.IsNullOrWhiteSpace(resultColumnName)
                ? first.Name
                : resultColumnName;


            if (targetColumn != first.Name)
            {
                AddColumnCore("right", first.Name, targetColumn);
            }


            foreach (DataRow row in CurrentData.Rows)
            {
                string firstValue = row[first.Name]?.ToString() ?? "";
                string secondValue = row[second.Name]?.ToString() ?? "";

                row[targetColumn] =
                    string.IsNullOrEmpty(secondValue)
                    ? firstValue
                    : $"{firstValue}{separator}{secondValue}";
            }

            if (!IsApplyingMapping)
            {
                mappingEngine.AddMergeColumnsStep(
                    targetColumn,
                    first.Name,
                    second.Name,
                    separator);
            }

            if (targetColumn != first.Name)
            {
                RemoveColumnCore(first.Name);
            }

            RemoveColumnCore(second.Name);
        }

        public List<ColumnReference> GetDataColumns()
        {
            if (CurrentFile == null)
                throw new InvalidOperationException("No file loaded.");
            if (CurrentData == null)
                throw new InvalidOperationException("Current data not available.");

            return [.. CurrentData.Columns
                .Cast<DataColumn>()
                .Select((col, index) => new ColumnReference
                {
                    Id = col.ColumnName,
                    Name = col.ColumnName,
                    Index = index
                })];
        }

        public void SortData(string columnName, bool ascending)
        {
            if (CurrentFile == null)
                throw new InvalidOperationException("No file loaded.");
            SavePreviousState();

            SortDataCore(columnName, ascending);

            CurrentFile.SortedColumn = columnName;
            CurrentFile.SortAscending = ascending;
        }

        private void SortDataCore(string columnName, bool ascending)
        {
            if (!CurrentData.Columns.Contains(columnName))
                throw new ArgumentException($"Column '{columnName}' does not exist.");

            string direction = ascending ? "ASC" : "DESC";

            DataView view = CurrentData.DefaultView;
            view.Sort = $"{columnName} {direction}";

            CurrentFile!.CurrentData = view.ToTable();
        }

        public void ApplyFormulaToColumn(string columnName, string formula)
        {
            if (CurrentFile == null)
                throw new InvalidOperationException("No file loaded.");

            if (!CurrentData.Columns.Contains(columnName))
                throw new ArgumentException($"Column '{columnName}' does not exist.");

            SavePreviousState();

            ApplyFormulaToColumnCore(columnName, formula);

            if (!IsApplyingMapping)
            {
                mappingEngine.AddFormulaStep(
                    columnName,
                    formula);
            }
        }

        public void ApplyFormulaToColumnCore(string columnName, string formula)
        {
            if (CurrentFile == null)
                throw new InvalidOperationException("No file loaded.");

            if (!CurrentData.Columns.Contains(columnName))
                throw new ArgumentException(
                    $"Column '{columnName}' does not exist.");


            var tokenizer = new FormulaService();


            var tokens = FormulaService.Tokenize(formula);


            var formulaTree = tokenizer.Parse(tokens);


            ApplyFormula(
                CurrentData,
                columnName,
                formulaTree);

        }

        private void ApplyFormula(DataTable dataTable, string targetColumn, FormulaNode formulaTree)
        {
            foreach (DataRow row in dataTable.Rows)
            {
                double result = formulaService.Evaluate(formulaTree, row);

                row[targetColumn] = result;
            }
        }

        public void SetColumnDataType(string columnName, Type dataType)
        {
            if (CurrentFile == null)
                throw new InvalidOperationException("No file loaded.");
            if (!CurrentData.Columns.Contains(columnName))
                throw new ArgumentException($"Column '{columnName}' does not exist.");

            SavePreviousState();

            SetColumnDataTypeCore(columnName, dataType);

            if (!IsApplyingMapping)
            {
                mappingEngine.AddSetColumnDataTypeStep(columnName, dataType);
            }
        }

        private void SetColumnDataTypeCore(string columnName, Type dataType)
        {
            if (!CurrentData.Columns.Contains(columnName))
                throw new ArgumentException($"Column '{columnName}' does not exist.");

            DataColumn oldColumn = CurrentData.Columns[columnName]!;
            int ordinal = oldColumn.Ordinal;

            // Izveido jaunu kolonnu ar vajadzīgo datu tipu
            DataColumn newColumn = new(columnName + "_tmp", dataType);

            CurrentData.Columns.Add(newColumn);

            // Pārkopē un konvertē datus
            foreach (DataRow row in CurrentData.Rows)
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
            CurrentData.Columns.Remove(oldColumn);

            // Pārsauc jauno kolonnu un atgriež sākotnējā vietā
            newColumn.ColumnName = columnName;
            newColumn.SetOrdinal(ordinal);
        }
    }
}
