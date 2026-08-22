using ClosedXML.Attributes;
using ClosedXML.Excel;
using ClosedXML.Parser;
using DocumentFormat.OpenXml.Spreadsheet;
using FileMappingEngine.Lib.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
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

            byte[] originalBytes = File.ReadAllBytes(filePath);
            rawExcelData.OriginalBytes = originalBytes;

            using var ms = new MemoryStream(originalBytes);
            using XLWorkbook workbook = new(ms);

            IXLWorksheet worksheet = workbook.Worksheet(1);

            if (worksheet.CellsUsed().Count() == 0)
                throw new ArgumentException("The Excel file is empty.");

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
            FileDefinition fileDefinition = dataState?.FileDefinition ?? new FileDefinition();
            fileDefinition.Columns = new List<ColumnData>();

            int headerIndex = dataState.HeaderRowIndex - 1;

            if (headerIndex >= dataState?.RawData?.Data?.Rows.Count)
                throw new ArgumentException("Invalid header row.");

            dataState.Workbook?.Dispose();

            IXLWorkbook? workbook = null;
            if (dataState?.RawData?.OriginalBytes != null)
            {
                var ms = new MemoryStream(dataState.RawData.OriginalBytes);
                workbook = new XLWorkbook(ms);
            }

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

                workbook?.Worksheet(1)?.Cell(headerIndex + 1, c + 1).SetValue(colName);


                dataTable.Columns.Add(colName, dataState.RawData.Data.Columns[c].DataType);
                fileDefinition?.Columns?.Add(new ColumnData
                {
                    Name = colName,
                    OriginalName = rawName,

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

            //string safeName = Regex.Replace(rawName, @"[^\w]", "_");

            string baseName = rawName;
            int suffix = 1;

            while (usedNames.Contains(rawName))
            {
                rawName = $"{baseName}_{suffix}";
                suffix++;
            }

            usedNames.Add(rawName);

            return rawName;

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

        public static IXLRange GetDataRangeAfterHeader(IXLWorksheet worksheet, int headerRowIndex)
        {
            int firstDataRow = headerRowIndex + 1;
            int lastRow = worksheet.LastRowUsed().RowNumber();
            int lastColumn = worksheet.LastColumnUsed().ColumnNumber();

            return worksheet.Range(
                firstDataRow,
                1,
                lastRow,
                lastColumn);
        }

        public static IXLRange GetDataRangeForColumn(IXLWorksheet worksheet, int headerRowIndex, IXLAddress columnAddress)
        {
            int firstDataRow = headerRowIndex + 1;
            int lastRow = worksheet.LastRowUsed().RowNumber();

            return worksheet.Range(
                firstDataRow,
                columnAddress.ColumnNumber,
                lastRow,
                columnAddress.ColumnNumber);
        }

    }
}

