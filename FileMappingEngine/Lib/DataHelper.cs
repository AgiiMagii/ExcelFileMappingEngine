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
        public static void SetCellValue(IXLCell cell, object? value)
        {
            if (value == null || value == DBNull.Value)
            {
                cell.Value = "";
                return;
            }

            switch (value)
            {
                case string s:
                    cell.Value = s;
                    break;

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
                    cell.Value = value.ToString();
                    break;
            }
        }
    }
}
