using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using DocumentFormat.OpenXml.Spreadsheet;
using FileMappingEngine.Lib.Database.Repositories;
using FileMappingEngine.Lib.Helpers;
using FileMappingEngine.Lib.Models;
using FileMappingEngine.Lib.Services;
using FileMappingEngine.Lib.Sessions;
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
        private readonly MappingService _mappingService;
        private readonly FileService _fileService;
        private readonly DataService _dataService;
        private readonly MappingRepository _mappingRepository;
        private DataSession? _currentSession { get; set; }

        public DataSession Session => _currentSession ?? throw new InvalidOperationException("No file loaded.");

        //public FileState? CurrentFile => CurrentSession?.File;
        //public DataTable CurrentData => CurrentFile?.CurrentData ?? throw new InvalidOperationException("No file loaded.");

        public bool HasFile => _currentSession != null;

        public AppManager(FileService fileService, DataService dataService, MappingRepository mappingRepository, MappingService mappingService)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _mappingRepository = mappingRepository ?? throw new ArgumentNullException(nameof(mappingRepository));
            _mappingService = mappingService ?? throw new ArgumentNullException(nameof(mappingService));
        }

        public void OpenFile(string path)
        {
            _currentSession = _fileService.OpenFile(path);
        }

        public void UpdateHeaderRow(int newHeaderRow)
        {
            if (_currentSession == null)
                throw new InvalidOperationException("No file loaded.");
            if (newHeaderRow < 0)
                throw new ArgumentOutOfRangeException(nameof(newHeaderRow), "Header row index cannot be negative.");
            if (_currentSession.Data?.RawData?.Data == null)
                throw new InvalidOperationException("Raw data not loaded.");
            _dataService.UpdateHeaderRow(_currentSession.Data, newHeaderRow);
        }

        public void CloseCurrentFile(DataSession session)
        {
            _fileService.CloseCurrentFile(session);
        }

        public string[] GetExistingMappings()
        {
            if (_mappingRepository == null)
                throw new InvalidOperationException("Mapping repository not initialized.");
            return _mappingRepository.GetAllMappingFiles();
        }

        public void SaveFile(string filePath)
        {
            if (_currentSession == null)
                throw new InvalidOperationException("No file loaded.");
            _fileService.SaveFile(_currentSession, filePath);
        }

        public void RemoveColumn(string columnName)
        {
            if (_currentSession == null)
                throw new InvalidOperationException("No file loaded.");
            _dataService.RemoveColumn(_currentSession, columnName);
        }

        public void AddColumn(string direction, string anchorId, string? newName)
        {
            if (_currentSession == null)
                throw new InvalidOperationException("No file loaded.");
            _dataService.AddColumn(_currentSession, direction, anchorId, newName);
        }

        public void SaveMappingSet(string filePath)
        {
            _mappingService.SaveMappingSet(_currentSession, filePath);
        }

        public bool IsColumnNameTaken(string columnName)
        {
            return _dataService.IsColumnNameTaken(_currentSession.Data, columnName);
        }

        public void RenameColumn(string oldName, string newName)
        {
            _dataService.RenameColumn(_currentSession, oldName, newName);
        }

        public void ApplyMappingSet(string filePath)
        {
            _mappingService.ApplyMappingSet(_currentSession, _dataService, filePath);
        }

        public void ResetTable()
        {
            _dataService.ResetTable(_currentSession);
        }

        public void UndoLastAction()
        {
            _dataService.UndoLastAction(_currentSession);
        }

        public void MergeColumns(ColumnReference first, ColumnReference second, string separator, string? resultColumnName)
        {
            _dataService.MergeColumns(_currentSession, first, second, separator, resultColumnName);
        }

        public List<ColumnReference> GetDataColumns()
        {
            if (_currentSession == null)
                throw new InvalidOperationException("No file loaded.");
            if (_currentSession.Data?.CurrentData == null)
                throw new InvalidOperationException("Current data not available.");

            return [.. _currentSession.Data.CurrentData.Columns
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
            _dataService.SortData(_currentSession, columnName, ascending);
        }


        public void ApplyFormulaToColumn(string columnName, string formula)
        {
            _dataService.ApplyFormulaToColumn(_currentSession, columnName, formula);
        }


        public void SetColumnDataType(string columnName, Type dataType)
        {
            _dataService.SetColumnDataType(_currentSession, columnName, dataType);
        }
    }
}
