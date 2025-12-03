using ZentroAPI.Data;
using ZentroAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

var builder = WebApplication.CreateBuilder(args);

// Add Azure Key Vault configuration
if (!builder.Environment.IsDevelopment())
{
    var keyVaultUri = builder.Configuration["KeyVault:VaultUri"];
    if (!string.IsNullOrEmpty(keyVaultUri))
    {
        var credential = new DefaultAzureCredential();
        builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), credential);
    }
}

// Add services to the container
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? builder.Configuration["DatabaseConnectionString"];

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add caching
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ICacheService, CacheService>();

// Add Azure Blob Storage
var azureConnectionString = builder.Configuration["AzureBlobStorageConnectionString"]
    ?? builder.Configuration.GetConnectionString("AzureBlobStorage") 
    ?? builder.Configuration["AzureBlobStorage:ConnectionString"];

if (!string.IsNullOrEmpty(azureConnectionString))
{
    builder.Services.AddSingleton(x => new Azure.Storage.Blobs.BlobServiceClient(azureConnectionString));
    builder.Services.AddScoped<IAzureBlobService, AzureBlobService>();
}
else
{
    // Fallback to local storage if Azure not configured
    builder.Services.AddScoped<IAzureBlobService, LocalFileService>();
}

// Add custom services
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IJwtService, SecureJwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IServiceRequestService, ServiceRequestService>();
builder.Services.AddScoped<IAddressService, AddressService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IQuoteService, QuoteService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IAgreementService, AgreementService>();
builder.Services.AddScoped<IWorkflowService, WorkflowService>();
builder.Services.AddScoped<IEarningsService, EarningsService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddScoped<IStripePaymentService, StripePaymentService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IRealTimeNotificationService, RealTimeNotificationService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<IProviderMatchingService, ProviderMatchingService>();
builder.Services.AddScoped<IProviderService, ProviderService>();
builder.Services.AddScoped<IHealthCheckService, HealthCheckService>();
builder.Services.AddHttpClient<NotificationService>();

// Add SignalR
builder.Services.AddSignalR();

// Add logging
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.AddDebug();
});

// Add controllers
builder.Services.AddControllers();

// Add CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
    ?? new[] { "http://localhost:3000", "http://localhost:3001", "http://localhost:8081", "http://localhost:8000", "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMobileApp", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Add authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = builder.Configuration["JwtSecretKey"] 
    ?? jwtSettings.GetValue<string>("SecretKey") 
    ?? "TempSecretKeyForStartup123456789012345678901234567890";
var issuer = jwtSettings.GetValue<string>("Issuer") ?? "ZentroAPI";
var audience = jwtSettings.GetValue<string>("Audience") ?? "ZentroMobileApp";

// Ensure minimum key length
if (secretKey.Length < 32)
{
    secretKey = "TempSecretKeyForStartup123456789012345678901234567890";
}

var key = Encoding.ASCII.GetBytes(secretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"JWT Authentication failed: {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine("JWT Token validated successfully");
            return Task.CompletedTask;
        },
        OnMessageReceived = context =>
        {
            // Allow SignalR to get token from query string
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chathub"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
    
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Add authorization
builder.Services.AddAuthorization();



// Add Swagger/OpenAPI
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Zentro API",
        Version = "v1",
        Description = "Email OTP Authentication API with JWT support",
        Contact = new OpenApiContact
        {
            Name = "Zentro Support",
            Email = "support@zentro.com"
        }
    });

    // Add JWT Bearer authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by a space and then your token"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });

    // Add XML comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Configure Kestrel to listen on specific ports
// Use appsettings.json or environment variables for URL configuration

// Build the app
var app = builder.Build();

// Apply migrations and initialize database
try
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            
            // Ensure the schema exists before applying migrations
            context.Database.ExecuteSqlRaw("CREATE SCHEMA IF NOT EXISTS zentro_api;");
            Console.WriteLine("Schema zentro_api created/verified");
            
            // Apply migrations
            context.Database.Migrate();
            Console.WriteLine("Database migrated successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database migration failed: {ex.Message}");
            // Continue startup even if migration fails
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Database initialization failed: {ex.Message}");
    // Continue startup even if database initialization fails
}

// Configure the HTTP request pipeline
// Add global error handling
app.UseMiddleware<ZentroAPI.Middleware.ErrorHandlingMiddleware>();

// Add performance monitoring
app.UseMiddleware<ZentroAPI.Middleware.PerformanceMiddleware>();

// Add rate limiting - TEMPORARILY DISABLED
// app.UseMiddleware<ZentroAPI.Middleware.RateLimitingMiddleware>();

// Add request logging in development
if (app.Environment.IsDevelopment())
{
    app.UseMiddleware<ZentroAPI.Middleware.RequestLoggingMiddleware>();
}

// Enable Swagger in all environments for testing
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Zentro API v1");
    c.RoutePrefix = "swagger";
    c.DisplayRequestDuration();
});

// Don't enforce HTTPS in Docker environment
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowMobileApp");

// Serve static files (uploaded images)
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map SignalR hub
app.MapHub<ZentroAPI.Hubs.ChatHub>("/chathub");

// Map health check endpoints
app.MapGet("/api/health/ping", () => Results.Ok(new { status = "ok", timestamp = DateTime.UtcNow }))
    .WithName("HealthPing");

app.MapGet("/api/health/status", () => Results.Ok(new 
{ 
    status = "healthy", 
    service = "ZentroAPI", 
    version = "1.0.0", 
    timestamp = DateTime.UtcNow 
}))
    .WithName("HealthStatus");

app.MapGet("/", () => Results.Ok(new { message = "Zentro API is running", timestamp = DateTime.UtcNow }));

app.Run();