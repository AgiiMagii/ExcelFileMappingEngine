using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using Npgsql;

namespace FileMappingEngine.Lib.Database
{
    public class DbConnFactory
    {
        private readonly string _connectionString;

        public DbConnFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnection CreateConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }
    }
}
