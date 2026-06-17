using ClosedXML.Excel;
using FileMappingEngine.Lib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;

namespace FileMappingEngine.Lib.Services
{
    public class ExcelService
    {
        public List<string[]>? IgnoredRows { get; private set; }

        public DataTable LoadRawData(string filePath)
        {
            DataTable rawData = new DataTable();

            using XLWorkbook workbook = new XLWorkbook(filePath);
            IXLWorksheet worksheet = workbook.Worksheet(1);

            int maxCol = worksheet.LastCellUsed().Address.ColumnNumber;
            var allRows = worksheet.RowsUsed().ToList();

            // Izveido tehniskās kolonnas
            for (int c = 1; c <= maxCol; c++)
            {
                rawData.Columns.Add($"Column{c}");
            }

            // Ielādē visas rindas
            foreach (var row in allRows)
            {
                DataRow dr = rawData.NewRow();

                for (int c = 1; c <= maxCol; c++)
                {
                    var cell = row.Cell(c);
                    dr[c - 1] = cell.Value.IsBlank
                        ? ""
                        : cell.Value.ToString();
                }

                rawData.Rows.Add(dr);
            }

            return rawData;
        }

        public DataTable BuildDataTable(DataTable rawData, int headerRowIndex = 1)
        {
            DataTable dataTable = new DataTable();

            int headerIndex = headerRowIndex - 1;

            if (headerIndex >= rawData.Rows.Count)
                throw new ArgumentException("Invalid header row.");

            // Header rinda
            DataRow headerRow = rawData.Rows[headerIndex];

            HashSet<string> usedNames = new();

            for (int c = 0; c < rawData.Columns.Count; c++)
            {
                string rawName = headerRow[c]?.ToString()?.Trim() ?? "";

                string colName =
                    GetSafeColumnName(
                        dataTable,
                        rawName,
                        c + 1,
                        usedNames);

                dataTable.Columns.Add(colName);
            }

            // Dati zem header
            for (int r = headerIndex + 1; r < rawData.Rows.Count; r++)
            {
                DataRow newRow = dataTable.NewRow();

                for (int c = 0; c < rawData.Columns.Count; c++)
                {
                    newRow[c] = rawData.Rows[r][c];
                }

                dataTable.Rows.Add(newRow);
            }
            return dataTable;
        }

        private string GetSafeColumnName(DataTable dt, string rawName, int index, HashSet<string> usedNames)
        {
            if (string.IsNullOrWhiteSpace(rawName))
                rawName = "Column" + index;

            string safeName = Regex.Replace(rawName, @"[^\w]", "_");

            string baseName = safeName;
            int suffix = 1;

            while (usedNames.Contains(safeName))
            {
                safeName = $"{baseName}_{suffix}";
                suffix++;
            }

            usedNames.Add(safeName);

            return safeName;
            
        }

        public void SaveFile(string filePath, DataTable dt, List<List<string>> ignoredRows)
        {
            using XLWorkbook workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Sheet1");

            int currentRow = 1;

            foreach (var row in ignoredRows)
            {
                for (int c = 0; c < row.Count; c++)
                    ws.Cell(currentRow, c + 1).Value = row[c];
                currentRow++;
            }

            for (int c = 0; c < dt.Columns.Count; c++)
                ws.Cell(currentRow, c + 1).Value = dt.Columns[c].ColumnName;
            currentRow++;

            foreach (DataRow dr in dt.Rows)
            {
                for (int c = 0; c < dt.Columns.Count; c++)
                    ws.Cell(currentRow, c + 1).Value = dr[c]?.ToString() ?? "";
                currentRow++;
            }

            workbook.SaveAs(filePath);
        }
    }
}

