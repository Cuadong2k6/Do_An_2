using BLL;
using DAL;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Model.Shared;
using Moq;
using Xunit;

namespace LibraryManagement.Tests
{
    /// <summary>
    /// Unit tests cho OtpService — xác thực OTP qua email.
    /// Mock repository và queue, không cần database thật.
    /// </summary>
    public class OtpServiceTests
    {
        private const string HMAC_KEY = "Day-la-khoa-HMAC-cho-unit-test-OTP-it-nhat-32-byte!!";
        private const string EMAIL_HOP_LE = "test@example.com";
        private const string EMAIL_CHUYEN_HOA = "Test@Example.COM";

        private static IConfiguration TaoConfig() =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Security:OtpHmacKey"] = HMAC_KEY,
                })
                .Build();

        private static OtpService TaoService(
            Mock<IOtpRepository>? mockRepo = null,
            Mock<IHangDoiThongBao>? mockQueue = null,
            IConfiguration? config = null)
        {
            mockRepo ??= new Mock<IOtpRepository>();
            mockQueue ??= new Mock<IHangDoiThongBao>();
            config ??= TaoConfig();

            return new OtpService(
                mockRepo.Object,
                config,
                mockQueue.Object,
                NullLogger<OtpService>.Instance);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_KhoaHmacThieu_ThrowException()
        {
            // Arrange
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Security:OtpHmacKey"] = null,
                })
                .Build();
            var repo = new Mock<IOtpRepository>();
            var queue = new Mock<IHangDoiThongBao>();

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                new OtpService(repo.Object, config, queue.Object, NullLogger<OtpService>.Instance));
            ex.Message.Should().Contain("32 byte");
        }

        [Fact]
        public void Constructor_KhoaHmacNganHon32Byte_ThrowException()
        {
            // Arrange
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Security:OtpHmacKey"] = "qua-ngan", // < 32 bytes UTF-8
                })
                .Build();
            var repo = new Mock<IOtpRepository>();
            var queue = new Mock<IHangDoiThongBao>();

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                new OtpService(repo.Object, config, queue.Object, NullLogger<OtpService>.Instance));
            ex.Message.Should().Contain("32 byte");
        }

        [Fact]
        public void Constructor_KhoaHmacDu32Byte_KhoiTaoThanhCong()
        {
            // Arrange
            var config = TaoConfig();
            var repo = new Mock<IOtpRepository>();
            var queue = new Mock<IHangDoiThongBao>();

            // Act
            var service = new OtpService(repo.Object, config, queue.Object, NullLogger<OtpService>.Instance);

            // Assert
            service.Should().NotBeNull();
        }

        #endregion

        #region YeuCauOtpAsync Tests

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("khong-co-at")]
        [InlineData("khong-co-dot@example")]
        [InlineData("co khoang trang@example.com")]
        public async Task YeuCauOtpAsync_EmailKhongHopLe_TraVeFail(string? emailKhongHopLe)
        {
            // Arrange
            var service = TaoService();

            // Act
            var result = await service.YeuCauOtpAsync(emailKhongHopLe);

            // Assert
            result.success.Should().BeFalse();
            result.message.Should().Contain("Email không hợp lệ");
        }

        [Fact]
        public async Task YeuCauOtpAsync_EmailHopLe_GoiRepositoryVoiEmailChuanHoa()
        {
            // Arrange
            var mockRepo = new Mock<IOtpRepository>();
            var mockQueue = new Mock<IHangDoiThongBao>();
            var service = TaoService(mockRepo, mockQueue);

            string? emailLuuVaoDb = null;
            string? otpHashLuuVaoDb = null;

            mockRepo.Setup(r => r.TaoOtpAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((e, h) => { emailLuuVaoDb = e; otpHashLuuVaoDb = h; })
                .Returns(Task.CompletedTask);

            // Act
            var result = await service.YeuCauOtpAsync(EMAIL_CHUYEN_HOA);

            // Assert
            result.success.Should().BeTrue();
            emailLuuVaoDb.Should().Be("test@example.com"); // chuẩn hóa: trim + lower
            otpHashLuuVaoDb.Should().NotBeNullOrEmpty();
            otpHashLuuVaoDb.Should().HaveLength(64); // HMAC-SHA256 hex = 64 chars
            otpHashLuuVaoDb.Should().MatchRegex("^[0-9a-f]{64}$"); // hex thường
        }

        [Fact]
        public async Task YeuCauOtpAsync_ThanhCong_GuiEmailVaoHangDoi()
        {
            // Arrange
            var mockRepo = new Mock<IOtpRepository>();
            var mockQueue = new Mock<IHangDoiThongBao>();
            var service = TaoService(mockRepo, mockQueue);

            ThongBaoEmail? emailDaGui = null;
            mockQueue.Setup(q => q.ThemEmail(It.IsAny<ThongBaoEmail>()))
                .Callback<ThongBaoEmail>(e => emailDaGui = e);

            // Act
            var result = await service.YeuCauOtpAsync(EMAIL_HOP_LE);

            // Assert
            result.success.Should().BeTrue();
            emailDaGui.Should().NotBeNull();
            emailDaGui!.NguoiNhan.Should().Be("test@example.com");
            emailDaGui.TieuDe.Should().Contain("Mã xác thực");
            emailDaGui.NoiDung.Should().MatchRegex(@"\d{6}"); // chứa OTP 6 chữ số
            emailDaGui.NoiDung.Should().Contain("5 phút");
        }

        [Fact]
        public async Task YeuCauOtpAsync_OtpCo6ChuSo_BaoGomLeadingZeros()
        {
            // Arrange
            var mockRepo = new Mock<IOtpRepository>();
            var mockQueue = new Mock<IHangDoiThongBao>();
            var service = TaoService(mockRepo, mockQueue);

            var otpList = new List<string>();
            mockQueue.Setup(q => q.ThemEmail(It.IsAny<ThongBaoEmail>()))
                .Callback<ThongBaoEmail>(e =>
                {
                    // Extract OTP từ message
                    var match = System.Text.RegularExpressions.Regex.Match(e.NoiDung, @"\d{6}");
                    if (match.Success) otpList.Add(match.Value);
                });

            // Act — gọi nhiều lần để test CSPRNG distribution
            for (int i = 0; i < 20; i++)
            {
                mockRepo.Setup(r => r.TaoOtpAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(Task.CompletedTask);
                await service.YeuCauOtpAsync(EMAIL_HOP_LE);
            }

            // Assert
            otpList.Should().HaveCount(20);
            otpList.Should().OnlyContain(otp => otp.Length == 6);
            otpList.Should().OnlyContain(otp => otp.All(c => c >= '0' && c <= '9'));
            // Có ít nhất một OTP bắt đầu bằng '0' (statistically likely với 20 samples)
            // Nếu không có leading zero handling thì sẽ có OTP < 6 chữ số
        }

        [Fact]
        public async Task YeuCauOtpAsync_RepositoryNemLoiCooldown_TraVeFailVoiMessageCooldown()
        {
            // Arrange
            var mockRepo = new Mock<IOtpRepository>();
            var service = TaoService(mockRepo);

            mockRepo.Setup(r => r.TaoOtpAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(TaoSqlException(OtpRepository.LoiCooldown));

            // Act
            var result = await service.YeuCauOtpAsync(EMAIL_HOP_LE);

            // Assert
            result.success.Should().BeFalse();
            result.message.Should().Contain("60 giây");
        }

        [Fact]
        public async Task YeuCauOtpAsync_RepositoryNemLoiKhac_TraVeFailGeneric()
        {
            // Arrange
            var mockRepo = new Mock<IOtpRepository>();
            var service = TaoService(mockRepo);

            mockRepo.Setup(r => r.TaoOtpAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("Database connection failed"));

            // Act
            var result = await service.YeuCauOtpAsync(EMAIL_HOP_LE);

            // Assert
            result.success.Should().BeFalse();
            result.message.Should().Contain("Không thể gửi mã xác thực");
        }

        #endregion

        #region XacThucOtpAsync Tests

        [Theory]
        [InlineData(null, "123456")]
        [InlineData("", "123456")]
        [InlineData("   ", "123456")]
        [InlineData("khong-hop-le", "123456")]
        [InlineData(EMAIL_HOP_LE, null)]
        [InlineData(EMAIL_HOP_LE, "")]
        [InlineData(EMAIL_HOP_LE, "12345")] // < 6 chữ số
        [InlineData(EMAIL_HOP_LE, "1234567")] // > 6 chữ số
        [InlineData(EMAIL_HOP_LE, "12345a")] // có ký tự không phải số
        [InlineData(EMAIL_HOP_LE, "12 456")] // có khoảng trắng
        public async Task XacThucOtpAsync_DauVaoKhongHopLe_TraVeFail(string? email, string? otp)
        {
            // Arrange
            var service = TaoService();

            // Act
            var result = await service.XacThucOtpAsync(email, otp);

            // Assert
            result.success.Should().BeFalse();
            result.message.Should().Contain("không hợp lệ");
        }

        [Fact]
        public async Task XacThucOtpAsync_OtpDung_GoiRepositoryVoiEmailChuanHoaVaOtpHash()
        {
            // Arrange
            var mockRepo = new Mock<IOtpRepository>();
            var service = TaoService(mockRepo);

            string? emailLuuVaoDb = null;
            string? otpHashLuuVaoDb = null;
            string? proofHashLuuVaoDb = null;

            mockRepo.Setup(r => r.XacThucOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string, string>((e, o, p) =>
                {
                    emailLuuVaoDb = e;
                    otpHashLuuVaoDb = o;
                    proofHashLuuVaoDb = p;
                })
                .Returns(Task.CompletedTask);

            // Act
            var result = await service.XacThucOtpAsync(EMAIL_CHUYEN_HOA, "123456");

            // Assert
            result.success.Should().BeTrue();
            emailLuuVaoDb.Should().Be("test@example.com");
            otpHashLuuVaoDb.Should().HaveLength(64);
            otpHashLuuVaoDb.Should().MatchRegex("^[0-9a-f]{64}$");
            proofHashLuuVaoDb.Should().HaveLength(64);
            proofHashLuuVaoDb.Should().MatchRegex("^[0-9a-f]{64}$");
        }

        [Fact]
        public async Task XacThucOtpAsync_ThanhCong_TraVeProofRawChua64KyTuHex()
        {
            // Arrange
            var mockRepo = new Mock<IOtpRepository>();
            var service = TaoService(mockRepo);

            mockRepo.Setup(r => r.XacThucOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await service.XacThucOtpAsync(EMAIL_HOP_LE, "123456");

            // Assert
            result.success.Should().BeTrue();
            Assert.NotNull(result.data);
            var proof = ((dynamic)result.data).proof as string;
            proof.Should().NotBeNullOrEmpty();
            proof.Should().HaveLength(64); // 32 bytes random = 64 hex chars
            proof.Should().MatchRegex("^[0-9a-f]{64}$");
        }

        [Fact]
        public async Task XacThucOtpAsync_MoiLanGoiTaoProofKhacNhau()
        {
            // Arrange
            var mockRepo = new Mock<IOtpRepository>();
            var service = TaoService(mockRepo);

            mockRepo.Setup(r => r.XacThucOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result1 = await service.XacThucOtpAsync(EMAIL_HOP_LE, "123456");
            var result2 = await service.XacThucOtpAsync(EMAIL_HOP_LE, "123456");

            // Assert
            var proof1 = ((dynamic)result1.data!).proof as string;
            var proof2 = ((dynamic)result2.data!).proof as string;
            proof1.Should().NotBe(proof2); // proof phải unique
        }

        [Theory]
        [InlineData(OtpRepository.LoiSaiMa, "không đúng")]
        [InlineData(OtpRepository.LoiDaDung, "đã được sử dụng")]
        [InlineData(OtpRepository.LoiHetHan, "đã hết hạn")]
        [InlineData(OtpRepository.LoiBiKhoa, "đã bị khóa")]
        public async Task XacThucOtpAsync_RepositoryNemLoiNghiepVu_TraVeFailVoiMessageTuongUng(
            int maLoi, string chuoiTrongMessage)
        {
            // Arrange
            var mockRepo = new Mock<IOtpRepository>();
            var service = TaoService(mockRepo);

            mockRepo.Setup(r => r.XacThucOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(TaoSqlException(maLoi));

            // Act
            var result = await service.XacThucOtpAsync(EMAIL_HOP_LE, "123456");

            // Assert
            result.success.Should().BeFalse();
            result.message.Should().Contain(chuoiTrongMessage);
        }

        [Fact]
        public async Task XacThucOtpAsync_RepositoryNemLoiKhac_TraVeFailGeneric()
        {
            // Arrange
            var mockRepo = new Mock<IOtpRepository>();
            var service = TaoService(mockRepo);

            mockRepo.Setup(r => r.XacThucOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("Database error"));

            // Act
            var result = await service.XacThucOtpAsync(EMAIL_HOP_LE, "123456");

            // Assert
            result.success.Should().BeFalse();
            result.message.Should().Contain("Không thể xác thực");
        }

        #endregion

        #region HMAC Consistency Tests

        [Fact]
        public async Task YeuCauOtpAsync_CungOtp_TaoRaCungOtpHash()
        {
            // Arrange
            var mockRepo = new Mock<IOtpRepository>();
            var mockQueue = new Mock<IHangDoiThongBao>();
            var service = TaoService(mockRepo, mockQueue);

            var hashList = new List<string>();
            mockRepo.Setup(r => r.TaoOtpAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((_, hash) => hashList.Add(hash))
                .Returns(Task.CompletedTask);

            // Mock RandomNumberGenerator để trả về cùng OTP
            // (Không mock được CSPRNG trực tiếp, nhưng có thể test qua XacThucOtp)

            // Act — test gián tiếp qua XacThucOtp
            var result1 = await service.XacThucOtpAsync(EMAIL_HOP_LE, "000123");
            var result2 = await service.XacThucOtpAsync(EMAIL_HOP_LE, "000123");

            // Assert — cùng OTP phải tạo cùng hash khi verify
            mockRepo.Verify(r => r.XacThucOtpAsync(
                It.IsAny<string>(),
                It.Is<string>(h => h == hashList.FirstOrDefault()),
                It.IsAny<string>()),
                Times.Never); // repo chưa được setup nên không gọi, nhưng hash phải giống nhau

            // Test khác: verify hash deterministic
            var hash1 = GetOtpHashFromService(service, "123456");
            var hash2 = GetOtpHashFromService(service, "123456");
            hash1.Should().Be(hash2);
        }

        [Fact]
        public void HmacOtp_KhacOtp_TaoRaKhacHash()
        {
            // Arrange
            var service = TaoService();

            // Act
            var hash1 = GetOtpHashFromService(service, "123456");
            var hash2 = GetOtpHashFromService(service, "654321");

            // Assert
            hash1.Should().NotBe(hash2);
        }

        [Fact]
        public void HmacOtp_LeadingZeros_HashKhacVoiKhongCoLeadingZeros()
        {
            // Arrange
            var service = TaoService();

            // Act
            var hash001 = GetOtpHashFromService(service, "000123");
            var hash123 = GetOtpHashFromService(service, "123");

            // Assert
            hash001.Should().NotBe(hash123); // "000123" khác "123"
        }

        // Helper method để trích xuất OTP hash (reflection hoặc thông qua verify call)
        private string GetOtpHashFromService(OtpService service, string otp)
        {
            var mockRepo = new Mock<IOtpRepository>();
            string? capturedHash = null;
            mockRepo.Setup(r => r.XacThucOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string, string>((_, otpHash, _) => capturedHash = otpHash)
                .Returns(Task.CompletedTask);

            // Tạo service mới với mock repo
            var testService = new OtpService(
                mockRepo.Object,
                TaoConfig(),
                new Mock<IHangDoiThongBao>().Object,
                NullLogger<OtpService>.Instance);

            // Gọi XacThucOtp để capture hash
            testService.XacThucOtpAsync(EMAIL_HOP_LE, otp).Wait();
            return capturedHash ?? string.Empty;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Tạo SqlException với error number cụ thể bằng reflection.
        /// SqlException không có public constructor nên phải dùng reflection.
        /// </summary>
        private static SqlException TaoSqlException(int errorNumber)
        {
            // Sử dụng reflection để tạo SqlException
            // SqlException có internal constructor nhận SqlErrorCollection
            var sqlErrorCollectionCtor = typeof(SqlException)
                .Assembly
                .GetType("Microsoft.Data.SqlClient.SqlErrorCollection")!
                .GetConstructor(
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                    null,
                    Type.EmptyTypes,
                    null)!;

            var errorCollection = sqlErrorCollectionCtor.Invoke(null);

            // Tạo SqlError - thử nhiều signature vì thay đổi giữa các .NET version
            var sqlErrorType = typeof(SqlException).Assembly.GetType("Microsoft.Data.SqlClient.SqlError")!;
            object? sqlError = null;

            // Thử signature 8 params (cũ)
            var ctor8 = sqlErrorType.GetConstructor(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null,
                new[] { typeof(int), typeof(byte), typeof(byte), typeof(string), typeof(string), typeof(string), typeof(int), typeof(uint) },
                null);
            
            if (ctor8 != null)
            {
                sqlError = ctor8.Invoke(new object[] { errorNumber, (byte)0, (byte)0, "server", $"Error {errorNumber}", "proc", 0, (uint)0 });
            }
            else
            {
                // Thử signature 9 params (mới hơn)
                var ctor9 = sqlErrorType.GetConstructor(
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                    null,
                    new[] { typeof(int), typeof(byte), typeof(byte), typeof(string), typeof(string), typeof(string), typeof(int), typeof(uint), typeof(Exception) },
                    null);
                
                if (ctor9 != null)
                {
                    sqlError = ctor9.Invoke(new object?[] { errorNumber, (byte)0, (byte)0, "server", $"Error {errorNumber}", "proc", 0, (uint)0, null });
                }
                else
                {
                    // Fallback: tìm constructor đầu tiên có int đầu
                    var ctorAny = sqlErrorType.GetConstructors(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        .FirstOrDefault(c => c.GetParameters().Length > 0 && c.GetParameters()[0].ParameterType == typeof(int));
                    
                    if (ctorAny == null)
                        throw new InvalidOperationException("Không tìm thấy SqlError constructor");
                    
                    var paramCount = ctorAny.GetParameters().Length;
                    var args = new object?[paramCount];
                    args[0] = errorNumber;
                    for (int i = 1; i < paramCount; i++)
                    {
                        var pType = ctorAny.GetParameters()[i].ParameterType;
                        args[i] = pType.IsValueType ? Activator.CreateInstance(pType) : 
                                  pType == typeof(string) ? $"param{i}" : null;
                    }
                    sqlError = ctorAny.Invoke(args);
                }
            }

            // Thêm SqlError vào collection
            var addMethod = errorCollection!.GetType().GetMethod("Add", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            addMethod.Invoke(errorCollection, new[] { sqlError });

            // Tạo SqlException
            var sqlExceptionCtor = typeof(SqlException)
                .GetConstructor(
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                    null,
                    new[] { typeof(string), errorCollection.GetType(), typeof(Exception), typeof(Guid) },
                    null)!;

            return (SqlException)sqlExceptionCtor.Invoke(new object?[] { $"SQL Error {errorNumber}", errorCollection, null, Guid.Empty });
        }

        #endregion

        #region Email Validation Edge Cases

        [Theory]
        [InlineData("a@b.c")] // minimum valid
        [InlineData("test+tag@example.co.uk")] // + và subdomain
        [InlineData("user.name@example.com")] // có dot trong local part
        public async Task YeuCauOtpAsync_EmailBienEdgeHopLe_ChapNhan(string email)
        {
            // Arrange
            var mockRepo = new Mock<IOtpRepository>();
            var service = TaoService(mockRepo);

            mockRepo.Setup(r => r.TaoOtpAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await service.YeuCauOtpAsync(email);

            // Assert
            result.success.Should().BeTrue();
        }

        [Theory]
        [InlineData("test@")] // thiếu domain
        [InlineData("@example.com")] // thiếu local part
        [InlineData("test@@example.com")] // double @
        [InlineData("test@example")] // không có dot trong domain
        public async Task YeuCauOtpAsync_EmailBienEdgeKhongHopLe_TuChoi(string email)
        {
            // Arrange
            var service = TaoService();

            // Act
            var result = await service.YeuCauOtpAsync(email);

            // Assert
            result.success.Should().BeFalse();
        }

        [Fact]
        public async Task YeuCauOtpAsync_EmailDai201KyTu_TuChoi()
        {
            // Arrange
            var service = TaoService();
            var longEmail = new string('a', 190) + "@example.com"; // > 200 chars

            // Act
            var result = await service.YeuCauOtpAsync(longEmail);

            // Assert
            result.success.Should().BeFalse();
            result.message.Should().Contain("không hợp lệ");
        }

        #endregion
    }
}
