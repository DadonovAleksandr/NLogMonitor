using NLog;
using NLog.Web;

// Early init of NLog to allow startup errors to be logged
var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{
    logger.Info("Starting NLogMonitor API...");

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

    // TODO: Register application services (will be added in later phases)
    // builder.Services.AddScoped<ILogParser, NLogParser>();
    // builder.Services.AddSingleton<ISessionStorage, InMemorySessionStorage>();
    // builder.Services.AddScoped<ILogService, LogService>();
    // builder.Services.AddSingleton<IFileWatcherService, FileWatcherService>();
    // builder.Services.AddSingleton<IRecentLogsRepository, RecentLogsFileRepository>();

    var app = builder.Build();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "NLogMonitor API v1");
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

    logger.Info("NLogMonitor API started successfully");
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
