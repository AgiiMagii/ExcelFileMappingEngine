using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using FileMappingEngine.Lib.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Windows.Controls;

namespace FileMappingEngine.Lib
{
    public class MappingEngine
    {
        private readonly List<ActionStep> steps = new List<ActionStep>();
        public string[] GetJsonFiles()
        {
            string folderPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MappingSets");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);
            string[] jsonFiles = Directory.GetFiles(folderPath, "*.json");
            return jsonFiles;
        }
        public void RemoveColumn(System.Data.DataTable data, string columnName)
        {
            if (data.Columns.Contains(columnName))
                data.Columns.Remove(columnName);

            steps.Add(new ActionStep
            {
                ActionType = "DeleteColumn",
                ColumnId = columnName,
                Order = steps.Count + 1
            });
        }
        public void AddNewColumn(string newColumnName, string anchorId, string direction)
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

        public MappingSet GenerateMappingSet(string fileName, int headerRow)
        {
            MappingSet mapping = new MappingSet
            {
                Name = fileName,
                HeaderRow = headerRow,
                Steps = steps
            };
            return mapping;
        }
    }
}
