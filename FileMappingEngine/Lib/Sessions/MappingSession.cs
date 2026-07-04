using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using FileMappingEngine.Lib.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Windows.Controls;

namespace FileMappingEngine.Lib.Sessions
{
    public class MappingSession
    {
        private readonly List<ActionStep> steps = [];
        private readonly List<ActionStep> previousSteps = [];

        public void RemoveColumnStep(string columnName)
        {
            steps.Add(new ActionStep
            {
                ActionType = "DeleteColumn",
                ColumnId = columnName,
                Order = steps.Count + 1
            });
        }

        public void AddNewColumnStep(string newColumnName, string anchorId, string direction)
        {
            steps.Add(new ActionStep
            {
                ActionType = "AddColumn",
                ColumnId = newColumnName,
                Order = steps.Count + 1,
                Parameters = new Dictionary<string, object>
                {
                    ["AnchorColumnId"] = anchorId,
                    ["Direction"] = direction
                }
            });
        }

        public void RenameColumnStep(string oldName, string newName)
        {
            steps.Add(new ActionStep
            {
                ActionType = "RenameColumn",
                ColumnId = oldName,
                Order = steps.Count + 1,
                Parameters = new Dictionary<string, object>
                {
                    ["NewName"] = newName
                }
            });
        }

        public MappingSet GenerateMappingSet(string fileName, int headerRow)
        {
            MappingSet mapping = new()
            {
                Name = fileName,
                HeaderRow = headerRow,
                Steps = steps
            };
            return mapping;
        }

        public void ClearSteps()
        {
            steps.Clear();
        }

        public void SavePreviousSteps()
        {
            previousSteps.Clear();
            previousSteps.AddRange(steps);
        }

        public void UndoLastStep()
        {
            if (steps.Count > 0)
            {
                steps.RemoveAt(steps.Count - 1);
            }
        }

        public void AddMergeColumnsStep(string newColumnName, string firstColumn, string secondColumn, string separator)
        {
            steps.Add(new ActionStep
            {
                ActionType = "MergeColumns",
                ColumnId = newColumnName,
                Order = steps.Count + 1,
                Parameters = new Dictionary<string, object>
                {
                    ["FirstColumn"] = firstColumn,
                    ["SecondColumn"] = secondColumn,
                    ["Separator"] = separator
                }
            });
        }

        public void AddSortStep(string columnName, bool sortOrder)
        {
            steps.Add(new ActionStep
            {
                ActionType = "Sort",
                ColumnId = columnName,
                Order = steps.Count + 1,
                Parameters = new Dictionary<string, object>
                {
                    ["Ascending"] = sortOrder
                }
            });
        }

        public void AddFormulaStep(string columnName, string formula)
        {
            steps.Add(new ActionStep
            {
                ActionType = "Formula",
                ColumnId = columnName,
                Order = steps.Count + 1,
                Parameters = new Dictionary<string, object>
                {
                    ["Formula"] = formula
                }
            });
        }

        public void AddSetColumnDataTypeStep(string columnName, Type dataType)
        {
            steps.Add(new ActionStep
            {
                ActionType = "SetColumnDataType",
                ColumnId = columnName,
                Order = steps.Count + 1,
                Parameters = new Dictionary<string, object>
                {
                    ["DataType"] = dataType.FullName ?? string.Empty,
                }
            });
        }
    }
}
