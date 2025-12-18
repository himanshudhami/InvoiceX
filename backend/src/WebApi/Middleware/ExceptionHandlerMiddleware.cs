using System.Net;
using System.Text.Json;
using System.Linq;

namespace WebApi.Middleware;

public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;

    public ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Log full exception details with Serilog (includes stack trace automatically)
            // Using LogError with exception object ensures full stack trace is included
            _logger.LogError(ex, 
                "An unhandled exception occurred while processing {RequestMethod} {RequestPath}",
                context.Request.Method, 
                context.Request.Path);
            
            // Also log inner exception separately if it exists
            if (ex.InnerException != null)
            {
                _logger.LogError(ex.InnerException, 
                    "Inner exception for {RequestMethod} {RequestPath}",
                    context.Request.Method,
                    context.Request.Path);
            }
            
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Ensure CORS headers are set even on errors
        if (!context.Response.Headers.ContainsKey("Access-Control-Allow-Origin"))
        {
            context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = new
        {
            type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
            title = "An error occurred while processing your request.",
            status = 500,
            detail = exception.Message,
            innerException = exception.InnerException?.Message,
            stackTrace = exception.StackTrace?.Split('\n').Take(10).ToArray(), // First 10 lines of stack trace
            traceId = context.TraceIdentifier
        };

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return context.Response.WriteAsync(jsonResponse);
    }
}





