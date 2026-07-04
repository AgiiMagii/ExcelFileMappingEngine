using FileMappingEngine.Lib.Helpers;
using FileMappingEngine.Lib.Models;
using FileMappingEngine.Lib.Sessions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FileMappingEngine.Lib.Services
{
    public class FileService
    {
        public DataSession OpenFile(string path)
        {
            RawExcelData rawData = ExcelHelper.LoadRawData(path);

            FileState fileState = new FileState
            {
                FilePath = path,
                FileName = Path.GetFileName(path)
            };

            DataState dataState = new DataState
            {
                RawData = rawData,
                HeaderRowIndex = 1,
                CurrentData = ExcelHelper.BuildDataTable(rawData.Data, 1)
            };

            return new DataSession
            {
                File = fileState,
                Data = dataState
            };
        }
        public void CloseCurrentFile(DataSession session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));
            session.File = null;
            session.Data = null;
        }
        public void SaveFile(DataSession session, string path, List<List<string>>? ignoredRows = null)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));
            if (session.Data?.CurrentData == null)
                throw new InvalidOperationException("No data to save.");
            ExcelHelper.SaveFile(path, session.Data.CurrentData, ignoredRows);
        }
    }
}
