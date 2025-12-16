using DbUp;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace Migrations;

class Program
{
    static int Main(string[] args)
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Get connection string
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            // If not found in current directory, try to get from WebApi project
            var webApiPath = Path.Combine("..", "WebApi");
            if (Directory.Exists(webApiPath))
            {
                configuration = new ConfigurationBuilder()
                    .SetBasePath(webApiPath)
                    .AddJsonFile("appsettings.json", optional: false)
                    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true)
                    .AddEnvironmentVariables()
                    .Build();
                
                connectionString = configuration.GetConnectionString("DefaultConnection");
            }
        }

        if (string.IsNullOrEmpty(connectionString))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("ERROR: Could not find connection string 'DefaultConnection' in configuration.");
            Console.ResetColor();
            return -1;
        }

        Console.WriteLine("========================================");
        Console.WriteLine("InvoiceApp Database Migration Tool");
        Console.WriteLine("========================================");
        Console.WriteLine();
        Console.WriteLine($"Connection: {MaskConnectionString(connectionString)}");
        Console.WriteLine();

        // Ensure database exists
        EnsureDatabase.For.PostgresqlDatabase(connectionString);

        // Resolve scripts path (default: backend/migrations, override with MIGRATIONS_PATH)
        var migrationsPath = Environment.GetEnvironmentVariable("MIGRATIONS_PATH") 
            ?? Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "migrations"));

        if (!Directory.Exists(migrationsPath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: Migrations folder not found at {migrationsPath}");
            Console.ResetColor();
            return -1;
        }

        Console.WriteLine($"Using migrations from: {migrationsPath}");

        // Configure DbUp
        var upgrader = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsFromFileSystem(migrationsPath, script => !script.ToLowerInvariant().Contains("_down"))
            .JournalToPostgresqlTable("public", "schemaversions")
            .LogToConsole()
            .WithTransactionPerScript()
            .Build();

        // Check if there are scripts to execute
        var scriptsToExecute = upgrader.GetScriptsToExecute();
        
        if (!scriptsToExecute.Any())
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ Database is up to date. No migrations to run.");
            Console.ResetColor();
            return 0;
        }

        Console.WriteLine($"Found {scriptsToExecute.Count} migration(s) to execute:");
        foreach (var script in scriptsToExecute)
        {
            Console.WriteLine($"  - {script.Name}");
        }
        Console.WriteLine();

        // Perform upgrade
        Console.WriteLine("Applying migrations...");
        var result = upgrader.PerformUpgrade();

        if (!result.Successful)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("✗ Migration failed!");
            Console.WriteLine(result.Error);
            Console.ResetColor();
            return -1;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✓ Migrations applied successfully!");
        Console.ResetColor();
        
        // Show executed scripts
        if (result.Scripts.Any())
        {
            Console.WriteLine();
            Console.WriteLine("Executed scripts:");
            foreach (var script in result.Scripts)
            {
                Console.WriteLine($"  ✓ {script.Name}");
            }
        }

        return 0;
    }

    private static string MaskConnectionString(string connectionString)
    {
        // Mask password in connection string for display
        var parts = connectionString.Split(';');
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Trim().StartsWith("Password", StringComparison.OrdinalIgnoreCase))
            {
                var keyValue = parts[i].Split('=');
                if (keyValue.Length == 2)
                {
                    parts[i] = $"{keyValue[0]}=****";
                }
            }
        }
        return string.Join(";", parts);
    }
}