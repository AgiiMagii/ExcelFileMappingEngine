using ClosedXML.Excel;
using FileMappingEngine.Lib.Models;
using FileMappingEngine.Lib.Sessions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace FileMappingEngine.Lib.Services
{
    public class MappingService
    {
        private bool IsApplyingMapping { get; set; }
        
        public void SaveMappingSet(DataSession session, string filePath)
        {
            if (session.File == null)
                throw new InvalidOperationException("No file loaded.");

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

            JsonService.CreateJson(session.MappingSet, filePath);
        }
        public void ApplyMappingSet(DataSession session, DataService dataService, string filePath)
        {
            if (session.File == null)
                throw new InvalidOperationException("No file loaded.");
            if (session.Data.CurrentData == null)
                throw new InvalidOperationException("Current data not available.");

            string json = File.ReadAllText(filePath);
            MappingSet mapping = JsonService.CreateObject<MappingSet>(filePath) ?? throw new InvalidOperationException("Failed to load mapping set from JSON.");
            try
            {
                IsApplyingMapping = true;

                int headerRow = mapping.HeaderRow;

                dataService.UpdateHeaderRow(session.Data, headerRow);

                ExecuteMappingSteps(mapping, session, dataService);

            }
            finally
            {
                IsApplyingMapping = false;
            }
        }
        private void ExecuteMappingSteps(MappingSet mapping, DataSession session, DataService dataService)
        {
            foreach (var step in mapping.Steps.OrderBy(s => s.Order))
            {
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
                        string firstColumnName = step.Parameters["FirstColumn"].ToString() ?? throw new InvalidOperationException("First column name missing.");
                        string secondColumnName = step.Parameters["SecondColumn"].ToString() ?? throw new InvalidOperationException("Second column name missing.");
                        string separator = step.Parameters["Separator"].ToString() ?? throw new InvalidOperationException("Separator missing.");
                        dataService.MergeColumns(session, new ColumnReference { Name = firstColumnName }, new ColumnReference { Name = secondColumnName }, separator, step.ColumnId);
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
    }
}
