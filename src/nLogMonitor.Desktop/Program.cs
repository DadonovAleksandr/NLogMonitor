using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using nLogMonitor.Application.Configuration;
using nLogMonitor.Application.DTOs;
using nLogMonitor.Application.Interfaces;
using nLogMonitor.Application.Services;
using nLogMonitor.Desktop.Hubs;
using nLogMonitor.Desktop.Middleware;
using nLogMonitor.Desktop.Services;
using nLogMonitor.Desktop.Validators;
using nLogMonitor.Infrastructure.Export;
using nLogMonitor.Infrastructure.FileSystem;
using nLogMonitor.Infrastructure.Parsing;
using nLogMonitor.Infrastructure.Storage;
using NLog;
using NLog.Web;
using Photino.NET;

namespace nLogMonitor.Desktop;

public class Program
{
    private static PhotinoWindow? _mainWindow;
    private static WebApplication? _webApp;
    private static readonly CancellationTokenSource _cts = new();
    private static int _serverPort;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [STAThread]
    public static void Main(string[] args)
    {
        var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

        try
        {
            logger.Info("Starting nLogMonitor Desktop...");

            // Find available port for embedded server
            _serverPort = GetAvailablePort();
            logger.Info($"Using port {_serverPort} for embedded server");

            // Start embedded ASP.NET Core server in background
            var serverTask = Task.Run(() => StartEmbeddedServer(args, _cts.Token));

            // Wait for server to start
            Thread.Sleep(1000);

            // Determine content source (dev mode or production)
            var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
            var contentUrl = isDevelopment
                ? "http://localhost:5173" // Vite dev server
                : $"http://localhost:{_serverPort}"; // Embedded server serves static files

            logger.Info($"Loading content from: {contentUrl}");

            // Create Photino window
            _mainWindow = new PhotinoWindow()
                .SetTitle("nLogMonitor")
                .SetSize(1400, 900)
                .SetMinSize(800, 600)
                .Center()
                .SetDevToolsEnabled(isDevelopment)
                .SetContextMenuEnabled(isDevelopment)
                .RegisterWebMessageReceivedHandler(OnWebMessageReceived)
                .RegisterWindowClosingHandler(OnWindowClosing)
                .Load(contentUrl);

            // Run the window (blocks until closed)
            _mainWindow.WaitForClose();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Application stopped due to exception");
            throw;
        }
        finally
        {
            // Cleanup
            _cts.Cancel();
            _webApp?.StopAsync().Wait(TimeSpan.FromSeconds(5));
            LogManager.Shutdown();
        }
    }

