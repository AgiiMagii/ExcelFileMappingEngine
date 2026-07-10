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

        public async Task<List<MappingEntity>> GetMappingSetsByFileIdAsync(long fileId)
        {
            using var connection = await _dbConnFactory.CreateConnectionAsync();
            var sql = "SELECT name, id  FROM mappings WHERE file_id = @FileId";
            var result = await connection.QueryAsync<MappingEntity>(sql, new { FileId = fileId });
            return result.ToList();
        }

        public async Task<MappingEntity?> GetMappingByIdAsync(long mappingId)
        {
            using var connection = await _dbConnFactory.CreateConnectionAsync();
            var sql = "SELECT * FROM mappings WHERE id = @MappingId";
            return await connection.QueryFirstOrDefaultAsync<MappingEntity>(sql, new { MappingId = mappingId });
        }
    }
}
