using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.RateLimiting;
using nLogMonitor.Api.Hubs;
using nLogMonitor.Api.Middleware;
using nLogMonitor.Api.Services;
using nLogMonitor.Api.Validators;
using nLogMonitor.Application.Configuration;
using nLogMonitor.Application.DTOs;
using nLogMonitor.Application.Interfaces;
using nLogMonitor.Application.Services;
using nLogMonitor.Infrastructure.Export;
using nLogMonitor.Infrastructure.FileSystem;
using nLogMonitor.Infrastructure.Parsing;
using nLogMonitor.Infrastructure.Services;
using nLogMonitor.Infrastructure.Storage;
using NLog;
using NLog.Web;

// Early init of NLog to allow startup errors to be logged
var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{
    logger.Info("Starting nLogMonitor API...");

    var builder = WebApplication.CreateBuilder(args);

    // NLog: Setup NLog for Dependency injection
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // Add services to the container
    builder.Services.AddControllers();

    // Swagger/OpenAPI
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
        {
            Version = "v1",
            Title = "nLogMonitor API",
            Description = "API для просмотра и анализа NLog-логов"
        });

        // Include XML comments
        var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }
    });

    // CORS
    var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? new[] { "http://localhost:5173" };

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins(corsOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // Required for SignalR
        });
    });

    // SignalR for real-time updates
    builder.Services.AddSignalR();

    // FluentValidation
    builder.Services.AddScoped<IValidator<FilterOptionsDto>, FilterOptionsValidator>();
    builder.Services.AddScoped<IValidator<ClientLogDto>, ClientLogDtoValidator>();

    // Rate Limiting for client logs endpoint (100 requests per minute per IP)
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.AddPolicy("ClientLogs", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                }));
    });

    // Configuration
    builder.Services.Configure<FileSettings>(
        builder.Configuration.GetSection(FileSettings.SectionName));
    builder.Services.Configure<SessionSettings>(
        builder.Configuration.GetSection(SessionSettings.SectionName));
    builder.Services.Configure<RecentLogsSettings>(
        builder.Configuration.GetSection(RecentLogsSettings.SectionName));
    builder.Services.Configure<AppSettings>(
        builder.Configuration.GetSection(AppSettings.SectionName));

    // Application Services
    builder.Services.AddScoped<ILogParser, NLogParser>();
    builder.Services.AddSingleton<ISessionStorage, InMemorySessionStorage>();
    builder.Services.AddScoped<ILogService, LogService>();
    builder.Services.AddScoped<IDirectoryScanner, DirectoryScanner>();
    builder.Services.AddSingleton<IRecentLogsRepository, RecentLogsFileRepository>();

    // Exporters
    builder.Services.AddScoped<ILogExporter, JsonExporter>();
    builder.Services.AddScoped<ILogExporter, CsvExporter>();

    // FileWatcher for real-time monitoring
    builder.Services.AddSingleton<IFileWatcherService, FileWatcherService>();

    // Background service for FileWatcher -> SignalR integration
    builder.Services.AddHostedService<FileWatcherBackgroundService>();

    // User Settings Service
    builder.Services.AddSingleton<IUserSettingsService, UserSettingsService>();

    var app = builder.Build();

    // Global exception handling middleware (should be first)
    app.UseExceptionHandling();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "nLogMonitor API v1");
            options.RoutePrefix = "swagger";
        });
    }

    app.UseHttpsRedirection();
    app.UseCors("AllowFrontend");

    // Rate limiting middleware
    app.UseRateLimiter();

    // Serve static files from wwwroot (production mode)
    app.UseDefaultFiles(); // Serves index.html by default
    app.UseStaticFiles();

    app.UseAuthorization();

    app.MapControllers();

    // SignalR Hub for real-time log updates
    app.MapHub<LogWatcherHub>("/hubs/logwatcher");

    // Health check endpoint
    app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
       .WithName("HealthCheck");

    logger.Info("nLogMonitor API started successfully");
    app.Run();
}
catch (Exception ex)
{
    logger.Error(ex, "Application stopped due to exception");
    throw;
}
finally
{
    LogManager.Shutdown();
}

// Make the implicit Program class public so test projects can access it
public partial class Program { }
