using ClosedXML.Excel;
using FileMappingEngine.Lib.Database.Entities;
using FileMappingEngine.Lib.Database.Repositories;
using FileMappingEngine.Lib.Helpers;
using FileMappingEngine.Lib.Interfaces;
using FileMappingEngine.Lib.Models;
using FileMappingEngine.Lib.Sessions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using static FileMappingEngine.Lib.Models.Enums;

namespace FileMappingEngine.Lib.Services
{
    public class MappingService
    {
        private readonly MappingRepository mappingRepository;
        private readonly FileRepository fileRepository;
        

        public MappingService(MappingRepository mappingRepository, FileRepository fileRepository)
        {
            this.mappingRepository = mappingRepository;
            this.fileRepository = fileRepository;
        }
        public async Task SaveMappingSet(DataSession session, string fileDefName, string mappingName)
        {
            if (session.File == null)
                throw new InvalidOperationException("No file loaded.");
            if (session.Data == null)
                throw new InvalidOperationException("Current data not available.");
            

            if (session.Data.SortedColumn != null &&
                session.Data.SortAscending.HasValue)
            {
                session.MappingSet.Steps.Add(new ActionStep
                {
                    ActionType = "Sort",
                    ColumnId = session.Data.SortedColumn,
                    Order = session.MappingSet.Steps.Count + 1,
                    Parameters = new Dictionary<string, object>
                    {
                        ["Ascending"] = session.Data.SortAscending.Value
                    }
                });
            }

            if (session.Data.FileDefinition?.Id  == null)
            {
                session.Data.FileDefinition?.Id =
                        await CreateFileDefinition(session, fileDefName);
            }

            session.MappingSet.Name = mappingName;
            session.MappingSet.HeaderRow = session.Data.HeaderRowIndex;

            string json = JsonService.CreateJson(session.MappingSet);

             // Replace with actual user ID retrieval logic if needed
            long fileId = session.Data.FileDefinition?.Id ?? 0; // Assuming session.File.Id is the correct file ID
            long userId = 1;

            await CreateMapping(userId, fileId, mappingName, json);
            
        }
        public async Task<long> CreateFileDefinition(DataSession session, string fileDefName)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));
            if (session.Data == null || session.Data.FileDefinition == null)
                throw new InvalidOperationException("Current data not available.");
            if (session.Data.FileDefinition.Columns == null)
                throw new InvalidOperationException("File definition columns not available.");

            string json = JsonService.CreateJson(session.Data.FileDefinition.Columns);
            string hash = DataHelper.CreateHashFromString(json);

            FileEntity fileEntity = new FileEntity
            {
                Name = fileDefName,
                FingerPrint = json,
                FingerprintHash = hash,
            };

            return await fileRepository.AddFileDefinitionAsync(fileEntity);
        }
        public async Task ApplyMappingSetAsync(DataSession session, DataService dataService, long id, IDataTableActionExecutor actionExecutor)
        {
            if (session.File == null)
                throw new InvalidOperationException("No file loaded.");
            if (session.Data == null || session.Data.CurrentData == null)
                throw new InvalidOperationException("Current data not available.");

            MappingSet mapping = await GetMappingById(id) ?? throw new InvalidOperationException("Mapping set not found.");
            session.MappingSet = mapping;

            dataService.UpdateHeaderRow(session.Data, mapping.HeaderRow);

            ExecuteMappingSteps(
                mapping,
                session,
                dataService,
                actionExecutor);

            session.Data.IsMappingApplied = true;
        }
        public async Task<MappingSet?> GetMappingById(long id)
        {
            var entity = await mappingRepository.GetMappingByIdAsync(id);

            if (entity == null)
                return null;
            var dataJson = entity.Data;
            var data = JsonService.CreateObject<MappingSet>(dataJson);

            return new MappingSet
            {
                Id = entity.Id,
                Name = entity.Name,
                HeaderRow = data?.HeaderRow ?? 0,
                Steps = data?.Steps ?? new List<ActionStep>()
            };
        }

        private List<string> PrepareRemoveColumns(ActionStep step)
        {
            if (step.Parameters == null)
                throw new InvalidOperationException("Parameters missing for DeleteColumns action.");

            if (!step.Parameters.TryGetValue("ColumnIds", out object? columnIdsObj))
                throw new InvalidOperationException("Column IDs missing for DeleteColumns action.");

            List<string>? columnIds = null;

            if (columnIdsObj is JsonElement jsonElement)
            {
                columnIds = jsonElement
                    .EnumerateArray()
                    .Select(x => x.GetString()!)
                    .ToList();
            }
            else if (columnIdsObj is IEnumerable<string> strings)
            {
                columnIds = strings.ToList();
            }

            if (columnIds == null)
                throw new InvalidOperationException("Invalid column IDs format.");

            return columnIds;
        }
        private string PrepareRenameColumns(ActionStep step)
        {
            if (step.Parameters == null)
                throw new InvalidOperationException("Parameters missing for RenameColumn action.");
            
            return GetStringParameter(step, "NewName");
        }
        private List<string> PrepareMergeColumns(ActionStep step)
        {
            if (step.Parameters == null)
                throw new InvalidOperationException("Parameters missing for MergeColumns action.");
            string firstColumnName = step.ColumnId ?? throw new InvalidOperationException("First column name missing.");
            string secondColumnName = GetStringParameter(step, "SecondColumnId");
            string separator = GetStringParameter(step, "Separator");
            string? newColumnName = step.Parameters.TryGetValue("NewName", out object? newNameObj) ? newNameObj.ToString() : null;
            return new List<string> { firstColumnName, secondColumnName, separator, newColumnName};
        }

        private string GetStringParameter(ActionStep step, string parameterName)
        {
            if (step.Parameters == null || !step.Parameters.TryGetValue(parameterName, out object? valueObj))
                throw new InvalidOperationException($"{parameterName} parameter is missing.");
            return valueObj?.ToString() ?? throw new InvalidOperationException($"{parameterName} is null.");
        }
        private ColumnDirection GetDirection(ActionStep step)
        {
            if (step.Parameters == null || !step.Parameters.TryGetValue("Direction", out object? directionObj))
                throw new InvalidOperationException("Direction parameter is missing.");
            return Enum.Parse<ColumnDirection>(directionObj?.ToString() ?? throw new InvalidOperationException("Direction is null."));
        }

        private void ExecuteMappingSteps(MappingSet mapping, DataSession session, DataService dataService, IDataTableActionExecutor actionExecutor)
        {
            foreach (var step in mapping.Steps.OrderBy(s => s.Order))
            {
                if (session.Data == null)
                    throw new InvalidOperationException("Current data not available.");

                switch (step.ActionType)
                {
                    case "DeleteColumn":
                        if (step.ColumnId == null)
                            throw new InvalidOperationException("Column ID missing for DeleteColumn action.");
                        actionExecutor.RemoveColumn(session.Data, step.ColumnId);
                        break;
                    case "DeleteColumns":
                        var columnIds = PrepareRemoveColumns(step);
                        actionExecutor.RemoveColumns(session.Data, columnIds);
                        break;
                    case "AddColumn":
                        if (step.Parameters == null)
                            throw new InvalidOperationException("Parameters missing for AddColumn action.");
                        string anchorId = GetStringParameter(step, "AnchorColumnId");
                        string? givenName = step.Parameters.TryGetValue("NewName", out object? value) ? value.ToString() : null;
                        var direction = GetDirection(step);
                        actionExecutor.AddColumn(session.Data, direction, anchorId, givenName);
                        break;
                    case "RenameColumn":
                        if (step.ColumnId == null)
                            throw new InvalidOperationException("Column ID missing for RenameColumn action.");
                        var newName = PrepareRenameColumns(step);
                        actionExecutor.RenameColumn(session.Data, step.ColumnId, newName);
                        break;
                    case "MergeColumns":
                        List<string> parametrs = PrepareMergeColumns(step);
                        actionExecutor.MergeColumns(session, new ColumnReference { Name = parametrs[0] }, new ColumnReference { Name = parametrs[1] }, parametrs[2], parametrs[3]);
                        break;
                    case "Sort":
                        if (step.Parameters == null)
                            throw new InvalidOperationException("Parameters missing for Sort action.");
                        if (step.ColumnId == null)
                            throw new InvalidOperationException("Column ID missing for Sort action.");
                        bool ascending = ((JsonElement)step.Parameters["Ascending"]).GetBoolean();
                        actionExecutor.SortData(session.Data, step.ColumnId, ascending);
                        break;
                    case "Formula":
                        if (step.Parameters == null)
                            throw new InvalidOperationException("Parameters missing for Formula action.");
                        if (step.ColumnId == null)
                            throw new InvalidOperationException("Column ID missing for Formula action.");
                        string formula = step.Parameters["Formula"].ToString() ?? throw new InvalidOperationException("Formula missing.");
                        actionExecutor.ApplyFormulaToColumn(session.Data, step.ColumnId, formula);
                        break;
                    case "SetColumnDataType":
                        continue;
                    default:
                        throw new InvalidOperationException($"Unknown action type: {step.ActionType}");
                }
            }
        }
        private void ExecuteMappingStepsOnWorkbook(DataSession session, IWorkbookActionExecutor actionExecutor)
        {
            foreach (var step in session.MappingSet.Steps.OrderBy(s => s.Order))
            {
                if (session.Data == null)
                    throw new InvalidOperationException("Current data not available.");

                switch (step.ActionType)
                {
                    case "DeleteColumn":
                        if (step.ColumnId == null)
                            throw new InvalidOperationException("Column ID missing for DeleteColumn action.");
                        actionExecutor.RemoveColumn(session.Data, step.ColumnId);
                        break;
                    case "DeleteColumns":
                        var columnIds = PrepareRemoveColumns(step);
                        actionExecutor.RemoveColumns(session.Data, columnIds);
                        break;
                    case "AddColumn":
                        if (step.Parameters == null)
                            throw new InvalidOperationException("Parameters missing for AddColumn action.");
                        string anchorId = GetStringParameter(step, "AnchorColumnId");
                        string name = step.ColumnId ?? throw new InvalidOperationException("Column ID missing for AddColumn action.");
                        var direction = GetDirection(step);
                        actionExecutor.AddColumn(session.Data, direction, anchorId, name);
                        break;
                    case "RenameColumn":
                        if (step.ColumnId == null)
                            throw new InvalidOperationException("Column ID missing for RenameColumn action.");
                        var newName = PrepareRenameColumns(step);
                        actionExecutor.RenameColumn(session.Data, step.ColumnId, newName);
                        break;
                    case "MergeColumns":
                        List<string> parametrs = PrepareMergeColumns(step);
                        actionExecutor.MergeColumns(session, new ColumnReference { Name = parametrs[0] }, new ColumnReference { Name = parametrs[1] }, parametrs[2], parametrs[3]);
                        break;
                    case "Sort":
                        if (step.Parameters == null)
                            throw new InvalidOperationException("Parameters missing for Sort action.");
                        if (step.ColumnId == null)
                            throw new InvalidOperationException("Column ID missing for Sort action.");
                        bool ascending = ((JsonElement)step.Parameters["Ascending"]).GetBoolean();
                        actionExecutor.SortData(session.Data, step.ColumnId, ascending);
                        break;
                    case "Formula":
                        if (step.Parameters == null)
                            throw new InvalidOperationException("Parameters missing for Formula action.");
                        if (step.ColumnId == null)
                            throw new InvalidOperationException("Column ID missing for Formula action.");
                        string formula = step.Parameters["Formula"].ToString() ?? throw new InvalidOperationException("Formula missing.");
                        actionExecutor.ApplyFormulaToColumn(session.Data, step.ColumnId, formula);
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

                        actionExecutor.SetColumnDataType(session.Data, step.ColumnId, dataType);
                        break;

                    default:
                        throw new InvalidOperationException($"Unknown action type: {step.ActionType}");
                }
            }
        }

        private async Task CreateMapping(long userId, long fileId, string name, string json)
        {
            MappingEntity mapping = new MappingEntity
            {
                UserId = userId,
                FileId = fileId,
                Name = name,
                Data = json,
            };

            await mappingRepository.AddMappingAsync(mapping);
        }

        public async Task<List<MappingSet>> GetMappingSetsAsync(long fileId)
        {
            var entities = await mappingRepository.GetMappingSetsByFileIdAsync(fileId);

            return entities.Select(e => new MappingSet
            {
                Id = e.Id,
                Name = e.Name
            }).ToList();
        }

        public bool TransformWorkbook(IXLWorkbook workbook, DataSession session, IWorkbookActionExecutor actionExecutor)
        {
            MappingSet mapping = session.MappingSet ?? throw new InvalidOperationException("Mapping set not available.");
            if (workbook == null)
                throw new ArgumentNullException(nameof(workbook));
            if (mapping == null)
                throw new ArgumentNullException(nameof(mapping));
            //DataState dataState = new DataState
            //{
            //    Workbook = workbook,
            //    HeaderRowIndex = mapping.HeaderRow
            //};
            //dataService.UpdateHeaderRow(dataState, mapping.HeaderRow);
            ExecuteMappingStepsOnWorkbook(session, actionExecutor);
            return true;
        }
    }
}
