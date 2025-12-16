# Database Migrations

This project uses DbUp for managing database migrations for the InvoiceApp.

## Prerequisites

- .NET 9.0 SDK
- PostgreSQL database
- Connection string configured in appsettings.json

## Running Migrations

The DbUp runner now reads filesystem scripts from `backend/migrations` (override with `MIGRATIONS_PATH`).

### From Command Line

1. Run from the project directory:
```bash
cd backend/src/Migrations
MIGRATIONS_PATH=../migrations dotnet run
```

### From Visual Studio / VS Code

Set the environment variable if you moved the folder; otherwise the default `../migrations` is used. Then run the Migrations project.

## Adding New Migrations

1. Create a new SQL file in the `backend/migrations` folder
2. Name it with a sequential number prefix: `002_YourMigrationName.sql`
3. Create an optional rollback companion named `002_YourMigrationName_down.sql`
4. The runner will automatically pick up `*.sql` files that do **not** contain `_down` in the filename

## Migration Naming Convention

- Use format: `XXX_MigrationName.sql`
- XXX = Sequential number (001, 002, 003, etc.)
- MigrationName = CamelCase descriptive name
- Example: `001_InitSchema.sql`

## Features

- **Automatic versioning**: DbUp tracks which scripts have been executed
- **Transaction support**: Each script runs in a transaction
- **Idempotent scripts**: Use IF NOT EXISTS checks to make scripts re-runnable
- **Console logging**: Clear output of migration progress
- **Connection string**: Reads from appsettings.json or WebApi project

## Current Migrations

1. **001_AddPaymentInstructions.sql** - Adds payment_instructions columns to companies and invoices tables

## Rollback

DbUp doesn't support automatic rollbacks. If you need to rollback:
1. Create a new forward migration that reverses the changes
2. Name it appropriately (e.g., `002_RemovePaymentInstructions.sql`)
3. Run the migrations normally

## Connection String

The connection string is read from:
1. `appsettings.json` in the Migrations project
2. If not found, from `../WebApi/appsettings.json`
3. Environment variables can override the settings

## Troubleshooting

- **Connection failed**: Check your PostgreSQL server is running and connection string is correct
- **Migration failed**: Check the SQL syntax and ensure the database user has required permissions
- **Scripts not found**: Ensure SQL files are marked as Embedded Resources in the .csproj file