using Dapper;
using FileMappingEngine.Lib.Database.Entities;
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

        public async Task<long> AddMappingAsync(MappingEntity mapping)
        {
            using var connection = await _dbConnFactory.CreateConnectionAsync();
            var sql = @"
                INSERT INTO mappings (user_id, file_id, name, data)
                VALUES (@UserId, @FileId, @Name, @Data::jsonb)
                RETURNING id;";

            return await connection.ExecuteScalarAsync<long>(sql, mapping);
        }

        public async Task<List<MappingEntity>> GetAllMappingNamesAsync()
        {
            using var connection = await _dbConnFactory.CreateConnectionAsync();

            var sql = @"SELECT id, name FROM mappings;";

            var result = await connection.QueryAsync<MappingEntity>(sql);

            return result.ToList();
        }

        public async Task<MappingEntity?> GetMappingByIdAsync(long id)
        {
            using var connection = await _dbConnFactory.CreateConnectionAsync();
            var sql = "SELECT * FROM mappings WHERE id = @Id";
            return await connection.QuerySingleOrDefaultAsync<MappingEntity>(sql, new { Id = id });
        }
    }
}
