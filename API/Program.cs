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
// Database configuration
string connectionString;

if (builder.Environment.IsDevelopment())
{
    // Development - SQL Server
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<StoreContext>(options =>
        options.UseSqlServer(connectionString));
    Console.WriteLine("🔧 Using SQL Server for local development");
}
else
{
    // Production - Use Railway Private Network
    var dbHost = "postgres.railway.internal"; // DIRECT private hostname
    var dbPort = "5432";
    var dbName = "railway";
    var dbUser = "postgres";
    var dbPassword = "qFpiRxJeAnquRMmqycYtathxcqWpcHvg";

    connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword};SSL Mode=Prefer;Trust Server Certificate=true;Timeout=30";

    Console.WriteLine($"🚀 Using PostgreSQL PRIVATE NETWORK: {dbHost}");

    builder.Services.AddDbContext<StoreContext>(options =>
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(2), null);
        }));
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

// Apply pending migrations
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var context = services.GetRequiredService<StoreContext>();

    try
    {
        logger.LogInformation("Checking for pending migrations...");

        var pendingMigrations = (await context.Database.GetPendingMigrationsAsync()).ToList();
        logger.LogInformation("Pending migrations: {Count}", pendingMigrations.Count);

        foreach (var migration in pendingMigrations)
        {
            logger.LogInformation("Pending: {Migration}", migration);
        }

        if (pendingMigrations.Any())
        {
            logger.LogInformation("Applying {Count} pending migrations...", pendingMigrations.Count);
            await context.Database.MigrateAsync();
            logger.LogInformation("✅ All migrations applied successfully!");

            // Verify tables were created
            var connection = context.Database.GetDbConnection();
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public'";
            var tableCount = await command.ExecuteScalarAsync();
            logger.LogInformation("✅ Total tables after migration: {TableCount}", tableCount);
        }
        else
        {
            logger.LogInformation("✅ No pending migrations");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Failed to apply migrations");
        throw; // Re-throw to see the error in logs
    }
}

// Health check endpoints
app.MapGet("/", () => "FundTrackr API is running!");
app.MapGet("/health", () => "Healthy");

// Simple connection test
app.MapGet("/simple-test", async () =>
{
    try
    {
        var dbHost = Environment.GetEnvironmentVariable("PGHOST");
        var dbUser = Environment.GetEnvironmentVariable("PGUSER");

        return Results.Json(new
        {
            status = "Environment variables set",
            host = dbHost,
            user = dbUser,
            hasPassword = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PGPASSWORD"))
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new { error = ex.Message });
    }
});
app.MapGet("/network-test", async () =>
{
    var dbHost = Environment.GetEnvironmentVariable("PGHOST");
    var dbPort = Environment.GetEnvironmentVariable("PGPORT") ?? "5432";
    
    try
    {
        using var tcpClient = new System.Net.Sockets.TcpClient();
        var connectTask = tcpClient.ConnectAsync(dbHost, int.Parse(dbPort));
        
        if (await Task.WhenAny(connectTask, Task.Delay(5000)) == connectTask)
        {
            await connectTask;
            return Results.Json(new { 
                status = "TCP Connection SUCCESS", 
                host = dbHost, 
                port = dbPort 
            });
        }
        else
        {
            return Results.Json(new { 
                status = "TCP Connection TIMEOUT", 
                host = dbHost, 
                port = dbPort,
                error = "Cannot reach PostgreSQL server on network level"
            });
        }
    }
    catch (Exception ex)
    {
        return Results.Json(new { 
            status = "TCP Connection FAILED", 
            host = dbHost, 
            port = dbPort,
            error = ex.Message 
        });
    }
});
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

app.MapGet("/check-tables", async (StoreContext context) =>
{
    try
    {
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();

        var tables = new List<object>();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT 
                table_name,
                (SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = 'public' AND table_name = t.table_name) as column_count
            FROM information_schema.tables t 
            WHERE table_schema = 'public' 
            ORDER BY table_name";

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(new
            {
                table = reader.GetString(0),
                columns = reader.GetInt32(1)
            });
        }

        return Results.Json(new
        {
            success = true,
            tables = tables,
            totalTables = tables.Count
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new
        {
            success = false,
            error = ex.Message,
            details = "Database might be empty or tables don't exist"
        });
    }
});
app.MapGet("/check-migrations", async (StoreContext context) =>
{
    try
    {
        var migrations = await context.Database.GetAppliedMigrationsAsync();
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();

        return Results.Json(new
        {
            appliedMigrations = migrations.ToArray(),
            pendingMigrations = pendingMigrations.ToArray(),
            hasPendingMigrations = pendingMigrations.Any()
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new { error = ex.Message });
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