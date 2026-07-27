using ClosedXML.Attributes;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using FileMappingEngine.Lib.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using static FileMappingEngine.Lib.Models.Enums;

namespace FileMappingEngine.Lib.Helpers
{
    public static class ExcelHelper
    {
        public static RawExcelData LoadRawData(string filePath)
        {
            RawExcelData rawExcelData = new RawExcelData();
            DataTable rawData = new();
            List<ColumnReference> columns = [];
            List<CellReference> cellMetadata = [];

            XLWorkbook workbook = new(filePath);
            IXLWorksheet worksheet = workbook.Worksheet(1);
            rawExcelData.RawBook = workbook;

            int maxCol = worksheet.LastCellUsed().Address.ColumnNumber;
            var allRows = worksheet.RowsUsed().ToList();

            for (int c = 1; c <= maxCol; c++)
            {
                string columnName = $"Column{c}";

                rawData.Columns.Add(columnName, typeof(object));

                columns.Add(new ColumnReference
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = columnName,
                    Index = c - 1,
                    ExcelLetter = XLHelper.GetColumnLetterFromNumber(c)
                });
            }

            foreach (var row in allRows)
            {
                DataRow dr = rawData.NewRow();

                for (int c = 1; c <= maxCol; c++)
                {
                    var cell = row.Cell(c);

                    dr[c - 1] = GetCellValue(row.Cell(c));
                    if (cell.HasHyperlink)
                    {
                        var hyperlink = cell.GetHyperlink();
                        cellMetadata.Add(new CellReference
                        {
                            RowIndex = row.RowNumber() - 1,
                            ColumnId = columns[c - 1].Id,
                            Hyperlink = hyperlink?.ExternalAddress?.ToString() ?? hyperlink?.InternalAddress?.ToString()
                        });
                    }
                }

                rawData.Rows.Add(dr);
            }

            rawExcelData.Data = rawData;
            rawExcelData.Columns = columns;
            rawExcelData.Cells = cellMetadata;

            return rawExcelData;
        }

        public static void BuildCurrentData(DataState dataState)
        {
            DataTable dataTable = new();
            FileDefinition fileDefinition = new FileDefinition();

            int headerIndex = dataState.HeaderRowIndex - 1;

            if (headerIndex >= dataState?.RawData?.Data?.Rows.Count)
                throw new ArgumentException("Invalid header row.");

            IXLWorkbook? workbook = dataState?.RawData?.RawBook;

            DataRow? headerRow = dataState?.RawData?.Data?.Rows[headerIndex];

            HashSet<string> usedNames = [];

            for (int c = 0; c < dataState?.RawData?.Data?.Columns.Count; c++)
            {
                string rawName = headerRow?[c]?.ToString()?.Trim() ?? "";

                string colName =
                    GetSafeColumnName(
                        rawName,
                        c + 1,
                        usedNames);


                dataTable.Columns.Add(colName, dataState.RawData.Data.Columns[c].DataType);
                fileDefinition?.Columns?.Add(new ColumnData
                {
                    Name = colName
                });
            }

            for (int r = headerIndex + 1; r < dataState?.RawData?.Data?.Rows.Count; r++)
            {
                DataRow newRow = dataTable.NewRow();

                for (int c = 0; c < dataState?.RawData?.Data?.Columns.Count; c++)
                {
                    newRow[c] = dataState?.RawData?.Data?.Rows[r]?[c];
                }

                dataTable.Rows.Add(newRow);
            }
            dataState?.CurrentData = dataTable;
            dataState?.FileDefinition = fileDefinition;
            dataState?.Workbook = workbook;
        }
        private static string GetSafeColumnName(string rawName, int index, HashSet<string> usedNames)
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

        public static void SaveFile(string filePath, DataTable dt, List<List<string>> ignoredRows = null)
        {
            ignoredRows ??= [];

            using XLWorkbook workbook = new();
            var ws = workbook.Worksheets.Add("Sheet1");

            int currentRow = 1;

            if (ignoredRows != null)
            {
                foreach (var row in ignoredRows)
                {
                    for (int c = 0; c < row.Count; c++)
                        ws.Cell(currentRow, c + 1).Value = row[c];
                    currentRow++;
                }
            }

            for (int c = 0; c < dt.Columns.Count; c++)
                ws.Cell(currentRow, c + 1).Value = dt.Columns[c].ColumnName;
            currentRow++;

            foreach (DataRow dr in dt.Rows)
            {
                for (int c = 0; c < dt.Columns.Count; c++)
                    DataHelper.SetCellValue(ws.Cell(currentRow, c + 1), dr[c], dt.Columns[c].DataType);
                currentRow++;
            }

            ws.Columns().AdjustToContents();

            workbook.SaveAs(filePath);
        }

        public static void SaveExcelFile(string filePath, IXLWorkbook workbook)
        {
            workbook.SaveAs(filePath);
        }

        private static object GetCellValue(IXLCell cell)
        {
            if (cell.IsEmpty())
                return DBNull.Value;

            return cell.DataType switch
            {
                XLDataType.Number => decimal.TryParse(cell.Value.ToString(), out var dec)
                    ? dec
                    : 0m,
                XLDataType.DateTime => cell.GetDateTime(),
                XLDataType.Boolean => cell.GetBoolean(),
                XLDataType.Text => cell.GetString(),
                _ => DBNull.Value
            };
        }
        
        public static IXLAddress GetColumnAddressByHeaderRow(IXLWorksheet worksheet, int headerRowIndex, string columnName)
        {
            var headerRow = worksheet.Row(headerRowIndex);
            foreach (var cell in headerRow.CellsUsed())
            {
                if (cell.GetString().Trim().Equals(columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return cell.Address;
                }
            }
            throw new ArgumentException($"Column '{columnName}' not found in header row {headerRowIndex}.");
        }
    }
}

