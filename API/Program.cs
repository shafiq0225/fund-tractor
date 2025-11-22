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

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddScoped<IAmfiExcelDownloadService, AmfiExcelDownloadService>();
builder.Services.AddScoped<IMarketHolidayProvider, MarketHolidayProvider>();
builder.Services.AddHostedService<DailyNavDownloaderService>();

builder.Services.AddScoped<IAmfiNavService, AmfiNavService>();
builder.Services.AddHostedService<AmfiNavBackgroundService>();
builder.Services.AddControllers();
builder.Services.AddHttpClient();


builder.Services.AddDbContext<StoreContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IAmfiRepository, AmfiRepository>();
builder.Services.AddHttpContextAccessor();
// NEW: Authentication Services
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IInvestmentRepository, InvestmentRepository>();
builder.Services.AddScoped<ValidateUserStatusFilter>();

// NEW: JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!))
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

            // This will be validated in each request using a custom middleware or action filter
            return true;
        }));
});

builder.Services.AddCors();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// ADD THESE HEALTH ENDPOINTS RIGHT HERE:
app.MapGet("/", () => "FundTrackr API is running!");
app.MapGet("/health", () => "Healthy");
app.MapGet("/test", () => new { message = "API is working", time = DateTime.UtcNow });

app.UseMiddleware<ExceptionMiddleware>();

app.UseCors(x => x
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()
    .WithOrigins(
        "http://localhost:4200", 
        "https://localhost:4200",
        "https://cerulean-dango-230e36.netlify.app"  // Your Netlify URL
    ));
    
// NEW: Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
