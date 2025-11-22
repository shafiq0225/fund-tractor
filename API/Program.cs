using API.Middleware;
using Core.Interfaces;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Core.Interfaces.Auth;
using Infrastructure.Services.Auth;
using Core.Interfaces.Notification;
using Infrastructure.Services.Notification;
using Core.Interfaces.InvestmentRepo;
using Infrastructure.Services.Investment;
using System.Security.Claims;
using API;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddScoped<IAmfiExcelDownloadService, AmfiExcelDownloadService>();
builder.Services.AddScoped<IMarketHolidayProvider, MarketHolidayProvider>();
builder.Services.AddHostedService<DailyNavDownloaderService>();

builder.Services.AddScoped<IAmfiNavService, AmfiNavService>();
builder.Services.AddHostedService<AmfiNavBackgroundService>();
builder.Services.AddControllers();
builder.Services.AddHttpClient();
// Database configuration - STRICT VERSION
string connectionString;
bool isUsingPostgreSQL = false;

if (builder.Environment.IsDevelopment())
{
    // Local Development - SQL Server (Docker)
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<StoreContext>(options =>
        options.UseSqlServer(connectionString));

    Console.WriteLine("🔧 Using SQL Server for local development (Docker)");
}
else
{
    // PRODUCTION - STRICT PostgreSQL Check
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

    Console.WriteLine($"=== DEBUG: DATABASE_URL = {databaseUrl} ===");

    // STRICT validation - only use PostgreSQL
    if (string.IsNullOrEmpty(databaseUrl) || databaseUrl.Contains("${{"))
    {
        // Log detailed error
        Console.WriteLine("❌ FATAL: DATABASE_URL is missing or invalid in production!");
        Console.WriteLine($"❌ DATABASE_URL value: '{databaseUrl}'");

        // Use a dummy PostgreSQL connection that will fail clearly
        connectionString = "Host=invalid;Database=invalid;Username=invalid;Password=invalid";
        builder.Services.AddDbContext<StoreContext>(options =>
            options.UseNpgsql(connectionString));

        Console.WriteLine("🚨 FORCING PostgreSQL (will fail with clear error)");
    }
    else
    {
        // Valid PostgreSQL connection
        connectionString = ParsePostgresConnectionString(databaseUrl);

        // PostgreSQL with retry logic
        builder.Services.AddDbContext<StoreContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            }));

        isUsingPostgreSQL = true;
        Console.WriteLine("🚀 Using PostgreSQL for production (Railway)");
        Console.WriteLine($"🔗 PostgreSQL Host: {new Uri(databaseUrl).Host}");
    }
}

// Helper method to parse Railway's DATABASE_URL
static string ParsePostgresConnectionString(string databaseUrl)
{
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':');

    return new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port,
        Database = uri.AbsolutePath.TrimStart('/'),
        Username = userInfo[0],
        Password = userInfo[1],
        SslMode = SslMode.Prefer,  // ← CHANGED from Require to Prefer
        TrustServerCertificate = true,
        Timeout = 30,              // ← ADD timeout
        CommandTimeout = 30,       // ← ADD command timeout
        KeepAlive = 30             // ← ADD keepalive
    }.ToString();
}


builder.Services.AddScoped<IAmfiRepository, AmfiRepository>();
builder.Services.AddHttpContextAccessor();

// Authentication Services
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IInvestmentRepository, InvestmentRepository>();
builder.Services.AddScoped<ValidateUserStatusFilter>();

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "FundTrackrAPI",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "FundTrackrClient",
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong123!"))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ActiveUser", policy =>
        policy.RequireAssertion(context =>
        {
            if (!context.User.Identity.IsAuthenticated)
                return false;

            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return false;

            return true;
        }));
});

builder.Services.AddCors();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var context = services.GetRequiredService<StoreContext>();

    logger.LogInformation("Testing database connection...");
    logger.LogInformation("Database Provider: {Provider}", context.Database.ProviderName);

    try
    {
        var canConnect = await context.Database.CanConnectAsync();
        if (canConnect)
        {
            logger.LogInformation("✅ Database connection successful!");

            // Ensure database is created and migrations are applied
            await context.Database.MigrateAsync();
            logger.LogInformation("✅ Database migrations applied!");
        }
        else
        {
            logger.LogError("❌ Database connection failed!");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Database connection failed with error: {ErrorMessage}", ex.Message);
    }
}

// Health check endpoints
app.MapGet("/", () => "FundTrackr API is running!");
app.MapGet("/health", () => "Healthy");

// Database test endpoint
app.MapGet("/db-test", async (HttpContext httpContext) =>
{
    try
    {
        var context = httpContext.RequestServices.GetRequiredService<StoreContext>();
        var canConnect = await context.Database.CanConnectAsync();

        var dbProvider = context.Database.ProviderName;
        var databaseName = context.Database.GetDbConnection().Database;

        return Results.Json(new
        {
            database = canConnect ? "Connected" : "Disconnected",
            provider = dbProvider,
            databaseName = databaseName,
            environment = app.Environment.EnvironmentName,
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new
        {
            database = "Error",
            error = ex.Message,
            environment = app.Environment.EnvironmentName,
            timestamp = DateTime.UtcNow
        });
    }
});

// Database debug endpoint
app.MapGet("/db-debug", async (HttpContext httpContext) =>
{
    var context = httpContext.RequestServices.GetRequiredService<StoreContext>();
    var connection = context.Database.GetDbConnection();

    try
    {
        await connection.OpenAsync();
        var dbInfo = new
        {
            database = connection.Database,
            dataSource = connection.DataSource,
            serverVersion = connection.ServerVersion,
            state = connection.State,
            connectionString = connection.ConnectionString?[..Math.Min(connection.ConnectionString?.Length ?? 0, 50)] + "..." // First 50 chars only
        };
        await connection.CloseAsync();

        return Results.Json(new
        {
            status = "Connected",
            info = dbInfo,
            provider = context.Database.ProviderName,
            environment = app.Environment.EnvironmentName,
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new
        {
            status = "Error",
            error = ex.Message,
            connectionString = connection.ConnectionString?[..Math.Min(connection.ConnectionString?.Length ?? 0, 50)] + "...",
            environment = app.Environment.EnvironmentName,
            timestamp = DateTime.UtcNow
        });
    }
});

app.UseMiddleware<ExceptionMiddleware>();

app.UseCors(x => x
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()
    .WithOrigins(
        "http://localhost:4200",
        "https://localhost:4200",
        "https://cerulean-dango-230e36.netlify.app"
    ));

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();