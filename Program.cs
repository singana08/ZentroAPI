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

// Add file logging for Azure
var logPath = Path.Combine(Directory.GetCurrentDirectory(), "logs", "app.log");
Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

static void WriteLog(string message, string level = "INFO")
{
    var logPath = Path.Combine(Directory.GetCurrentDirectory(), "logs", "app.log");
    var logMessage = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}\n";
    File.AppendAllText(logPath, logMessage);
    Console.WriteLine(logMessage.Trim());
}

static void WriteError(string message, Exception? ex = null)
{
    var errorMsg = ex != null ? $"{message} - Exception: {ex.Message}" : message;
    WriteLog(errorMsg, "ERROR");
    if (ex != null)
    {
        WriteLog($"Stack Trace: {ex.StackTrace}", "ERROR");
    }
}

static void WriteWarning(string message)
{
    WriteLog(message, "WARN");
}

WriteLog("=== APPLICATION STARTING ===");

// Add Azure Key Vault configuration - DISABLED until managed identity is configured
var keyVaultUri = builder.Configuration["KeyVault:VaultUri"];
WriteLog($"Key Vault URI: {keyVaultUri}");
WriteLog($"Environment: {builder.Environment.EnvironmentName}");

// Enable Key Vault configuration with detailed debugging
if (!string.IsNullOrEmpty(keyVaultUri))
{
    try
    {
        WriteLog($"Attempting Key Vault connection to: {keyVaultUri}");
        
        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ExcludeEnvironmentCredential = false,
            ExcludeInteractiveBrowserCredential = true,
            ExcludeAzureCliCredential = false,
            ExcludeVisualStudioCredential = false,
            ExcludeVisualStudioCodeCredential = true,
            ExcludeManagedIdentityCredential = false,
            ExcludeSharedTokenCacheCredential = true
        });
        
        WriteLog("DefaultAzureCredential created, testing access...");
        
        // Test Key Vault access first
        var secretClient = new SecretClient(new Uri(keyVaultUri), credential);
        
        try
        {
            WriteLog("Testing Key Vault access with a secret read...");
            var testSecret = await secretClient.GetSecretAsync("StripeSecretKey");
            WriteLog($"Key Vault access successful - StripeSecretKey found: {testSecret.Value.Value?.Length > 0}");
        }
        catch (Exception secretEx)
        {
            WriteError($"Key Vault secret access test failed", secretEx);
        }
        
        builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), credential);
        WriteLog("Key Vault configuration added to builder");
        
        // Test configuration values after adding Key Vault
        var stripeKey = builder.Configuration["StripeSecretKey"];
        var emailSender = builder.Configuration["EmailSenderEmail"];
        var dbConn = builder.Configuration["DatabaseConnectionString"];
        WriteLog($"Config check - Stripe: {(!string.IsNullOrEmpty(stripeKey) ? "Found" : "Empty")}, Email: {(!string.IsNullOrEmpty(emailSender) ? "Found" : "Empty")}, DB: {(!string.IsNullOrEmpty(dbConn) ? "Found" : "Empty")}");
    }
    catch (Exception ex)
    {
        WriteError($"Key Vault setup failed - using appsettings fallback", ex);
    }
}
else
{
    WriteLog("Key Vault URI not found - using appsettings values");
}

