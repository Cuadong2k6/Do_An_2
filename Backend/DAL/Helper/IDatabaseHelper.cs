using System.Data;

namespace DAL.Helper
{
    public interface IDatabaseHelper
    {
        Task<IEnumerable<T>> QueryAsync<T>(string storedProcedure, object? param = null);
        Task<T?> QueryFirstOrDefaultAsync<T>(string storedProcedure, object? param = null);
        Task<int> ExecuteAsync(string storedProcedure, object? param = null);
        Task<T?> ExecuteScalarAsync<T>(string storedProcedure, object? param = null);
        IDbConnection GetConnection();
    }
}
