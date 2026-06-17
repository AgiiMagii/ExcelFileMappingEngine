using FileMappingEngine.Lib.Models;
using FileMappingEngine.Lib.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

namespace FileMappingEngine.Lib
{
    public class AppManager
    {
        private readonly ExcelService excelService = new ExcelService();
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
    }
}
