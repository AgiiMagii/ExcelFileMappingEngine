using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FileMappingEngine.Lib.Database.Repositories
{
    public class MappingRepository
    {
        private readonly DbConnFactory _dbConnFactory;

        public MappingRepository(DbConnFactory dbConnFactory)
        {
            _dbConnFactory = dbConnFactory;
        }

        public string[] GetAllMappingFiles()
        {
            var folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MappingSets");

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            return Directory.GetFiles(folderPath, "*.json");
        }

    }
}
