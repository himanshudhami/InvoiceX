using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Filters;
using Serilog.Sinks.SystemConsole.Themes;
using Destructurama;

namespace WebApi.Configuration
{
    /// <summary>
    /// Serilog configuration with structured logging and sensitive data protection
    /// </summary>
    public static class SerilogConfiguration
    {
        /// <summary>
        /// Configure Serilog with best practices for production use
        /// </summary>
        public static void ConfigureSerilog(WebApplicationBuilder builder)
        {
            var configuration = builder.Configuration;
            var environment = builder.Environment;

            // Remove default logging providers
            builder.Logging.ClearProviders();

            // Configure Serilog
            builder.Host.UseSerilog((context, services, loggerConfig) =>
            {
                loggerConfig
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("ApplicationName", "InvoiceApp")
                    .Enrich.WithProperty("Environment", environment.EnvironmentName)
                    .Enrich.WithMachineName()
                    .Enrich.WithProcessId()
                    .Enrich.WithThreadId()
                    .Destructure.UsingAttributes()
                    .ConfigureSensitiveDataMasking()
                    .ConfigureLogLevel(environment)
                    .ConfigureSinks(environment);
            });
        }

        private static LoggerConfiguration ConfigureSensitiveDataMasking(this LoggerConfiguration loggerConfig)
        {
            return loggerConfig
                .Filter.ByExcluding(Matching.WithProperty<string>("RequestPath", path => 
                    path != null && (path.StartsWith("/health") || 
                    path.StartsWith("/metrics") ||
                    path.StartsWith("/_framework"))))
                // Don't limit exception details - show full stack traces
                // Transform objects with sensitive properties
                .Destructure.ByTransformingWhere<object>(
                    obj => obj.GetType().GetProperties().Any(p => 
                        p.Name.ToLowerInvariant().Contains("password") ||
                        p.Name.ToLowerInvariant().Contains("token") ||
                        p.Name.ToLowerInvariant().Contains("secret") ||
                        p.Name.ToLowerInvariant().Contains("key")),
                    obj => "[SENSITIVE DATA REDACTED]");
        }

        private static LoggerConfiguration ConfigureLogLevel(this LoggerConfiguration loggerConfig, IWebHostEnvironment? environment = null)
        {
            var config = loggerConfig
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                // Ensure exception handler middleware logs are visible
                .MinimumLevel.Override("WebApi.Middleware.ExceptionHandlerMiddleware", LogEventLevel.Error);

            // In Development, show more detailed logs
            if (environment?.IsDevelopment() == true)
            {
                config = config
                    .MinimumLevel.Override("WebApi", LogEventLevel.Debug)
                    .MinimumLevel.Override("Application", LogEventLevel.Debug)
                    .MinimumLevel.Override("Infrastructure", LogEventLevel.Debug)
                    .MinimumLevel.Override("WebApi.Middleware.ExceptionHandlerMiddleware", LogEventLevel.Debug);
            }

            return config;
        }

        private static LoggerConfiguration ConfigureSinks(this LoggerConfiguration loggerConfig, IWebHostEnvironment environment)
        {
            if (environment.IsDevelopment())
            {
                // Development - Console with colors and detailed exception output
                loggerConfig
                    .WriteTo.Console(
                        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                        theme: AnsiConsoleTheme.Literate)
                    .WriteTo.File(
                        path: "logs/app-development-.log",
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 7,
                        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");
            }
            else
            {
                // Production - Readable console output (not JSON) for easier debugging, JSON for files
                loggerConfig
                    .WriteTo.Console(
                        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                        theme: AnsiConsoleTheme.Literate)
                    .WriteTo.File(
                        new Serilog.Formatting.Json.JsonFormatter(),
                        path: "logs/app-.log",
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 30,
                        fileSizeLimitBytes: 100 * 1024 * 1024, // 100MB
                        rollOnFileSizeLimit: true);
            }

            return loggerConfig;
        }

        /// <summary>
        /// Create a logger for early application startup before DI is configured
        /// </summary>
        public static void CreateBootstrapLogger()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateBootstrapLogger();
        }
    }
}