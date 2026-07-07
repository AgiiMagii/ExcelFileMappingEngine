using ClosedXML.Attributes;
using ClosedXML.Excel;
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
            DataTable rawData = new();
            List<ColumnReference> columns = [];

            using XLWorkbook workbook = new(filePath);
            IXLWorksheet worksheet = workbook.Worksheet(1);

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
                }

                rawData.Rows.Add(dr);
            }

            return new RawExcelData
            {
                Data = rawData,
                Columns = columns
            };
        }

        public static DataTable BuildDataTable(DataTable rawData, int headerRowIndex = 1)
        {
            DataTable dataTable = new();

            int headerIndex = headerRowIndex - 1;

            if (headerIndex >= rawData.Rows.Count)
                throw new ArgumentException("Invalid header row.");

            DataRow headerRow = rawData.Rows[headerIndex];

            HashSet<string> usedNames = [];

            for (int c = 0; c < rawData.Columns.Count; c++)
            {
                string rawName = headerRow[c]?.ToString()?.Trim() ?? "";

                string colName =
                    GetSafeColumnName(
                        rawName,
                        c + 1,
                        usedNames);

                dataTable.Columns.Add(colName, rawData.Columns[c].DataType);
            }

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
        private static ColumnFormat GetFormat(IXLColumn? column, IXLCell? cell)
        {
            if (column == null && cell == null)
                return ColumnFormat.General;

            int id = column?.Style.NumberFormat.NumberFormatId ?? cell?.Style.NumberFormat.NumberFormatId ?? 0;
            string format = column?.Style.NumberFormat.Format ?? cell?.Style.NumberFormat.Format ?? "";

            switch (id)
            {
                case 0:
                    return ColumnFormat.General;

                case 1:
                case 2:
                case 3:
                case 4:
                    return ColumnFormat.Number;

                case 9:
                case 10:
                    return ColumnFormat.Percentage;

                case 11:
                case 48:
                    return ColumnFormat.Scientific;

                case 12:
                case 13:
                    return ColumnFormat.Fraction;

                case 14:
                    return ColumnFormat.DateShort;

                case 15:
                case 16:
                case 17:
                    return ColumnFormat.DateLong;

                case 18:
                case 19:
                case 20:
                case 21:
                case 45:
                case 46:
                case 47:
                    return ColumnFormat.Time;

                case 22:
                    return ColumnFormat.DateTime;

                case 37:
                case 38:
                case 39:
                case 40:
                    return ColumnFormat.Accounting;

                case 49:
                    return ColumnFormat.Text;
            }

            // Pielāgoti (Custom) formāti
            if (string.IsNullOrWhiteSpace(format))
                return ColumnFormat.General;

            format = format.ToUpperInvariant();

            if (format.Contains('%'))
                return ColumnFormat.Percentage;

            if (format.Contains('€') ||
                format.Contains('$') ||
                format.Contains('£') ||
                format.Contains('¥'))
                return ColumnFormat.Currency;

            if (format.Contains("E+"))
                return ColumnFormat.Scientific;

            if (format.Contains("?/?"))
                return ColumnFormat.Fraction;

            if (format.Contains('@'))
                return ColumnFormat.Text;

            if (format.Contains("DD") ||
                format.Contains("MM") ||
                format.Contains("YY") ||
                format.Contains("HH") ||
                format.Contains("SS"))
            {
                if (format.Contains("HH"))
                    return ColumnFormat.DateTime;

                return ColumnFormat.Date;
            }

            return ColumnFormat.Custom;
        }
        private static Type GetColumnDataType(IXLColumn column)
        {
            bool hasNumber = false;
            bool hasDate = false;
            bool hasText = false;
            bool hasBool = false;


            foreach (var cell in column.CellsUsed())
            {
                if (cell.IsEmpty())
                    continue;


                switch (cell.DataType)
                {
                    case XLDataType.Number:
                        hasNumber = true;
                        break;

                    case XLDataType.DateTime:
                        hasDate = true;
                        break;

                    case XLDataType.Boolean:
                        hasBool = true;
                        break;

                    case XLDataType.Text:
                        hasText = true;
                        break;
                }
            }


            // ja ir teksts, prioritāte tekstam
            if (hasText)
                return typeof(string);

            if (hasDate)
                return typeof(DateTime);

            if (hasNumber)
                return typeof(double);

            if (hasBool)
                return typeof(bool);


            return typeof(string);
        }
    }
}

