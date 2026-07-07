using System;
using System.Collections.Generic;
using System.Text;
using Dapper;
using FileMappingEngine.Lib.Database.Entities;

namespace FileMappingEngine.Lib.Database.Repositories
{
    public class UserRepository
    {
        private readonly DbConnFactory _dbConnFactory;

        public UserRepository(DbConnFactory dbConnFactory)
        {
            _dbConnFactory = dbConnFactory;
        }

        //public async Task<List<UserEntity>> GetAllUsersAsync()
        //{
        //    using var connection = _dbConnFactory.CreateConnection();
        //    var result = await connection.QueryAsync<UserEntity>("SELECT * FROM Users");
        //    return result.ToList();
        //}
    }
}
