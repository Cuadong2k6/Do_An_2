using BLL;
using DAL;
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

        private static AuthService TaoGoogleService(GoogleReaderLoginResult? reader)
        {
            var db = new Mock<IDatabaseHelper>();
            var repo = new Mock<IGoogleReaderRepository>();
            repo.Setup(x => x.DangNhapBangGoogleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
                .ReturnsAsync(reader);
            return new AuthService(db.Object, CauHinhJwt(), NullLogger<AuthService>.Instance, null, null, repo.Object);
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

        // ---------- Bạn đọc chỉ đăng nhập qua Google ----------

        [Fact]
        public async Task Dangnhap_bandoc_matkhau_bi_tu_choi()
        {
            // Không có trong bảng users → rơi xuống tra bảng readers theo email
            var svc = TaoService(user: null, BanDoc(TestHelper.Bcrypt("123456")));

            var res = await svc.dangnhap("bandoc1@gmail.com", "123456");

            res.success.Should().BeFalse();
            res.message.Should().Be("Bạn đọc vui lòng đăng nhập bằng Google.");
        }

        [Fact]
        public async Task Dangnhap_bandoc_matkhau_sai_thi_that_bai()
        {
            var svc = TaoService(user: null, BanDoc(TestHelper.Bcrypt("123456")));

            var res = await svc.dangnhap("bandoc1@gmail.com", "sai-mat-khau");

            res.success.Should().BeFalse();
            res.message.Should().Be("Bạn đọc vui lòng đăng nhập bằng Google.");
        }

        [Fact]
        public async Task Dangnhap_bandoc_bi_khoa_thi_that_bai()
        {
            var svc = TaoService(user: null, BanDoc(TestHelper.Bcrypt("123456"), trangthai: 1));

            var res = await svc.dangnhap("bandoc1@gmail.com", "123456");

            res.success.Should().BeFalse();
            res.message.Should().Be("Bạn đọc vui lòng đăng nhập bằng Google.");
        }

        [Fact]
        public async Task Dangnhap_bandoc_bi_khoa_mkhau_sai_khong_lo_trang_thai()
        {
            // Reader password login tắt; không tiết lộ trạng thái reader.
            var svc = TaoService(user: null, BanDoc(TestHelper.Bcrypt("123456"), trangthai: 1));

            var res = await svc.dangnhap("bandoc1@gmail.com", "sai-mat-khau");

            res.message.Should().Be("Bạn đọc vui lòng đăng nhập bằng Google.");
        }

        [Fact]
        public async Task Dangnhap_ten_tai_khoan_khong_co_at_thi_tra_ban_doc()
        {
            // Tài khoản không thuộc users được từ chối bằng thông báo chung.
            var svc = TaoService(user: null, reader: null);

            var res = await svc.dangnhap("thuthu1", "123456");

            res.success.Should().BeFalse();
            res.message.Should().Be("Sai tài khoản hoặc mật khẩu.");
        }

        [Fact]
        public async Task DangNhapGoogle_PendingPaymentDuocCapJwtCoOnboardingClaim()
        {
            var svc = TaoGoogleService(new GoogleReaderLoginResult
            {
                reader_id = TestHelper.READER_ID,
                hoten = "Độc giả",
                trangthai = 0,
                trangthai_onboarding = 1,
                is_new = true,
                status_code = 0
            });

            var result = await svc.DangNhapGoogleAsync("sub", "reader@example.com", "Độc giả");

            result.success.Should().BeTrue();
            var token = (ThuocTinh(result, "token") as string)!;
            var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(token);
            jwt.Claims.Should().Contain(c => c.Type == "onboarding_status" && c.Value == "1");
        }

        [Fact]
        public async Task DangNhapGoogle_TuChoiReaderBiKhoa()
        {
            var svc = TaoGoogleService(new GoogleReaderLoginResult { status_code = 0, trangthai = 2 });

            var result = await svc.DangNhapGoogleAsync("sub", "reader@example.com", "Độc giả");

            result.success.Should().BeFalse();
        }

        [Fact]
        public async Task DangNhapGoogle_TuChoiEmailConflictKhongCapJwt()
        {
            var svc = TaoGoogleService(new GoogleReaderLoginResult { status_code = 50301 });

            var result = await svc.DangNhapGoogleAsync("sub", "staff@example.com", "Staff");

            result.success.Should().BeFalse();
        }

        // ---------- Nhân viên bị khóa (is_locked) ----------

        [Fact]
        public async Task Dangnhap_staff_bi_khoa_thi_that_bai_va_khong_cap_token()
        {
            // Regression test: staff có is_locked=true phải bị từ chối đăng nhập,
            // không kiểm tra mật khẩu, không phát JWT.
            var userBiKhoa = new UserModel
            {
                user_id   = TestHelper.USER_ID,
                taikhoan  = "thuthu1",
                hoten     = "Thủ Thư Bị Khóa",
                role      = "ThuThu",
                matkhau   = TestHelper.Bcrypt("123456"),
                is_locked = true,
            };
            var svc = TaoService(userBiKhoa);

            var res = await svc.dangnhap("thuthu1", "123456");

            res.success.Should().BeFalse();
            res.message.Should().Contain("bị khóa");
            res.DuLieu().Should().BeNull("không được cấp token khi tài khoản bị khóa");
        }
    }
}
