using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DAL.Helper
{
    public class DatabaseHelper : IDatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnection GetConnection() => new SqlConnection(_connectionString);

        public async Task<IEnumerable<T>> QueryAsync<T>(string storedProcedure, object? param = null)
        {
            using var conn = GetConnection();
            return await conn.QueryAsync<T>(storedProcedure, param, commandType: CommandType.StoredProcedure);
        }

        public async Task<T?> QueryFirstOrDefaultAsync<T>(string storedProcedure, object? param = null)
        {
            using var conn = GetConnection();
            return await conn.QueryFirstOrDefaultAsync<T>(storedProcedure, param, commandType: CommandType.StoredProcedure);
        }

        public async Task<int> ExecuteAsync(string storedProcedure, object? param = null)
        {
            using var conn = GetConnection();
            return await conn.ExecuteAsync(storedProcedure, param, commandType: CommandType.StoredProcedure);
        }

        public async Task<T?> ExecuteScalarAsync<T>(string storedProcedure, object? param = null)
        {
            using var conn = GetConnection();
            return await conn.ExecuteScalarAsync<T>(storedProcedure, param, commandType: CommandType.StoredProcedure);
        }
    }
}
