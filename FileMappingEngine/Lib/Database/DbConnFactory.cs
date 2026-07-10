using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Windows;

namespace FileMappingEngine.Lib.Database
{
    public class DbConnFactory
    {
        private readonly string _connectionString;

        public DbConnFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<IDbConnection> CreateConnectionAsync()
        {
            try
            {
                var connection = new NpgsqlConnection(_connectionString);

                await connection.OpenAsync();

                return connection;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                throw;
            }
        }
    }
}
