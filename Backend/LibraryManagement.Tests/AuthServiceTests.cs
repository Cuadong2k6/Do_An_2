using BLL;
using DAL.Helper;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Model.Reader;
using Model.Shared;
using Moq;
using Xunit;

namespace LibraryManagement.Tests
{
    /// <summary>
    /// Kiểm thử AuthService.dangnhap — hàm xác thực mật khẩu nhạy cảm nhất hệ thống.
    /// Không cần database thật: IDatabaseHelper được mock trả về UserModel / ReaderModel dựng sẵn.
    /// </summary>
    public class AuthServiceTests
    {
        private static IConfiguration CauHinhJwt() =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JwtSettings:SecretKey"]   = TestHelper.SECRET_KEY,
                    ["JwtSettings:Issuer"]      = "LibraryManagement.Test",
                    ["JwtSettings:Audience"]    = "LibraryManagement.Test",
                    ["JwtSettings:ExpireHours"] = "8",
                })
                .Build();

        private static AuthService TaoService(UserModel? user, ReaderModel? reader = null)
        {
            var db = new Mock<IDatabaseHelper>();
            db.Setup(x => x.QueryFirstOrDefaultAsync<UserModel>("sp_user_login", It.IsAny<object?>()))
              .ReturnsAsync(user);
            db.Setup(x => x.QueryFirstOrDefaultAsync<ReaderModel>("sp_reader_login", It.IsAny<object?>()))
              .ReturnsAsync(reader);

            return new AuthService(db.Object, CauHinhJwt(), NullLogger<AuthService>.Instance);
        }

        private static UserModel NguoiDung(string hashMatKhau) => new()
        {
            user_id  = TestHelper.USER_ID,
            taikhoan = "admin",
            hoten    = "Quản Trị Viên",
            role     = "Admin",
            matkhau  = hashMatKhau,
        };

        private static ReaderModel BanDoc(string hashMatKhau, int trangthai = 0) => new()
        {
            reader_id = TestHelper.READER_ID,
            so_the    = "THE-001",
            hoten     = "Nguyễn Văn An",
            email     = "bandoc1@gmail.com",
            trangthai = trangthai,
            matkhau   = hashMatKhau,
        };

        /// <summary>Đọc 1 thuộc tính của anonymous object trả về từ ResponseModel.data.</summary>
        private static object? ThuocTinh(ResponseModel res, string ten)
        {
            var duLieu = res.DuLieu();
            return duLieu?.GetType().GetProperty(ten)?.GetValue(duLieu);
        }

        // ---------- Không tìm thấy tài khoản ----------

        [Fact]
        public async Task Dangnhap_taikhoan_khong_ton_tai_thi_that_bai()
        {
            var svc = TaoService(user: null);

            var res = await svc.dangnhap("khongco", "123456");

            res.success.Should().BeFalse();
            res.message.Should().Be("Sai tài khoản hoặc mật khẩu.");
            res.DuLieu().Should().BeNull();
        }

        [Fact]
        public async Task Dangnhap_chuoi_rong_thi_that_bai()
        {
            var svc = TaoService(user: null);

            var res = await svc.dangnhap("   ", "123456");

            res.success.Should().BeFalse();
        }

        // ---------- BCrypt (định dạng hiện tại) ----------

        [Fact]
        public async Task Dangnhap_bcrypt_matkhau_dung_thi_thanh_cong()
        {
            var svc = TaoService(NguoiDung(TestHelper.Bcrypt("123456")));

            var res = await svc.dangnhap("admin", "123456");

            res.success.Should().BeTrue();
            res.message.Should().Be("Đăng nhập thành công.");
            res.DuLieu().Should().NotBeNull();
        }

        [Fact]
        public async Task Dangnhap_bcrypt_matkhau_sai_thi_that_bai()
        {
            var svc = TaoService(NguoiDung(TestHelper.Bcrypt("123456")));

            var res = await svc.dangnhap("admin", "sai-mat-khau");

            res.success.Should().BeFalse();
            res.message.Should().Be("Sai tài khoản hoặc mật khẩu.");
        }

        [Fact]
        public async Task Dangnhap_bcrypt_phan_biet_hoa_thuong_hoa()
        {
            // BCrypt có muối ngẫu nhiên → 2 lần hash cùng mật khẩu cho 2 chuỗi khác nhau
            var hash1 = TestHelper.Bcrypt("123456");
            var hash2 = TestHelper.Bcrypt("123456");
            hash1.Should().NotBe(hash2, "BCrypt phải sinh muối ngẫu nhiên mỗi lần hash");

            var svc = TaoService(NguoiDung(hash2));

            var res = await svc.dangnhap("admin", "123456");

            res.success.Should().BeTrue();
        }

        // ---------- MD5 cũ đã bị loại bỏ hoàn toàn ----------

        [Fact]
        public async Task Dangnhap_md5_thi_bi_tu_choi()
        {
            // Đã migrate seed sang BCrypt (Database/05_MigrateMd5ToBcrypt.sql)
            // → hash MD5 trong DB không còn được chấp nhận
            var svc = TaoService(NguoiDung(TestHelper.HASH_MD5_123456));

            var res = await svc.dangnhap("admin", "123456");

            res.success.Should().BeFalse();
        }

        // ---------- Định dạng lưu trữ không hợp lệ ----------

        [Fact]
        public async Task Dangnhap_plaintext_khong_duoc_chap_nhan()
        {
            // Unit 3: đã gỡ nhánh so sánh plaintext — hash lưu trữ không có tiền tố $2
            // và không phải MD5 32 ký tự hex → phải từ chối, KHÔNG so sánh thẳng chuỗi.
            var svc = TaoService(NguoiDung("123456"));

            var res = await svc.dangnhap("admin", "123456");

            res.success.Should().BeFalse("không được chấp nhận so sánh plaintext");
        }

        [Fact]
        public async Task Dangnhap_dinh_dang_la_thi_that_bai()
        {
            var svc = TaoService(NguoiDung("khong-phai-hash-hop-le-abc"));

            var res = await svc.dangnhap("admin", "123456");

            res.success.Should().BeFalse();
        }

        // ---------- Token ----------

        [Fact]
        public async Task Dangnhap_thanh_cong_phai_cap_hat_token()
        {
            var svc = TaoService(NguoiDung(TestHelper.Bcrypt("123456")));

            var res = await svc.dangnhap("admin", "123456");

            (ThuocTinh(res, "token") as string).Should().NotBeNullOrWhiteSpace();
            (ThuocTinh(res, "role") as string).Should().Be("Admin");
            (ThuocTinh(res, "hoten") as string).Should().Be("Quản Trị Viên");
        }

        [Fact]
        public async Task Dangnhap_that_bai_thi_khong_cap_hat_token()
        {
            var svc = TaoService(NguoiDung(TestHelper.Bcrypt("123456")));

            var res = await svc.dangnhap("admin", "sai-mat-khau");

            res.success.Should().BeFalse();
            res.DuLieu().Should().BeNull();
        }

        [Fact]
        public async Task Dangnhap_khong_duoc_lo_matkhau_vao_json()
        {
            var svc = TaoService(NguoiDung(TestHelper.Bcrypt("123456")));

            var res = await svc.dangnhap("admin", "123456");

            var json = System.Text.Json.JsonSerializer.Serialize(res.DuLieu());
            json.Should().NotContain("matkhau");
        }

        // ---------- Bạn đọc (khóa đăng nhập là email) ----------

        [Fact]
        public async Task Dangnhap_bandoc_matkhau_dung_thi_thanh_cong()
        {
            // Không có trong bảng users → rơi xuống tra bảng readers theo email
            var svc = TaoService(user: null, BanDoc(TestHelper.Bcrypt("123456")));

            var res = await svc.dangnhap("bandoc1@gmail.com", "123456");

            res.success.Should().BeTrue();
            (ThuocTinh(res, "role") as string).Should().Be("BanDoc");
            (ThuocTinh(res, "token") as string).Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task Dangnhap_bandoc_matkhau_sai_thi_that_bai()
        {
            var svc = TaoService(user: null, BanDoc(TestHelper.Bcrypt("123456")));

            var res = await svc.dangnhap("bandoc1@gmail.com", "sai-mat-khau");

            res.success.Should().BeFalse();
            res.message.Should().Be("Sai tài khoản hoặc mật khẩu.");
        }

        [Fact]
        public async Task Dangnhap_bandoc_bi_khoa_thi_that_bai()
        {
            var svc = TaoService(user: null, BanDoc(TestHelper.Bcrypt("123456"), trangthai: 1));

            var res = await svc.dangnhap("bandoc1@gmail.com", "123456");

            res.success.Should().BeFalse();
            res.message.Should().Be("Tài khoản đã bị khoá hoặc hết hạn.");
        }

        [Fact]
        public async Task Dangnhap_bandoc_bi_khoa_mkhau_sai_khong_lo_trang_thai()
        {
            // Sai mật khẩu thì phải trả thông báo chung, không tiết lộ tài khoản đang bị khoá
            var svc = TaoService(user: null, BanDoc(TestHelper.Bcrypt("123456"), trangthai: 1));

            var res = await svc.dangnhap("bandoc1@gmail.com", "sai-mat-khau");

            res.message.Should().Be("Sai tài khoản hoặc mật khẩu.");
        }

        [Fact]
        public async Task Dangnhap_ten_tai_khoan_khong_co_at_thi_tra_ban_doc()
        {
            // "thuthu1" không có @ vẫn được gửi lên; kết quả là "sai tài khoản" chứ không phải lỗi 400
            var svc = TaoService(user: null, reader: null);

            var res = await svc.dangnhap("thuthu1", "123456");

            res.success.Should().BeFalse();
            res.message.Should().Be("Sai tài khoản hoặc mật khẩu.");
        }
    }
}