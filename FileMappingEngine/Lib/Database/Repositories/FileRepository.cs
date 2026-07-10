using Dapper;
using FileMappingEngine.Lib.Database.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace FileMappingEngine.Lib.Database.Repositories
{
    public class FileRepository
    {
        private readonly DbConnFactory _dbConnFactory;

        public FileRepository(DbConnFactory dbConnFactory)
        {
            _dbConnFactory = dbConnFactory;
        }

        public async Task<long> AddFileDefinitionAsync(FileEntity fileEntity)
        {
            using var connection = await _dbConnFactory.CreateConnectionAsync();
            var sql = @"
                INSERT INTO file_definitions (name, fingerprint, fingerprint_hash)
                VALUES (@Name, @FingerPrint::jsonb, @FingerprintHash)
                RETURNING id;";

            return await connection.ExecuteScalarAsync<long>(sql, fileEntity);
        }
        public async Task<FileEntity?> GetFileDefinitionByHashAsync(string hash)
        {
            using var connection = await _dbConnFactory.CreateConnectionAsync();
            var sql = @"
                SELECT *
                FROM file_definitions
                WHERE fingerprint_hash = @FingerprintHash
                LIMIT 1;";

            var result = await connection.QueryFirstOrDefaultAsync<FileEntity>(
                sql,
                new { FingerprintHash = hash });

            return result;
        }
    }
}
