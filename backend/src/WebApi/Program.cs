using Application.Extensions;
using Application.DTOs.Auth;
using Infrastructure.Extensions;
using Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;
using WebApi.Configuration;
using WebApi.Middleware;
using Serilog;

// Configure Serilog early for startup logging
SerilogConfiguration.CreateBootstrapLogger();

try
{
    Log.Information("Starting web application");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog
    SerilogConfiguration.ConfigureSerilog(builder);

    // Configure Dapper type handlers
    DapperConfiguration.ConfigureTypeHandlers();

    // Configure JWT Settings
    var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
        ?? throw new InvalidOperationException("JwtSettings not found in configuration");
    builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

    // Add JWT Authentication
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ClockSkew = TimeSpan.Zero // No tolerance for token expiration
        };
    });

    // Add Authorization Policies
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", policy =>
            policy.RequireRole("Admin", "HR", "Accountant"));

        options.AddPolicy("AdminHrOnly", policy =>
            policy.RequireRole("Admin", "HR"));

        options.AddPolicy("EmployeeOnly", policy =>
            policy.RequireRole("Employee"));

        options.AddPolicy("ManagerOrAbove", policy =>
            policy.RequireRole("Admin", "HR", "Accountant", "Manager"));
    });

    // Add layer services using extension methods
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApplication();

    // Add framework services
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        });
    builder.Services.AddEndpointsApiExplorer();
    
    // Add CORS configuration
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowReactApp",
            policy =>
            {
                var environment = builder.Environment;
                var configuration = builder.Configuration;

                if (environment.IsDevelopment())
                {
                    // Development: Allow localhost origins, network IP, and domain origins
                    policy.WithOrigins(
                            "http://localhost:5173",
                            "http://localhost:3000",
                            "http://localhost:3001",
                            "http://localhost:5174",
                            "http://127.0.0.1:5173",
                            "http://127.0.0.1:3000",
                            "http://127.0.0.1:3001",
                            "http://192.168.86.250:3000",
                            "http://192.168.86.250:3001",
                            "http://192.168.86.250:5173",
                            "http://192.168.86.50:3000",
                            "http://192.168.86.50:3001",
                            "http://192.168.86.50:5173",
                            "https://rcmr.xcdify.com:3000",
                            "https://rcmr.xcdify.com",
                            "https://employee.rcmr.xcdify.com"
                          )
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials()
                          .SetPreflightMaxAge(TimeSpan.FromSeconds(3600));
                }
                else
                {
                    // Production: Only allow configured origins
                    var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
                    if (allowedOrigins == null || allowedOrigins.Length == 0)
                    {
                        throw new InvalidOperationException(
                            "CORS allowed origins must be configured in production. " +
                            "Set 'Cors:AllowedOrigins' in appsettings.json or environment variables.");
                    }

                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials()
                          .SetPreflightMaxAge(TimeSpan.FromSeconds(3600));
                }
            });
    });
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Generated API",
            Version = "v1",
            Description = "Auto-generated Clean Architecture API from PostgreSQL schema"
        });

        // Add JWT Authentication to Swagger
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
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
                Array.Empty<string>()
            }
        });

        // Include XML comments if available
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }
    });

    var app = builder.Build();

    // Add correlation ID middleware (must be early in pipeline)
    app.UseCorrelationId();

    // Configure the HTTP request pipeline
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Generated API V1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at the app's root
    });

    // Enable CORS early in pipeline (before other middleware)
    app.UseCors("AllowReactApp");
    
    // Add exception handler middleware (must be early to catch all exceptions)
    app.UseMiddleware<ExceptionHandlerMiddleware>();

    // Configure Serilog request logging (after exception handler so it can log exceptions)
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms";
        options.GetLevel = (httpContext, elapsed, ex) => ex != null 
            ? Serilog.Events.LogEventLevel.Error 
            : httpContext.Response.StatusCode > 499 
                ? Serilog.Events.LogEventLevel.Error
                : Serilog.Events.LogEventLevel.Information;
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            var requestHost = httpContext.Request.Host.HasValue ? httpContext.Request.Host.ToString() : string.Empty;
            var requestScheme = httpContext.Request.Scheme ?? string.Empty;
            var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

            diagnosticContext.Set("RequestHost", requestHost);
            diagnosticContext.Set("RequestScheme", requestScheme);
            diagnosticContext.Set("RemoteIpAddress", remoteIp);
        };
        // Include query string in request path for better logging
        options.IncludeQueryInRequestPath = true;
    });
    
    app.UseHttpsRedirection();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    Log.Information("Web application configured and ready to start");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.Information("Shutting down web application");
    Log.CloseAndFlush();
}