    private static void StartEmbeddedServer(string[] args, CancellationToken cancellationToken)
    {
        var logger = LogManager.GetCurrentClassLogger();

        try
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure Kestrel to listen on specific port
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Listen(IPAddress.Loopback, _serverPort);
            });

            // NLog: Setup NLog for Dependency injection
            builder.Logging.ClearProviders();
            builder.Host.UseNLog();

            // Add services to the container
            builder.Services.AddControllers();

            // Swagger/OpenAPI (only in development)
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
                {
                    Version = "v1",
                    Title = "nLogMonitor API",
                    Description = "API для просмотра и анализа NLog-логов (Desktop)"
                });
            });

            // CORS - allow requests from Photino WebView
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowPhotino", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            // SignalR for real-time updates
            builder.Services.AddSignalR();

            // FluentValidation
            builder.Services.AddScoped<IValidator<FilterOptionsDto>, FilterOptionsValidator>();
            builder.Services.AddScoped<IValidator<ClientLogDto>, ClientLogDtoValidator>();

            // Rate Limiting for client logs endpoint
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy("ClientLogs", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: "desktop",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 100,
                            Window = TimeSpan.FromMinutes(1)
                        }));
            });

            // Configuration - set Desktop mode
            builder.Services.Configure<FileSettings>(
                builder.Configuration.GetSection(FileSettings.SectionName));
            builder.Services.Configure<SessionSettings>(
                builder.Configuration.GetSection(SessionSettings.SectionName));
            builder.Services.Configure<RecentLogsSettings>(
                builder.Configuration.GetSection(RecentLogsSettings.SectionName));

            // Override AppSettings to Desktop mode
            builder.Services.Configure<AppSettings>(options =>
            {
                options.Mode = AppMode.Desktop;
            });

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

            _webApp = builder.Build();

            // Global exception handling middleware
            _webApp.UseExceptionHandling();

            // Configure the HTTP request pipeline
            if (_webApp.Environment.IsDevelopment())
            {
                _webApp.UseSwagger();
                _webApp.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "nLogMonitor API v1");
                    options.RoutePrefix = "swagger";
                });
            }

            _webApp.UseCors("AllowPhotino");
            _webApp.UseRateLimiter();

            // Serve static files from wwwroot (production mode)
            _webApp.UseDefaultFiles();
            _webApp.UseStaticFiles();

            _webApp.UseAuthorization();
            _webApp.MapControllers();

            // SignalR Hub for real-time log updates
            _webApp.MapHub<LogWatcherHub>("/hubs/logwatcher");

            // Health check endpoint
            _webApp.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
               .WithName("HealthCheck");

            logger.Info($"Embedded server started on http://localhost:{_serverPort}");
            _webApp.Run();
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.Error(ex, "Embedded server failed to start");
            throw;
        }
    }

    private static void OnWebMessageReceived(object? sender, string message)
    {
        var logger = LogManager.GetCurrentClassLogger();
        logger.Debug($"Received message from WebView: {message}");

        try
        {
            var request = JsonSerializer.Deserialize<BridgeRequest>(message, _jsonOptions);
            if (request == null) return;

            BridgeResponse response = request.Command switch
            {
                "isDesktop" => new BridgeResponse
                {
                    RequestId = request.RequestId,
                    Success = true,
                    Data = true
                },
                "getServerPort" => new BridgeResponse
                {
                    RequestId = request.RequestId,
                    Success = true,
                    Data = _serverPort
                },
                "showOpenFile" => HandleOpenFileDialog(request),
                "showOpenFolder" => HandleOpenFolderDialog(request),
                _ => new BridgeResponse
                {
                    RequestId = request.RequestId,
                    Success = false,
                    Error = $"Unknown command: {request.Command}"
                }
            };

            var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
            _mainWindow?.SendWebMessage(responseJson);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error processing web message");

            var errorResponse = new BridgeResponse
            {
                RequestId = null,
                Success = false,
                Error = ex.Message
            };
            _mainWindow?.SendWebMessage(JsonSerializer.Serialize(errorResponse, _jsonOptions));
        }
    }

    private static BridgeResponse HandleOpenFileDialog(BridgeRequest request)
    {
        if (_mainWindow == null)
        {
            return new BridgeResponse
            {
                RequestId = request.RequestId,
                Success = false,
                Error = "Window not initialized"
            };
        }

        var filters = new[]
        {
            ("Log files", new[] { "*.log" }),
            ("Text files", new[] { "*.txt" }),
            ("All files", new[] { "*.*" })
        };

        var results = _mainWindow.ShowOpenFile(
            title: "Выберите лог-файл",
            defaultPath: null,
            multiSelect: false,
            filters: filters
        );

        var selectedPath = results?.FirstOrDefault();

        return new BridgeResponse
        {
            RequestId = request.RequestId,
            Success = true,
            Data = selectedPath // null if cancelled
        };
    }

    private static BridgeResponse HandleOpenFolderDialog(BridgeRequest request)
    {
        if (_mainWindow == null)
        {
            return new BridgeResponse
            {
                RequestId = request.RequestId,
                Success = false,
                Error = "Window not initialized"
            };
        }

        var results = _mainWindow.ShowOpenFolder(
            title: "Выберите директорию с логами",
            defaultPath: null,
            multiSelect: false
        );

        var selectedPath = results?.FirstOrDefault();

        return new BridgeResponse
        {
            RequestId = request.RequestId,
            Success = true,
            Data = selectedPath // null if cancelled
        };
    }

    private static bool OnWindowClosing(object? sender, EventArgs e)
    {
        var logger = LogManager.GetCurrentClassLogger();
        logger.Info("Window closing...");

        // Cancel the server
        _cts.Cancel();

        // Return false to allow window to close
        return false;
    }

    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}

// Bridge communication models
public class BridgeRequest
{
    public string? RequestId { get; set; }
    public string Command { get; set; } = string.Empty;
    public JsonElement? Data { get; set; }
}

public class BridgeResponse
{
    public string? RequestId { get; set; }
    public bool Success { get; set; }
    public object? Data { get; set; }
    public string? Error { get; set; }
}
