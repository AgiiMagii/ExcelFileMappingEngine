using FileMappingEngine.Lib.Models;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data.Common;
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

        public async Task SaveMappingToDb(long userId, long fileId, string name, string json)
        {
            using var connection = await _dbConnFactory.CreateConnectionAsync();
            using var command = (DbCommand)connection.CreateCommand();

            command.CommandText = "INSERT INTO mappings (user_id, file_id, name, data) VALUES (@UserId, @FileId, @Name, @MappingJson)";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@UserId";
            parameter.Value = userId;
            command.Parameters.Add(parameter);
            parameter = command.CreateParameter();
            parameter.ParameterName = "@FileId";
            parameter.Value = fileId;
            command.Parameters.Add(parameter);
            parameter = command.CreateParameter();
            parameter.ParameterName = "@Name";
            parameter.Value = name;
            command.Parameters.Add(parameter);
            parameter = command.CreateParameter();
            parameter.ParameterName = "@MappingJson";
            parameter.Value = json;
            ((NpgsqlParameter)parameter).NpgsqlDbType = NpgsqlDbType.Jsonb;
            command.Parameters.Add(parameter);

            await command.ExecuteNonQueryAsync();       
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