/*
// Check managed identity
try
{
    var credential = new DefaultAzureCredential();
    WriteLog("DefaultAzureCredential created successfully");
    
    if (!string.IsNullOrEmpty(keyVaultUri))
    {
        WriteLog("Attempting to connect to Key Vault...");
        
        // Test Key Vault access first
        var secretClient = new SecretClient(new Uri(keyVaultUri), credential);
        WriteLog("SecretClient created, testing access...");
        
        try
        {
            // Try to get a specific secret to test access
            var testSecret = await secretClient.GetSecretAsync("DatabaseConnectionString");
            WriteLog($"Key Vault access successful - DatabaseConnectionString found: {testSecret.Value.Value?.Length > 0}");
        }
        catch (Exception secretEx)
        {
            WriteLog($"Key Vault secret access failed: {secretEx.Message}");
        }
        
        builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), credential);
        WriteLog("Key Vault configuration added to builder");
        
        // Log what secrets we can access after configuration
        var dbConn = builder.Configuration["DatabaseConnectionString"];
        var jwtKey = builder.Configuration["JwtSecretKey"];
        var senderEmail = builder.Configuration["SenderEmail"];
        WriteLog($"Final check - DB: {(!string.IsNullOrEmpty(dbConn) ? "Found" : "Empty")}, JWT: {(!string.IsNullOrEmpty(jwtKey) ? "Found" : "Empty")}, Email: {(!string.IsNullOrEmpty(senderEmail) ? "Found" : "Empty")}");
    }
    else
    {
        WriteLog("Key Vault URI not found in configuration");
    }
}
catch (Exception ex)
{
    WriteLog($"Key Vault setup failed: {ex.Message}");
    WriteLog($"Exception type: {ex.GetType().Name}");
    if (ex.InnerException != null)
    {
        WriteLog($"Inner exception: {ex.InnerException.Message}");
    }
}
*/

// Add services to the container
var connectionString = builder.Configuration["DatabaseConnectionString"] 
    ?? builder.Configuration.GetConnectionString("DefaultConnection");
WriteLog($"Using connection string from: {(builder.Configuration["DatabaseConnectionString"] != null ? "Key Vault" : "appsettings")}");

Console.WriteLine($"Connection String: {(string.IsNullOrEmpty(connectionString) ? "EMPTY" : "Found")}");
if (!string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine($"DB Host: {(connectionString.Contains("localhost") ? "localhost" : "remote")}");
}
else
{
    Console.WriteLine("WARNING: Database connection string is empty!");
}

try
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));
    Console.WriteLine("DbContext configured successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"DbContext configuration failed: {ex.Message}");
}

// Add caching
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ICacheService, CacheService>();

// Add Azure Blob Storage
var azureConnectionString = builder.Configuration["AzureBlobStorageConnectionString"];

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
Console.WriteLine("=== Registering services ===");
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<IEmailService, EmailService>();
// Add JWT service with error handling
try
{
    builder.Services.AddScoped<IJwtService, SecureJwtService>();
    WriteLog("JWT service registered successfully");
}
catch (Exception ex)
{
    WriteLog($"JWT service registration failed: {ex.Message}");
    // Register a fallback JWT service if needed
}
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
Console.WriteLine("CategoryService registered");
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
builder.Services.AddScoped<IPushNotificationService, PushNotificationService>();
builder.Services.AddScoped<IReferralService, ReferralService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddHostedService<ReferralBackgroundService>();
builder.Services.AddHttpClient<NotificationService>();
builder.Services.AddHttpClient<PushNotificationService>();

// Add SignalR
builder.Services.AddSignalR();

// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry();

// Add logging
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.AddDebug();
    config.AddApplicationInsights();
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

// Add authentication - Use same key source as SecureJwtService
var secretKey = builder.Configuration["JwtSecretKey"] 
    ?? builder.Configuration["JwtSettings:SecretKey"] 
    ?? "TempSecretKeyForStartup123456789012345678901234567890";
var issuer = builder.Configuration["JwtSettings:Issuer"] ?? "ZentroAPI";
var audience = builder.Configuration["JwtSettings:Audience"] ?? "ZentroMobileApp";

Console.WriteLine($"JWT Config - Using secret from: {(builder.Configuration["JwtSecretKey"] != null ? "Key Vault" : "appsettings")}");
Console.WriteLine($"JWT Config - Issuer: {issuer}, Audience: {audience}");

// Ensure minimum key length
if (secretKey.Length < 32)
{
    secretKey = "TempSecretKeyForStartup123456789012345678901234567890";
    Console.WriteLine("JWT Config - Using fallback secret key");
}

