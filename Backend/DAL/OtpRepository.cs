using DAL.Helper;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DAL
{
    public interface IOtpRepository
    {
        Task TaoOtpAsync(string emailNorm, string otpHash);
        Task XacThucOtpAsync(string emailNorm, string otpHash, string proofHash);
        Task<string?> TieuThuProofAsync(string proofHash);
    }

    public class OtpRepository : IOtpRepository
    {
        public const int LoiCooldown    = 60201;
        public const int LoiSaiMa       = 60202;
        public const int LoiDaDung      = 60203;
        public const int LoiHetHan      = 60204;
        public const int LoiBiKhoa      = 60205;
        public const int LoiThamSo      = 60206;
        public const int LoiProof       = 60207;
        public const int LoiApplock     = 60208;

        private readonly IDatabaseHelper _db;

        public OtpRepository(IDatabaseHelper db)
        {
            _db = db;
        }

        /// <summary>
        /// Gọi sp_otp_create. Cooldown 60s theo email ném 60201.
        /// </summary>
        public async Task TaoOtpAsync(string emailNorm, string otpHash)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            var p = new DynamicParameters();
            p.Add("@email_norm", emailNorm, DbType.String, size: 200);
            p.Add("@otp_hash", otpHash, DbType.AnsiString, size: 64);

            await conn.ExecuteAsync(
                "sp_otp_create",
                p,
                commandType: CommandType.StoredProcedure);
        }

        /// <summary>
        /// Gọi sp_otp_verify. Lỗi nghiệp vụ (60202-60205) ném SqlException
        /// để BLL phân biệt sai mã / đã dùng / hết hạn / bị khóa.
        /// </summary>
        public async Task XacThucOtpAsync(string emailNorm, string otpHash, string proofHash)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            var p = new DynamicParameters();
            p.Add("@email_norm", emailNorm, DbType.String, size: 200);
            p.Add("@otp_hash", otpHash, DbType.AnsiString, size: 64);
            p.Add("@proof_hash", proofHash, DbType.AnsiString, size: 64);

            await conn.ExecuteAsync(
                "sp_otp_verify",
                p,
                commandType: CommandType.StoredProcedure);
        }

        /// <summary>
        /// Gọi sp_proof_consume. Trả email chuẩn hóa, null nếu proof
        /// không hợp lệ / hết hạn / đã dùng (60207 nuốt tại đây).
        /// </summary>
        public async Task<string?> TieuThuProofAsync(string proofHash)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            var p = new DynamicParameters();
            p.Add("@proof_hash", proofHash, DbType.AnsiString, size: 64);
            p.Add("@email_norm", dbType: DbType.String, size: 200, direction: ParameterDirection.Output);

            try
            {
                await conn.ExecuteAsync(
                    "sp_proof_consume",
                    p,
                    commandType: CommandType.StoredProcedure);
            }
            catch (SqlException ex) when (ex.Number == LoiProof)
            {
                return null;
            }

            return p.Get<string?>("@email_norm");
        }
    }
}
