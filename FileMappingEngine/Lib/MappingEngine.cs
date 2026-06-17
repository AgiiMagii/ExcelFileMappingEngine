using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

namespace FileMappingEngine.Lib
{
    public class MappingEngine
    {
        public string[] GetJsonFiles()
        {
            string folderPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MappingSets");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);
            string[] jsonFiles = Directory.GetFiles(folderPath, "*.json");
            return jsonFiles;
        }
    }
}
