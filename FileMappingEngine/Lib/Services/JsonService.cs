using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using IOFile = System.IO.File;

namespace FileMappingEngine.Lib.Services
{
    public class JsonService
    {
        public static string CreateJson<Entity>(Entity entity, string saveDir) where Entity : class
        {
            try
            {
                string jsonContent = JsonSerializer.Serialize(entity, new JsonSerializerOptions { WriteIndented = true });
                IOFile.WriteAllText(saveDir, jsonContent);
                return jsonContent;
            }
            catch
            {
                return string.Empty;
            }
        }
        //public static T? CreateObject<T>(string filePath) where T : class
        //{
        //    try
        //    {
        //        string content = IOFile.ReadAllText(filePath);
        //        return JsonSerializer.Deserialize<T>(content);
        //    }
        //    catch
        //    {
        //        return null;
        //    }

        //}
        public static T? CreateObject<T>(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(json);
            }
            catch
            {
                return default;
            }
        }

    }
}