// Use same key encoding logic as SecureJwtService
byte[] key;
try
{
    // Try Base64 first (recommended)
    key = Convert.FromBase64String(secretKey);
    Console.WriteLine("JWT Config - Using Base64 decoded key");
}
catch
{
    // Fallback to UTF8 encoding
    key = Encoding.UTF8.GetBytes(secretKey);
    Console.WriteLine("JWT Config - Using UTF8 encoded key");
}

Console.WriteLine($"JWT Config - Key length: {key.Length} bytes");

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
Console.WriteLine("=== Configuring HTTP pipeline ===");

// Add global exception handler
app.UseExceptionHandler("/error");
app.Map("/error", (HttpContext context) =>
{
    var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
    Console.WriteLine($"=== GLOBAL EXCEPTION: {exception?.Message} ===");
    Console.WriteLine($"=== STACK TRACE: {exception?.StackTrace} ===");
    return Results.Problem("An error occurred");
});

// Add API logging
app.UseMiddleware<ZentroAPI.Middleware.ApiLoggingMiddleware>();

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

app.MapGet("/", () => {
    Console.WriteLine($"=== ROOT ENDPOINT HIT: {DateTime.UtcNow} ===");
    return Results.Ok(new { message = "Zentro API is running", timestamp = DateTime.UtcNow });
});

app.MapGet("/debug", (IConfiguration config) => {
    WriteLog($"=== DEBUG ENDPOINT HIT: {DateTime.UtcNow} ===");
    
    var debugInfo = new {
        status = "debug",
        timestamp = DateTime.UtcNow,
        environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
        configuration = new {
            keyVaultUri = config["KeyVault:VaultUri"],
            hasStripeKey = !string.IsNullOrEmpty(config["StripeSecretKey"]),
            hasEmailSender = !string.IsNullOrEmpty(config["EmailSenderEmail"]),
            hasDbConnection = !string.IsNullOrEmpty(config["DatabaseConnectionString"]) || !string.IsNullOrEmpty(config.GetConnectionString("DefaultConnection")),
            hasJwtKey = !string.IsNullOrEmpty(config["JwtSecretKey"]) || !string.IsNullOrEmpty(config["JwtSettings:SecretKey"])
        },
        endpoints = new {
            logs = "/logs",
            swagger = "/swagger",
            health = "/api/health/status"
        }
    };
    
    return Results.Ok(debugInfo);
});

app.MapGet("/logs", () => {
    try {
        var logPath = Path.Combine(Directory.GetCurrentDirectory(), "logs", "app.log");
        if (File.Exists(logPath)) {
            var logs = File.ReadAllText(logPath);
            var htmlLogs = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Zentro API Logs</title>
    <style>
        body {{ font-family: 'Courier New', monospace; background: #1e1e1e; color: #d4d4d4; margin: 20px; }}
        .log-container {{ background: #2d2d30; padding: 20px; border-radius: 5px; }}
        .log-line {{ margin: 2px 0; padding: 2px 5px; }}
        .error {{ background: #3c1e1e; color: #f48771; }}
        .warning {{ background: #3c3c1e; color: #dcdcaa; }}
        .info {{ color: #9cdcfe; }}
        .timestamp {{ color: #608b4e; }}
        .endpoint {{ color: #c586c0; font-weight: bold; }}
        h1 {{ color: #4ec9b0; }}
    </style>
</head>
<body>
    <h1>Zentro API Application Logs</h1>
    <div class='log-container'>
        <pre>{System.Web.HttpUtility.HtmlEncode(logs).Replace("\n", "<br>")}</pre>
    </div>
    <script>
        // Auto-refresh every 10 seconds
        setTimeout(() => location.reload(), 10000);
    </script>
</body>
</html>";
            return Results.Content(htmlLogs, "text/html");
        }
        return Results.Text("No logs found", "text/plain");
    } catch (Exception ex) {
        return Results.Text($"Error reading logs: {ex.Message}", "text/plain");
    }
});

WriteLog("=== APPLICATION STARTUP COMPLETE ===");
WriteLog($"=== Environment: {app.Environment.EnvironmentName} ===");
WriteLog($"=== Timestamp: {DateTime.UtcNow} ===");

app.Run();