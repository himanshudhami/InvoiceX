# Logging Configuration

This project uses Serilog for structured logging with environment-specific configurations.

## Environment Switching

The application automatically detects the environment based on the `ASPNETCORE_ENVIRONMENT` environment variable or `appsettings.{Environment}.json` files.

### Development Mode

To run in **Development** mode (detailed console logs, colored output):

```bash
# Option 1: Set environment variable
export ASPNETCORE_ENVIRONMENT=Development
dotnet run

# Option 2: Use launchSettings.json (if configured)
dotnet run --launch-profile Development

# Option 3: Use appsettings.Development.json automatically
# (if ASPNETCORE_ENVIRONMENT is not set, defaults to Production)
```

**Development mode features:**
- ✅ Colored console output with readable format
- ✅ Debug-level logging for Application and Infrastructure layers
- ✅ Full exception stack traces in console
- ✅ Detailed request/response logging
- ✅ Logs saved to `logs/app-development-{date}.log`

### Production Mode

To run in **Production** mode (structured logging):

```bash
# Option 1: Explicitly set (default if not specified)
export ASPNETCORE_ENVIRONMENT=Production
dotnet run

# Option 2: Unset the variable
unset ASPNETCORE_ENVIRONMENT
dotnet run
```

**Production mode features:**
- ✅ Readable console output (not JSON) for easier debugging
- ✅ JSON-formatted file logs for log aggregation tools
- ✅ Information-level logging (less verbose)
- ✅ Logs saved to `logs/app-{date}.log` (JSON format)
- ✅ Full exception stack traces in console

## Log Levels

### Development
- **Default**: Debug
- **Application/Infrastructure**: Debug
- **Microsoft.AspNetCore**: Information
- **Microsoft**: Information

### Production
- **Default**: Information
- **Application/Infrastructure**: Information
- **Microsoft.AspNetCore**: Warning
- **Microsoft**: Warning

## Log Output Locations

- **Console**: Always visible when running `dotnet run`
- **Development logs**: `logs/app-development-{date}.log`
- **Production logs**: `logs/app-{date}.log` (JSON format)

## Exception Logging

All exceptions are automatically logged with:
- Full stack traces
- Inner exception details
- Request context (method, path, trace ID)
- Correlation ID for request tracking

Exception details appear in:
1. Console output (immediately visible)
2. Log files (persisted)
3. API error responses (limited details for security)

## Quick Reference

```bash
# Development (detailed logs)
ASPNETCORE_ENVIRONMENT=Development dotnet run

# Production (standard logs)
ASPNETCORE_ENVIRONMENT=Production dotnet run
# or simply
dotnet run  # defaults to Production
```

## Viewing Logs

### Console
Logs appear directly in the terminal when running `dotnet run`.

### Log Files
```bash
# View latest development log
tail -f logs/app-development-*.log

# View latest production log (JSON)
tail -f logs/app-*.log | jq  # requires jq for JSON formatting
```

## Troubleshooting

If you're not seeing detailed logs:

1. **Check environment variable:**
   ```bash
   echo $ASPNETCORE_ENVIRONMENT
   ```

2. **Verify appsettings.Development.json exists** and has correct configuration

3. **Check log level overrides** in `appsettings.json` or `appsettings.Development.json`

4. **Ensure Serilog is properly configured** in `Program.cs` and `SerilogConfiguration.cs`






