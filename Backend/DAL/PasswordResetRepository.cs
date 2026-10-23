using DAL.Helper;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DAL
{
    public interface IPasswordResetRepository
    {
        Task<string?> TaoTokenAsync(string email, string tokenHash);
        Task TieuThuTokenAsync(string tokenHash, string matkhauHash);
    }

    public class PasswordResetRepository : IPasswordResetRepository
    {
        public const int LoiKhongTonTai = 50201;
        public const int LoiMoHo = 50202;

        private readonly IDatabaseHelper _db;

        public PasswordResetRepository(IDatabaseHelper db)
        {
            _db = db;
        }

        /// <summary>
        /// Gọi sp_pwreset_create_token.
        /// 50201 / 50202 nuốt tại đây, trả account_type null — không lộ tồn tại tài khoản.
        /// Lỗi DB khác ném lại.
        /// </summary>
        public async Task<string?> TaoTokenAsync(string email, string tokenHash)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            var p = new DynamicParameters();
            p.Add("@email", email, DbType.String, size: 200);
            p.Add("@token_hash", tokenHash, DbType.AnsiString, size: 64);
            p.Add("@account_type", dbType: DbType.String, size: 10, direction: ParameterDirection.Output);

            try
            {
                await conn.ExecuteAsync(
                    "sp_pwreset_create_token",
                    p,
                    commandType: CommandType.StoredProcedure);
            }
            catch (SqlException ex) when (ex.Number == LoiKhongTonTai || ex.Number == LoiMoHo)
            {
                return null;
            }

            return p.Get<string?>("@account_type");
        }

        public async Task TieuThuTokenAsync(string tokenHash, string matkhauHash)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            var p = new DynamicParameters();
            p.Add("@token_hash", tokenHash, DbType.AnsiString, size: 64);
            p.Add("@matkhau_hash", matkhauHash, DbType.String, size: 255);

            await conn.ExecuteAsync(
                "sp_pwreset_consume_token",
                p,
                commandType: CommandType.StoredProcedure);
        }
    }
}
