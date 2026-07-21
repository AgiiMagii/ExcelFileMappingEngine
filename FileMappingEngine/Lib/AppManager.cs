using FileMappingEngine.Lib.Database.Repositories;
using FileMappingEngine.Lib.Helpers;
using FileMappingEngine.Lib.Models;
using FileMappingEngine.Lib.Services;
using FileMappingEngine.Lib.Sessions;
using System.Data;
using static FileMappingEngine.Lib.Models.Enums;

namespace FileMappingEngine.Lib
{
    public class AppManager
    {
        private readonly MappingService _mappingService;
        private readonly FileService _fileService;
        private readonly DataService _dataService;
        private readonly MappingRepository _mappingRepository;
        private DataSession? CurrentSession { get; set; }

        public DataSession Session => CurrentSession ?? throw new InvalidOperationException("No file loaded.");
        public DataTable? CurrentData => DataState.CurrentData;
        public DataState DataState => Session.Data ?? throw new InvalidOperationException("No data state available.");

        public string? CurrentFileName => Session.File?.FileName;
        public bool? SortAscending => DataState.SortAscending;
        public string? SortColumn => DataState.SortedColumn;
        public bool HasFile => CurrentSession != null;
        public bool IsMappingApplied => DataState.IsMappingApplied;

        public AppManager(FileService fileService, DataService dataService, MappingRepository mappingRepository, MappingService mappingService)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _mappingRepository = mappingRepository ?? throw new ArgumentNullException(nameof(mappingRepository));
            _mappingService = mappingService ?? throw new ArgumentNullException(nameof(mappingService));
        }

        public async Task OpenFile(string path, int? headerRowIndex)
        {
            var session = await _fileService.OpenFile(path, headerRowIndex);

            if (session == null)
                throw new InvalidOperationException("Failed to open the file.");
            if (session.Data == null)
                throw new InvalidOperationException("Data state is not initialized.");

            CurrentSession = session;

            if (session.Data.FileDefinition?.Hash != null)
            {
                await _fileService.FindMatchingFileDefinition(session.Data);
            }
        }

        public async Task UpdateHeaderRow(int newHeaderRow)
        {
            if (newHeaderRow < 0)
                throw new ArgumentOutOfRangeException(nameof(newHeaderRow), "Header row index cannot be negative.");
            if (Session.Data?.RawData?.Data == null)
                throw new InvalidOperationException("Raw data not loaded.");
            _dataService.UpdateHeaderRow(Session.Data, newHeaderRow);

            if (Session.Data.FileDefinition?.Hash != null)
            {
                await _fileService.FindMatchingFileDefinition(Session.Data);
            }
        }

        public void CloseCurrentFile()
        {
            _fileService.CloseCurrentFile(Session);
            CurrentSession = null;
        }

        public async Task<List<MappingSet>> GetAvailableMappings(DataSession session)
        {
            long? fileDefId = session.Data?.FileDefinition?.Id;

            if (fileDefId == null)
                return new List<MappingSet>();

            return await _mappingService.GetMappingSetsAsync(fileDefId.Value);
        }

        public string? GetFileDefinitionName()
        {
            return Session?.Data?.FileDefinition?.Name
                ?? Session?.File?.FileName
                ?? "New File Type";
        }

        public void SaveFile(string filePath)
        {
            _fileService.SaveFile(Session, filePath);
        }

        public void RemoveColumn(string columnName)
        {
            _dataService.RemoveColumn(Session, columnName);
        }

        public void RemoveColumns(IEnumerable<string> columnNames)
        {
            _dataService.RemoveColumns(Session, columnNames);
        }

        public void AddColumn(ColumnDirection direction, string anchorId, string? newName)
        {
            _dataService.AddColumn(Session, direction, anchorId, newName);
        }

        public async Task SaveMappingSet(string fileDefName, string mappingName)
        {
            await _mappingService.SaveMappingSet(Session, fileDefName, mappingName);
        }

        public bool IsColumnNameTaken(string columnName)
        {
            return _dataService.IsColumnNameTaken(DataState, columnName);
        }

        public void RenameColumn(string oldName, string newName)
        {
            _dataService.RenameColumn(Session, oldName, newName);
        }

        public async Task ApplyMappingSetAsync(long id)
        {
            await _mappingService.ApplyMappingSetAsync(Session, _dataService, id);
        }

        public void ResetTable()
        {
            _dataService.ResetTable(DataState);
        }

        public void ClearCurrentMapping()
        {
            _dataService.SetIsAppliedMappingFalse(DataState);
        }

        public void UndoLastAction()
        {
            _dataService.UndoLastAction(Session);
        }

        public void MergeColumns(ColumnReference first, ColumnReference second, string separator, string? resultColumnName)
        {
            _dataService.MergeColumns(Session, first, second, separator, resultColumnName);
        }

        public List<ColumnReference> GetDataColumns()
        {
            if (CurrentData == null)
                return [];

            return [.. CurrentData.Columns
                .Cast<DataColumn>()
                .Select((col, index) => new ColumnReference
                {
                    Id = col.ColumnName,
                    Name = col.ColumnName,
                    Index = index,
                    
                })];
        }

        public int GetColumnCount()
        {
            if (CurrentData == null)
                return 0;

            return CurrentData.Columns.Count;
        }

        public void SortData(string columnName, bool ascending)
        {
            _dataService.SortData(Session, columnName, ascending);
        }

        public void ApplyFormulaToColumn(string columnName, string formula)
        {
            _dataService.ApplyFormulaToColumn(Session, columnName, formula);
        }

        public void SetColumnDataType(string columnName, DataType dataType)
        {
            Type systemType = DataHelper.GetSystemType(dataType);

            _dataService.SetColumnDataType(Session, columnName, systemType);
        }
    }
}
