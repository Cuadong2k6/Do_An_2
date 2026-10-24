using BLL;
using DAL;
using DAL.Helper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using LibraryManagement.API.Services;
using Serilog;
using System.IO;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ===================== SERILOG =====================
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/library-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

// ===================== JWT =====================
var jwtSection  = builder.Configuration.GetSection("JwtSettings");
var secretKey   = jwtSection["SecretKey"];
var issuer      = jwtSection["Issuer"]!;
var audience    = jwtSection["Audience"]!;

// SecretKey không lưu trong appsettings.json (đã commit lên git).
// Đặt bằng: dotnet user-secrets set "JwtSettings:SecretKey" "<chuỗi-bí-mat>"
if (string.IsNullOrWhiteSpace(secretKey))
{
    Console.Error.WriteLine(
        "Thiếu JwtSettings:SecretKey.\n" +
        "Chạy lệnh sau 1 lần để tạo:\n" +
        "  dotnet user-secrets set \"JwtSettings:SecretKey\" \"" + Guid.NewGuid() + "Lms_" + Guid.NewGuid() + "\"");
    return;
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = issuer,
            ValidAudience            = audience,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

builder.Services.AddAuthorization();

// ===================== RATE LIMITING =====================
var rateLimitPermit    = builder.Configuration.GetValue<int>("RateLimit:PermitLimit", 100);
var rateLimitWindowSec = builder.Configuration.GetValue<int>("RateLimit:WindowSeconds", 60);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = rateLimitPermit,
                QueueLimit  = 0,
                Window     = TimeSpan.FromSeconds(rateLimitWindowSec)
            }));
});

// ===================== HEALTH CHECKS =====================
builder.Services.AddHealthChecks();

// ===================== CORS (whitelist từ cấu hình) =====================
var corsOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

if (corsOrigins.Length == 0)
{
    Log.Warning("Cors:AllowedOrigins trống — dùng AllowAnyOrigin. Nên cấu hình whitelist cho production.");
    builder.Services.AddCors(opt =>
        opt.AddPolicy("ApiCors", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
}
else
{
    builder.Services.AddCors(opt =>
        opt.AddPolicy("ApiCors", p =>
            p.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()));
}

// ===================== DI — Infrastructure =====================
var connStr = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddTransient<IDatabaseHelper>(_ => new DatabaseHelper(connStr));

// ===================== DI — DAL (Repositories) =====================
builder.Services.AddTransient<BookRepository>();
builder.Services.AddTransient<ReaderRepository>();
builder.Services.AddTransient<DAL.IGoogleReaderRepository>(sp => sp.GetRequiredService<ReaderRepository>());
builder.Services.AddTransient<GoogleTokenValidator>();
builder.Services.AddTransient<LoanRepository>();
builder.Services.AddTransient<FineRepository>();
builder.Services.AddTransient<ShelfRepository>();
builder.Services.AddTransient<CopyRepository>();
builder.Services.AddTransient<ReservationRepository>();
builder.Services.AddTransient<ReportRepository>();
builder.Services.AddTransient<NhatkyRepository>();
builder.Services.AddTransient<IPasswordResetRepository, PasswordResetRepository>();
builder.Services.AddTransient<DAL.IOtpRepository, DAL.OtpRepository>();
builder.Services.AddTransient<StaffRepository>();
builder.Services.AddTransient<IStaffRepository>(sp => sp.GetRequiredService<StaffRepository>());

// ===================== DI — BLL (Services) =====================
builder.Services.AddTransient<StaffService>();
builder.Services.AddTransient<BLL.IOtpService, BLL.OtpService>();
builder.Services.AddTransient<BookService>();
builder.Services.AddTransient<ReaderService>();
builder.Services.AddTransient<LoanService>();
builder.Services.AddTransient<FineService>();
builder.Services.AddTransient<AuthService>(sp => new AuthService(
    sp.GetRequiredService<IDatabaseHelper>(),
    sp.GetRequiredService<IConfiguration>(),
    sp.GetRequiredService<ILogger<AuthService>>(),
    sp.GetRequiredService<IPasswordResetRepository>(),
    sp.GetRequiredService<BLL.IHangDoiThongBao>(),
    sp.GetRequiredService<ReaderRepository>()));
builder.Services.AddTransient<ShelfService>();
builder.Services.AddTransient<CopyService>();
builder.Services.AddTransient<ReservationService>();
builder.Services.AddTransient<ReportService>();
builder.Services.AddTransient<NhatkyService>();

// ===================== NOTIFICATION & BACKGROUND SERVICES =====================
builder.Services.AddSingleton<LibraryManagement.API.BackgroundServices.INotificationQueue, LibraryManagement.API.BackgroundServices.NotificationQueue>();
builder.Services.AddSingleton<BLL.IHangDoiThongBao, LibraryManagement.API.BackgroundServices.HangDoiThongBaoAdapter>();
builder.Services.AddScoped<LibraryManagement.API.Services.IEmailService, LibraryManagement.API.Services.EmailService>();
builder.Services.AddHostedService<LibraryManagement.API.BackgroundServices.NotificationWorkerService>();

// ===================== CONTROLLERS + SWAGGER =====================
builder.Services.AddControllers()
    .AddJsonOptions(opt => opt.JsonSerializerOptions.PropertyNamingPolicy = null);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title   = "Library Management System API",
        Version = "v1",
        Description = "Hệ Thống Quản Lý Thư Viện — .NET 8 + Dapper"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Nhập JWT Token. Ví dụ: Bearer {token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ===================== BUILD APP =====================
var app = builder.Build();

// Lỗi chưa bắt → JSON (tránh text/plain Microsoft.* làm frontend JSON.parse vỡ)
app.UseExceptionHandler(errApp =>
{
    errApp.Run(async context =>
    {
        var ex = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        Log.Error(ex, "Unhandled exception");
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            success = false,
            message = "Lỗi hệ thống. Vui lòng thử lại sau."
        });
    });
});

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Library Management API v1");
    c.RoutePrefix = "swagger"; // Swagger tại "/swagger" để không xung đột root
});

// Email đặt lại mật khẩu trỏ Frontend:BaseUrl + /reset-password (không có .html).
app.Use(async (ctx, next) =>
{
    if (HttpMethods.IsGet(ctx.Request.Method)
        && ctx.Request.Path.Equals("/reset-password", StringComparison.OrdinalIgnoreCase))
    {
        ctx.Request.Path = "/reset-password.html";
    }
    await next();
});

// Serve default files (index.html) from Frontend folder
app.UseDefaultFiles(new DefaultFilesOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "Frontend")),
    RequestPath = ""
});

// Serve static files from Frontend folder
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "Frontend")),
    RequestPath = ""
});

app.UseSerilogRequestLogging();
app.UseRateLimiter();
app.UseCors("ApiCors");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

Log.Information("Library Management System API đang chạy...");
app.Run();
