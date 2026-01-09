using FluentValidation;
using nLogMonitor.Api.Middleware;
using nLogMonitor.Api.Validators;
using nLogMonitor.Application.Configuration;
using nLogMonitor.Application.DTOs;
using nLogMonitor.Application.Interfaces;
using nLogMonitor.Application.Services;
using nLogMonitor.Infrastructure.Export;
using nLogMonitor.Infrastructure.FileSystem;
using nLogMonitor.Infrastructure.Parsing;
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
    builder.Services.AddSwaggerGen();

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

    // Configuration
    builder.Services.Configure<FileSettings>(
        builder.Configuration.GetSection(FileSettings.SectionName));
    builder.Services.Configure<SessionSettings>(
        builder.Configuration.GetSection(SessionSettings.SectionName));
    builder.Services.Configure<RecentLogsSettings>(
        builder.Configuration.GetSection(RecentLogsSettings.SectionName));

    // Application Services
    builder.Services.AddScoped<ILogParser, NLogParser>();
    builder.Services.AddSingleton<ISessionStorage, InMemorySessionStorage>();
    builder.Services.AddScoped<ILogService, LogService>();
    builder.Services.AddScoped<IDirectoryScanner, DirectoryScanner>();
    builder.Services.AddSingleton<IRecentLogsRepository, RecentLogsFileRepository>();

    // Exporters
    builder.Services.AddScoped<ILogExporter, JsonExporter>();
    builder.Services.AddScoped<ILogExporter, CsvExporter>();

    // TODO: FileWatcherService will be added in Phase 6
    // builder.Services.AddSingleton<IFileWatcherService, FileWatcherService>();

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
    app.UseAuthorization();

    app.MapControllers();

    // Health check endpoint
    app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
       .WithName("HealthCheck");

    // TODO: Map SignalR hub (will be added in Phase 6)
    // app.MapHub<LogWatcherHub>("/hubs/logwatcher");

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
