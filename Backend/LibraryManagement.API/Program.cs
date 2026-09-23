using BLL;
using DAL;
using DAL.Helper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ===================== SERILOG =====================
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/library-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

// ===================== JWT =====================
var jwtSection  = builder.Configuration.GetSection("JwtSettings");
var secretKey   = jwtSection["SecretKey"]!;
var issuer      = jwtSection["Issuer"]!;
var audience    = jwtSection["Audience"]!;

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

// ===================== CORS =====================
builder.Services.AddCors(opt =>
    opt.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// ===================== DI — Infrastructure =====================
var connStr = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddTransient<IDatabaseHelper>(_ => new DatabaseHelper(connStr));

// ===================== DI — DAL (Repositories) =====================
builder.Services.AddTransient<BookRepository>();
builder.Services.AddTransient<ReaderRepository>();
builder.Services.AddTransient<LoanRepository>();
builder.Services.AddTransient<FineRepository>();
builder.Services.AddTransient<ShelfRepository>();
builder.Services.AddTransient<CopyRepository>();
builder.Services.AddTransient<ReservationRepository>();
builder.Services.AddTransient<ReportRepository>();

// ===================== DI — BLL (Services) =====================
builder.Services.AddTransient<BookService>();
builder.Services.AddTransient<ReaderService>();
builder.Services.AddTransient<LoanService>();
builder.Services.AddTransient<FineService>();
builder.Services.AddTransient<AuthService>();
builder.Services.AddTransient<ShelfService>();
builder.Services.AddTransient<CopyService>();
builder.Services.AddTransient<ReservationService>();
builder.Services.AddTransient<ReportService>();

// ===================== CONTROLLERS + SWAGGER =====================
builder.Services.AddControllers();
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

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Library Management API v1");
    c.RoutePrefix = string.Empty; // Swagger tại root "/"
});

app.UseSerilogRequestLogging();
app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

Log.Information("Library Management System API đang chạy...");
app.Run();
