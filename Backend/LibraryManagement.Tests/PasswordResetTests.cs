using BLL;
using DAL;
using DAL.Helper;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace LibraryManagement.Tests
{
    public class PasswordResetTests
    {
        private sealed class HangDoiGia : IHangDoiThongBao
        {
            public List<ThongBaoEmail> DaNhan { get; } = new();
            public bool NemLoi { get; set; }

            public void ThemEmail(ThongBaoEmail thongBao)
            {
                if (NemLoi) throw new InvalidOperationException("queue");
                DaNhan.Add(thongBao);
            }
        }

        private static IConfiguration CauHinh(string? baseUrl = "http://localhost:5080") =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JwtSettings:SecretKey"] = TestHelper.SECRET_KEY,
                    ["JwtSettings:Issuer"] = "LibraryManagement.Test",
                    ["JwtSettings:Audience"] = "LibraryManagement.Test",
                    ["JwtSettings:ExpireHours"] = "8",
                    ["Frontend:BaseUrl"] = baseUrl,
                })
                .Build();

        private static AuthService Tao(
            Mock<IPasswordResetRepository> repo,
            HangDoiGia hangDoi,
            string? baseUrl = "http://localhost:5080")
        {
            return new AuthService(
                new Mock<IDatabaseHelper>().Object,
                CauHinh(baseUrl),
                NullLogger<AuthService>.Instance,
                repo.Object,
                hangDoi);
        }

        [Fact]
        public void Taotoken_32_byte_base64url_sha256_hex_64()
        {
            var raw = AuthService.taotoken();
            raw.Should().NotContain("+").And.NotContain("/").And.NotContain("=");
            Microsoft.IdentityModel.Tokens.Base64UrlEncoder.DecodeBytes(raw).Should().HaveCount(32);

            var hex = AuthService.bamtoken(raw);
            hex.Should().HaveLength(64).And.MatchRegex("^[0-9a-f]{64}$");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("khong-phai-email")]
        [InlineData("a@b")]
        public async Task Yeucau_email_khong_hop_le_khong_goi_db(string? email)
        {
            var repo = new Mock<IPasswordResetRepository>();
            var hangDoi = new HangDoiGia();
            var res = await Tao(repo, hangDoi).yeucaudatlaimatkhau(email);

            res.success.Should().BeTrue();
            res.message.Should().Be(AuthService.ThongDiepYeuCauDatLai);
            hangDoi.DaNhan.Should().BeEmpty();
            repo.Verify(x => x.TaoTokenAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("readers")]
        public async Task Yeucau_khong_ton_tai_va_ton_tai_cung_thong_diep(string? accountType)
        {
            var repo = new Mock<IPasswordResetRepository>();
            repo.Setup(x => x.TaoTokenAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(accountType);
            var hangDoi = new HangDoiGia();

            var res = await Tao(repo, hangDoi).yeucaudatlaimatkhau("ban.doc@thu-vien.vn");

            res.success.Should().BeTrue();
            res.message.Should().Be(AuthService.ThongDiepYeuCauDatLai);
            Assert.Null(res.data);
            hangDoi.DaNhan.Should().HaveCount(accountType == null ? 0 : 1);
        }

        [Fact]
        public async Task Yeucau_gui_hash_hex_khong_raw_va_link_dung_baseurl()
        {
            string? hashGui = null;
            var repo = new Mock<IPasswordResetRepository>();
            repo.Setup(x => x.TaoTokenAsync("ban.doc@thu-vien.vn", It.IsAny<string>()))
                .Callback<string, string>((_, hash) => hashGui = hash)
                .ReturnsAsync("users");
            var hangDoi = new HangDoiGia();

            await Tao(repo, hangDoi, "http://localhost:5080/").yeucaudatlaimatkhau("ban.doc@thu-vien.vn");

            hashGui.Should().MatchRegex("^[0-9a-f]{64}$");
            hangDoi.DaNhan.Should().ContainSingle();
            var mail = hangDoi.DaNhan[0];
            mail.NguoiNhan.Should().Be("ban.doc@thu-vien.vn");
            mail.NoiDung.Should().Contain("http://localhost:5080/reset-password?token=");
            mail.NoiDung.Should().NotContain(hashGui);
        }

        [Fact]
        public async Task Yeucau_loi_db_van_generic_khong_gui_mail()
        {
            var repo = new Mock<IPasswordResetRepository>();
            repo.Setup(x => x.TaoTokenAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("db down"));
            var hangDoi = new HangDoiGia();

            var res = await Tao(repo, hangDoi).yeucaudatlaimatkhau("ban.doc@thu-vien.vn");

            res.success.Should().BeTrue();
            res.message.Should().Be(AuthService.ThongDiepYeuCauDatLai);
            hangDoi.DaNhan.Should().BeEmpty();
        }

        [Fact]
        public async Task Yeucau_enqueue_loi_van_generic()
        {
            var repo = new Mock<IPasswordResetRepository>();
            repo.Setup(x => x.TaoTokenAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("readers");
            var hangDoi = new HangDoiGia { NemLoi = true };

            var res = await Tao(repo, hangDoi).yeucaudatlaimatkhau("ban.doc@thu-vien.vn");

            res.success.Should().BeTrue();
            res.message.Should().Be(AuthService.ThongDiepYeuCauDatLai);
        }

        [Fact]
        public async Task Yeucau_thieu_baseurl_khong_gui_mail_van_generic()
        {
            var repo = new Mock<IPasswordResetRepository>();
            repo.Setup(x => x.TaoTokenAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("readers");
            var hangDoi = new HangDoiGia();

            var res = await Tao(repo, hangDoi, "   ").yeucaudatlaimatkhau("ban.doc@thu-vien.vn");

            res.success.Should().BeTrue();
            hangDoi.DaNhan.Should().BeEmpty();
        }

        [Fact]
        public async Task Hoanthanh_token_sai_khong_goi_db()
        {
            var repo = new Mock<IPasswordResetRepository>();
            var res = await Tao(repo, new HangDoiGia()).hoanthanhdatlaimatkhau("!!!", "mat-khau-moi");

            res.success.Should().BeFalse();
            res.message.Should().Be(AuthService.ThongDiepDatLaiThatBai);
            repo.Verify(x => x.TieuThuTokenAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Hoanthanh_matkhau_rong_khong_goi_db()
        {
            var repo = new Mock<IPasswordResetRepository>();
            var res = await Tao(repo, new HangDoiGia()).hoanthanhdatlaimatkhau(AuthService.taotoken(), "");

            res.success.Should().BeFalse();
            res.message.Should().Be("Mật khẩu không hợp lệ.");
            repo.Verify(x => x.TieuThuTokenAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Hoanthanh_gui_hash_token_va_bcrypt()
        {
            string? tokenHash = null;
            string? matkhauHash = null;
            var raw = AuthService.taotoken();
            var repo = new Mock<IPasswordResetRepository>();
            repo.Setup(x => x.TieuThuTokenAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((t, m) => { tokenHash = t; matkhauHash = m; })
                .Returns(Task.CompletedTask);

            var res = await Tao(repo, new HangDoiGia()).hoanthanhdatlaimatkhau(raw, "MatKhauMoi");

            res.success.Should().BeTrue();
            res.message.Should().Be(AuthService.ThongDiepDatLaiThanhCong);
            tokenHash.Should().Be(AuthService.bamtoken(raw));
            matkhauHash.Should().StartWith("$2");
            matkhauHash!.Length.Should().BeLessThanOrEqualTo(255);
            BCrypt.Net.BCrypt.Verify("MatKhauMoi", matkhauHash).Should().BeTrue();
        }

        [Fact]
        public async Task Hoanthanh_loi_db_generic_khong_thanh_cong_gia()
        {
            var repo = new Mock<IPasswordResetRepository>();
            repo.Setup(x => x.TieuThuTokenAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("token invalid"));

            var res = await Tao(repo, new HangDoiGia()).hoanthanhdatlaimatkhau(AuthService.taotoken(), "MatKhauMoi");

            res.success.Should().BeFalse();
            res.message.Should().Be(AuthService.ThongDiepDatLaiThatBai);
            res.message.Should().NotContain("token invalid");
        }

        [Fact]
        public void Adapter_type_email()
        {
            var q = new LibraryManagement.API.BackgroundServices.NotificationQueue();
            var adapter = new LibraryManagement.API.BackgroundServices.HangDoiThongBaoAdapter(q);
            adapter.ThemEmail(new ThongBaoEmail { TieuDe = "t", NoiDung = "n", NguoiNhan = "a@b.co" });

            var msg = q.DequeueNotification();
            msg.Should().NotBeNull();
            msg!.Type.Should().Be("email");
            msg.Recipient.Should().Be("a@b.co");
        }
    }
}
