using FileMappingEngine.Lib.Models;
using FileMappingEngine.Lib.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Text.Json.Nodes;

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
        private readonly MappingEngine mappingEngine = new MappingEngine();
        public FileState? CurrentFile { get; private set; }
        public DataTable CurrentData => CurrentFile?.CurrentData ?? throw new InvalidOperationException("No file loaded.");

        public DataTable OpenFile(string filePath)
        {
            CurrentFile = new FileState
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath)
            };

            CurrentFile.RawData = excelService.LoadRawData(filePath);
            CurrentFile.CurrentData = excelService.BuildDataTable(CurrentFile.RawData, CurrentFile.HeaderRowIndex);

            return CurrentData;
        }
        public void UpdateHeaderRow(int newHeaderRow)
        {
            if (CurrentFile == null)
                throw new InvalidOperationException("No file loaded.");
            if (CurrentFile.RawData == null)
                throw new InvalidOperationException("Raw data not loaded.");
            CurrentFile.HeaderRowIndex = newHeaderRow;
            CurrentFile.CurrentData = excelService.BuildDataTable(CurrentFile.RawData, CurrentFile.HeaderRowIndex);
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
    }
}
