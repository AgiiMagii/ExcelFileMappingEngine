using Dapper;
using FileMappingEngine.Lib.Database.Entities;
using System;
using System.Collections.Generic;
using System.Text;

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
                INSERT INTO files (name, fingerprint)
                VALUES (@Name, @FingerPrint::jsonb)
                RETURNING id;";

            return await connection.ExecuteScalarAsync<long>(sql, fileEntity);
        }
    }
}
