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
        private DataSession? CurrentSession { get; set; }

        public DataSession Session => CurrentSession ?? throw new InvalidOperationException("No file loaded.");
        public DataTable? CurrentData => Session.Data?.CurrentData;

        public string? CurrentFileName => Session.File?.FileName;
        public bool IsSorted => Session.Data?.SortedColumn != null;
        public bool HasFile => CurrentSession != null;

        public AppManager(FileService fileService, DataService dataService, MappingRepository mappingRepository, MappingService mappingService)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _mappingRepository = mappingRepository ?? throw new ArgumentNullException(nameof(mappingRepository));
            _mappingService = mappingService ?? throw new ArgumentNullException(nameof(mappingService));
        }

        public async Task OpenFile(string path)
        {
            var session = await _fileService.OpenFile(path);

            if (session == null)
                throw new InvalidOperationException("Failed to open the file.");
            if (session.Data == null)
                throw new InvalidOperationException("Data state is not initialized.");

            CurrentSession = session;   // <-- assign BEFORE the DB lookup, not after

            if (session.Data.FileDefinition?.Hash != null)
            {
                await _fileService.FindMatchingFileDefinition(session.Data);
            }
        }

        public void UpdateHeaderRow(int newHeaderRow)
        {
            if (CurrentSession == null)
                throw new InvalidOperationException("No file loaded.");
            if (newHeaderRow < 0)
                throw new ArgumentOutOfRangeException(nameof(newHeaderRow), "Header row index cannot be negative.");
            if (CurrentSession.Data?.RawData?.Data == null)
                throw new InvalidOperationException("Raw data not loaded.");
            _dataService.UpdateHeaderRow(CurrentSession.Data, newHeaderRow);
        }

        public void CloseCurrentFile(DataSession session)
        {
            _fileService.CloseCurrentFile(session);
        }

        public async Task<List<MappingSet>> GetAvailableMappings(DataSession session)
        {
            long fileDefId = session.Data?.FileDefinition?.Id ?? throw new InvalidOperationException("File definition ID is not available.");

            return await _mappingService.GetMappingSetsAsync(fileDefId);
        }

        public void SaveFile(string filePath)
        {
            if (CurrentSession == null)
                throw new InvalidOperationException("No file loaded.");

            _fileService.SaveFile(CurrentSession, filePath);
        }

        public void RemoveColumn(string columnName)
        {
            if (CurrentSession == null)
                throw new InvalidOperationException("No file loaded.");

            _dataService.RemoveColumn(CurrentSession, columnName);
        }

        public void AddColumn(string direction, string anchorId, string? newName)
        {
            if (CurrentSession == null)
                throw new InvalidOperationException("No file loaded.");

            _dataService.AddColumn(CurrentSession, direction, anchorId, newName);
        }

        public async Task SaveMappingSet(string fileDefName, string mappingName)
        {
            if (CurrentSession == null)
                throw new InvalidOperationException("No file loaded.");

            await _mappingService.SaveMappingSet(CurrentSession, fileDefName, mappingName);
        }

        public bool IsColumnNameTaken(string columnName)
        {
            if (CurrentSession == null || CurrentSession.Data == null)
                throw new InvalidOperationException("No file loaded.");

            return _dataService.IsColumnNameTaken(CurrentSession.Data, columnName);
        }

        public void RenameColumn(string oldName, string newName)
        {
            if (CurrentSession == null)
                throw new InvalidOperationException("No file loaded.");

            _dataService.RenameColumn(CurrentSession, oldName, newName);
        }

        public async Task ApplyMappingSetAsync(long id)
        {
            if (CurrentSession == null)
                throw new InvalidOperationException("No file loaded.");

            await _mappingService.ApplyMappingSetAsync(CurrentSession, _dataService, id);
        }

        public void ResetTable()
        {
            if (CurrentSession == null || CurrentSession.Data == null)
                throw new InvalidOperationException("No file loaded.");

            _dataService.ResetTable(CurrentSession.Data);
        }

        public void UndoLastAction()
        {
            if (CurrentSession == null)
                throw new InvalidOperationException("No file loaded.");

            _dataService.UndoLastAction(CurrentSession);
        }

        public void MergeColumns(ColumnReference first, ColumnReference second, string separator, string? resultColumnName)
        {
            if (CurrentSession == null)
                throw new InvalidOperationException("No file loaded.");

            _dataService.MergeColumns(CurrentSession, first, second, separator, resultColumnName);
        }

        public List<ColumnReference> GetDataColumns()
        {
            if (CurrentSession == null)
                throw new InvalidOperationException("No file loaded.");
            if (CurrentSession.Data?.CurrentData == null)
                throw new InvalidOperationException("Current data not available.");

            return [.. CurrentSession.Data.CurrentData.Columns
                .Cast<DataColumn>()
                .Select((col, index) => new ColumnReference
                {
                    Id = col.ColumnName,
                    Name = col.ColumnName,
                    Index = index,
                    
                })];
        }

        public void SortData(string columnName, bool ascending)
        {
            if (CurrentSession == null)
                throw new InvalidOperationException("No file loaded.");

            _dataService.SortData(CurrentSession, columnName, ascending);
        }

        public void ApplyFormulaToColumn(string columnName, string formula)
        {
            if (CurrentSession == null)
                throw new InvalidOperationException("No file loaded.");

            _dataService.ApplyFormulaToColumn(CurrentSession, columnName, formula);
        }

        public void SetColumnDataType(string columnName, Type dataType)
        {
            if (CurrentSession == null)
                throw new InvalidOperationException("No file loaded.");

            _dataService.SetColumnDataType(CurrentSession, columnName, dataType);
        }
    }
}
