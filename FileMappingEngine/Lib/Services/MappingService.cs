using ClosedXML.Excel;
using FileMappingEngine.Lib.Database.Entities;
using FileMappingEngine.Lib.Database.Repositories;
using FileMappingEngine.Lib.Models;
using FileMappingEngine.Lib.Sessions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace FileMappingEngine.Lib.Services
{
    public class MappingService
    {
        private readonly MappingRepository mappingRepository;

        public MappingService(MappingRepository mappingRepository)
        {
            this.mappingRepository = mappingRepository;
        }
        public async Task SaveMappingSet(DataSession session, string filePath)
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

            string fileName = Path.GetFileNameWithoutExtension(filePath);

            session.MappingSet.Name = fileName;
            session.MappingSet.HeaderRow = session.Data.HeaderRowIndex;

            string json = JsonService.CreateJson(session.MappingSet, filePath);

            long UserId = 1; // Replace with actual user ID retrieval logic if needed
            long fileId = 1; // Assuming session.File.Id is the correct file ID


            await CreateMapping(UserId, fileId, fileName, json);
            
        }

        public async Task ApplyMappingSetAsync(DataSession session, DataService dataService, long id)
        {
            if (session.File == null)
                throw new InvalidOperationException("No file loaded.");
            if (session.Data == null || session.Data.CurrentData == null)
                throw new InvalidOperationException("Current data not available.");

            //string json = File.ReadAllText(filePath);
            MappingSet mapping = await GetMappingById(id) ?? throw new InvalidOperationException("Mapping set not found.");
            int headerRow = mapping.HeaderRow;

            dataService.UpdateHeaderRow(session.Data, headerRow);

            ExecuteMappingSteps(mapping, session, dataService);
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

        private void ExecuteMappingSteps(MappingSet mapping, DataSession session, DataService dataService)
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
                        dataService.RemoveColumnCore(session.Data, step.ColumnId);
                        break;
                    case "AddColumn":
                        if (step.Parameters == null)
                            throw new InvalidOperationException("Parameters missing for AddColumn action.");
                        string anchorId = step.Parameters["AnchorColumnId"].ToString() ?? throw new InvalidOperationException("Anchor column ID missing.");
                        string direction = step.Parameters["Direction"].ToString() ?? throw new InvalidOperationException("Direction missing.");
                        string? givenName = step.Parameters.TryGetValue("NewName", out object? value) ? value.ToString() : null;
                        Type? type = step.Parameters.TryGetValue("DataType", out object? typeValue) ? Type.GetType(typeValue.ToString() ?? "") : null;

                        dataService.AddColumnCore(session.Data, direction, anchorId, givenName, type);
                        break;
                    case "RenameColumn":
                        if (step.Parameters == null)
                            throw new InvalidOperationException("Parameters missing for RenameColumn action.");
                        if (step.ColumnId == null)
                            throw new InvalidOperationException("Column ID missing for RenameColumn action.");
                        string newName = step.Parameters["NewName"].ToString() ?? throw new InvalidOperationException("New name missing.");
                        dataService.RenameColumnCore(session.Data, step.ColumnId, newName);
                        break;
                    case "MergeColumns":
                        if (step.Parameters == null)
                            throw new InvalidOperationException("Parameters missing for MergeColumns action.");
                        string firstColumnName = step.ColumnId ?? throw new InvalidOperationException("First column name missing.");
                        string secondColumnName = step.Parameters["SecondColumnId"].ToString() ?? throw new InvalidOperationException("Second column name missing.");
                        string separator = step.Parameters["Separator"].ToString() ?? throw new InvalidOperationException("Separator missing.");
                        string? newColumnName = step.Parameters.TryGetValue("NewName", out object? newNameObj) ? newNameObj.ToString() : step.ColumnId;
                        dataService.MergeColumns(session, new ColumnReference { Name = firstColumnName }, new ColumnReference { Name = secondColumnName }, separator, newColumnName);
                        break;
                    case "Sort":
                        if (step.Parameters == null)
                            throw new InvalidOperationException("Parameters missing for Sort action.");
                        if (step.ColumnId == null)
                            throw new InvalidOperationException("Column ID missing for Sort action.");
                        bool ascending = ((JsonElement)step.Parameters["Ascending"]).GetBoolean();
                        dataService.SortDataCore(session.Data, step.ColumnId, ascending);
                        break;
                    case "Formula":
                        if (step.Parameters == null)
                            throw new InvalidOperationException("Parameters missing for Formula action.");
                        if (step.ColumnId == null)
                            throw new InvalidOperationException("Column ID missing for Formula action.");
                        string formula = step.Parameters["Formula"].ToString() ?? throw new InvalidOperationException("Formula missing.");
                        dataService.ApplyFormulaToColumnCore(session.Data, step.ColumnId, formula);
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

                        dataService.SetColumnDataTypeCore(session.Data, step.ColumnId, dataType);
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

        public async Task<List<MappingSet>> GetMappingSetsAsync()
        {
            var entities = await mappingRepository.GetAllMappingNamesAsync();

            return entities.Select(e => new MappingSet
            {
                Id = e.Id,
                Name = e.Name
            }).ToList();
        }
    }
}
