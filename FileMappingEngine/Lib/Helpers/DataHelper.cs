using ClosedXML.Excel;
using FileMappingEngine.Lib.Services;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging.Effects;
using System.Security.Cryptography;
using System.Text;
using static FileMappingEngine.Lib.Models.Enums;

namespace FileMappingEngine.Lib.Helpers
{
    public static class DataHelper
    {
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
                    cell.Value = Convert.ToDecimal(d);
                    break;

                case decimal d:
                    cell.Value = d;
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

        public static string CreateHash(object value)
        {
            string json = JsonService.CreateJson(value);
            return CreateHashFromString(json);
        }

        public static string CreateHashFromString(string value)
        {
            using SHA256 sha = SHA256.Create();

            byte[] bytes = Encoding.UTF8.GetBytes(value);
            byte[] hash = sha.ComputeHash(bytes);

            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        public static Type GetSystemType(DataType dataType)
        {
            return dataType switch
            {
                DataType.Text => typeof(string),
                DataType.Number => typeof(double),
                DataType.Date => typeof(DateTime),
                DataType.Boolean => typeof(bool),

                _ => typeof(object)
            };
        }
    }
}
