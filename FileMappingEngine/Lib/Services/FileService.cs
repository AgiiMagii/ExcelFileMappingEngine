using FileMappingEngine.Lib.Database.Entities;
using FileMappingEngine.Lib.Database.Repositories;
using FileMappingEngine.Lib.Helpers;
using FileMappingEngine.Lib.Models;
using FileMappingEngine.Lib.Sessions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace FileMappingEngine.Lib.Services
{
    public class FileService
    {
        private readonly FileRepository fileRepository;

        public FileService(FileRepository fileRepository)
        {
            this.fileRepository = fileRepository;
        }
        public async Task<DataSession> OpenFile(string path, int? headerRowIndex)
        {
            RawExcelData rawData = ExcelHelper.LoadRawData(path);

            if (rawData == null || rawData.Data == null)
                throw new InvalidOperationException("Failed to load data from the specified file.");

            FileState fileState = new FileState
            {
                FilePath = path,
                FileName = Path.GetFileName(path)
            };

            DataState dataState = new DataState
            {
                RawData = rawData,
                HeaderRowIndex = headerRowIndex ?? 1,
            };

            ExcelHelper.BuildCurrentData(dataState);
            dataState.FileDefinition.Hash = DataHelper.CreateHash(dataState.FileDefinition.Columns);

            var session = new DataSession
            {
                File = fileState,
                Data = dataState
            };

            return session;
        }

        public async Task FindMatchingFileDefinition(DataState dataState)
        {
            if (dataState == null)
                throw new ArgumentNullException(nameof(dataState));

            if (dataState.FileDefinition == null || dataState.FileDefinition.Hash == null)
                throw new InvalidOperationException("File definition hash is null.");

            FileEntity? fileDefinition =
                await fileRepository.GetFileDefinitionByHashAsync(dataState.FileDefinition.Hash);

            if (fileDefinition != null)
            {
                dataState.FileDefinition.Id = fileDefinition.Id;
                dataState.FileDefinition.Name = fileDefinition.Name;
            }
        }
        public void CloseCurrentFile(DataSession session)
        {
            ArgumentNullException.ThrowIfNull(session);
            session.File = null;
            session.Data = null;
        }
        public void SaveFile(DataSession session, string path, List<List<string>>? ignoredRows = null)
        {
            ArgumentNullException.ThrowIfNull(session);
            if (session.Data?.CurrentData == null)
                throw new InvalidOperationException("No data to save.");
            ExcelHelper.SaveFile(path, session.Data.CurrentData, ignoredRows?? []);
        }
    }
}
