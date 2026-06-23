using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Spreadsheet;
using FileMappingEngine.Lib.Models;
using FileMappingEngine.Lib.Services;
using System;
using System.Collections.Generic;
using System.Data;
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
        private readonly ExcelService excelService = new ExcelService();
        private readonly MappingService mappingEngine = new MappingService();
        public FileState? CurrentFile { get; private set; }
        public DataTable CurrentData => CurrentFile?.CurrentData ?? throw new InvalidOperationException("No file loaded.");

        private bool IsApplyingMapping { get; set; }

        public DataTable OpenFile(string filePath)
        {
            CurrentFile = new FileState
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath)
            };

            CurrentFile.RawData = excelService.LoadRawData(filePath);

            if (CurrentFile.RawData?.Data == null)
                throw new InvalidOperationException("Failed to load data from file.");

            CurrentFile.CurrentData = excelService.BuildDataTable(CurrentFile.RawData.Data, CurrentFile.HeaderRowIndex);
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
            CurrentFile.CurrentData = excelService.BuildDataTable(CurrentFile.RawData.Data, CurrentFile.HeaderRowIndex);
        }

        public void CloseCurrentFile()
        {
            CurrentFile = null;
        }

        public string[] GetExistingMappings()
        {
            try
            {
                return mappingEngine.GetJsonFiles();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving existing mappings: {ex.Message}");
                return Array.Empty<string>();
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
                ?? new();

            DataTable dataToSave = CurrentFile.CurrentData.Copy();

            for (int i = 0; i < CurrentFile.HeaderRowIndex - 1; i++)
            {
                dataToSave.Rows.RemoveAt(0);
            }

            excelService.SaveFile(
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

        private string AddColumnCore(string direction, string anchorId, string? newName)
        {
            string newColumnName = newName ?? GenerateColumnName();

            int index = CalculateColumnIndex(anchorId, direction);

            CurrentData.Columns.Add(newColumnName);
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
            var mapping = JsonSerializer.Deserialize<MappingSet>(json);

            if (mapping == null)
                throw new InvalidOperationException("Failed to deserialize mapping set.");
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
                        RemoveColumnCore(step.ColumnId);
                        break;
                    case "AddColumn":
                        string anchorId = step.Parameters["AnchorColumnId"].ToString() ?? throw new InvalidOperationException("Anchor column ID missing.");
                        string direction = step.Parameters["Direction"].ToString() ?? throw new InvalidOperationException("Direction missing.");
                        string? givenName = step.Parameters.ContainsKey("NewName") ? step.Parameters["NewName"].ToString() : null;
                        AddColumnCore(direction, anchorId, givenName);
                        break;
                    case "RenameColumn":
                        string newName = step.Parameters["NewName"].ToString() ?? throw new InvalidOperationException("New name missing.");
                        RenameColumnCore(step.ColumnId, newName);
                        break;
                    case "MergeColumns":
                        string firstColumnName = step.Parameters["FirstColumn"].ToString() ?? throw new InvalidOperationException("First column name missing.");
                        string secondColumnName = step.Parameters["SecondColumn"].ToString() ?? throw new InvalidOperationException("Second column name missing.");
                        string separator = step.Parameters["Separator"].ToString() ?? throw new InvalidOperationException("Separator missing.");
                        MergeColumns(new ColumnReference { Name = firstColumnName }, new ColumnReference { Name = secondColumnName }, separator, step.ColumnId);
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
            CurrentFile.CurrentData = excelService.BuildDataTable(CurrentFile.RawData.Data, CurrentFile.HeaderRowIndex);
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

        public void MergeColumns(ColumnReference first, ColumnReference second, string separator, string resultColumnName)
        {
            if (CurrentFile == null)
                throw new InvalidOperationException("No file loaded.");
            if (CurrentFile.CurrentData == null)
                throw new InvalidOperationException("Current data not available.");

            SavePreviousState();

            AddColumnCore("right", first.Name, resultColumnName);

            int newColumnIndex = CurrentData.Columns.IndexOf(resultColumnName);
            if (newColumnIndex == -1)
                throw new InvalidOperationException($"Result column '{resultColumnName}' was not added correctly.");

            foreach (DataRow row in CurrentData.Rows)
            {
                string firstValue = row[first.Name]?.ToString() ?? string.Empty;
                string secondValue = row[second.Name]?.ToString() ?? string.Empty;
                row[resultColumnName] = $"{firstValue}{separator}{secondValue}";
            }
            if (!IsApplyingMapping)
            {
                mappingEngine.AddMergeColumnsStep(resultColumnName, first.Name, second.Name, separator);
            }

            RemoveColumnCore(first.Name);
            RemoveColumnCore(second.Name);
        }

        public List<ColumnReference> GetDataColumns()
        {
            if (CurrentFile == null)
                throw new InvalidOperationException("No file loaded.");
            if (CurrentData == null)
                throw new InvalidOperationException("Current data not available.");

            return CurrentData.Columns
                .Cast<DataColumn>()
                .Select((col, index) => new ColumnReference
                {
                    Id = col.ColumnName,
                    Name = col.ColumnName,
                    Index = index
                })
                .ToList();
        }
    }
}
