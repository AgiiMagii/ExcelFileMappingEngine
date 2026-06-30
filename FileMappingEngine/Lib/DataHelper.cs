using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileMappingEngine.Lib
{
    public static class DataHelper
    {
        public static object GetCellValue(object? value)
        {
            if (value == null || value == DBNull.Value)
                return "";

            return value;
        }
        public static void SetCellValue(IXLCell cell, object? value, Type columnType)
        {
            if (columnType == typeof(string))
            {
                cell.Style.NumberFormat.Format = "@";
                cell.SetValue(value?.ToString() ?? "");
                return;
            }

            if (value == null || value == DBNull.Value)
            {
                cell.Value = "";
                return;
            }

            switch (value)
            {
                case double d:
                    cell.Value = d;
                    break;

                case decimal d:
                    cell.Value = (double)d;
                    break;

                case int i:
                    cell.Value = i;
                    break;

                case bool b:
                    cell.Value = b;
                    break;

                case DateTime dt:
                    cell.Value = dt;
                    break;

                default:
                    cell.SetValue(value.ToString() ?? "");
                    break;
            }
        }
    }
}
