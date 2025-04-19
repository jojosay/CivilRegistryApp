using CivilRegistryApp.Data.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CivilRegistryApp.Data
{
    public static class DatabaseInitializer
    {
        public static async Task InitializeAsync(AppDbContext context)
        {
            try
            {
                // Check if database exists
                bool dbExists = await context.Database.CanConnectAsync();

                if (!dbExists)
                {
                    // If database doesn't exist, let EF Core create it with the current schema
                    Log.Information("Database does not exist. Creating new database with current schema.");
                    await context.Database.EnsureCreatedAsync();
                    Log.Information("Database created successfully.");
                    return;
                }

                // Database exists, check if we need to add missing columns to Users table
                Log.Information("Database exists. Checking for schema updates.");
                await AddMissingColumnsAsync(context);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while initializing the database");
                throw;
            }
        }

        private static async Task AddMissingColumnsAsync(AppDbContext context)
        {
            try
            {
                // Create a new database connection to check schema
                var connectionString = context.Database.GetConnectionString();
                using var connection = new SqliteConnection(connectionString);
                await connection.OpenAsync();

                // Check if Users table exists
                var tableCheckCommand = connection.CreateCommand();
                tableCheckCommand.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Users'";
                var tableExists = await tableCheckCommand.ExecuteScalarAsync() != null;

                if (!tableExists)
                {
                    Log.Information("Users table does not exist yet. It will be created by EF Core.");
                    return;
                }

                // Check if ScheduledReports table exists
                tableCheckCommand = connection.CreateCommand();
                tableCheckCommand.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='ScheduledReports'";
                var scheduledReportsTableExists = await tableCheckCommand.ExecuteScalarAsync() != null;

                if (!scheduledReportsTableExists)
                {
                    Log.Information("ScheduledReports table does not exist. Creating it now.");
                    await CreateScheduledReportsTableAsync(connection);
                    Log.Information("ScheduledReports table created successfully.");
                }

                // Check if FieldConfigurations table exists
                tableCheckCommand = connection.CreateCommand();
                tableCheckCommand.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='FieldConfigurations'";
                var fieldConfigurationsTableExists = await tableCheckCommand.ExecuteScalarAsync() != null;

                if (!fieldConfigurationsTableExists)
                {
                    Log.Information("FieldConfigurations table does not exist. Creating it now.");
                    await CreateFieldConfigurationsTableAsync(connection);
                    Log.Information("FieldConfigurations table created successfully.");
                }

                // Get existing columns in Users table
                var command = connection.CreateCommand();
                command.CommandText = "PRAGMA table_info(Users)";
                var reader = await command.ExecuteReaderAsync();

                var existingColumns = new List<string>();
                while (await reader.ReadAsync())
                {
                    string columnName = reader.GetString(1); // Column name is at index 1
                    existingColumns.Add(columnName);
                }

                // Close the reader
                await reader.CloseAsync();

                // Check which columns need to be added
                var columnsToAdd = new Dictionary<string, string>
                {
                    // Basic user info columns
                    { "Email", "TEXT" },
                    { "PhoneNumber", "TEXT" },
                    { "Position", "TEXT" },
                    { "Department", "TEXT" },
                    { "ProfilePicturePath", "TEXT" },

                    // Account status
                    { "IsActive", "INTEGER NOT NULL DEFAULT 1" },

                    // Permission columns
                    { "CanAddDocuments", "INTEGER NOT NULL DEFAULT 1" },
                    { "CanEditDocuments", "INTEGER NOT NULL DEFAULT 1" },
                    { "CanDeleteDocuments", "INTEGER NOT NULL DEFAULT 0" },
                    { "CanViewRequests", "INTEGER NOT NULL DEFAULT 1" },
                    { "CanProcessRequests", "INTEGER NOT NULL DEFAULT 1" },
                    { "CanManageUsers", "INTEGER NOT NULL DEFAULT 0" },

                    // Timestamp columns
                    { "LastLoginAt", "TEXT" },
                    { "LastPasswordChangeAt", "TEXT" },
                    { "LastUpdatedAt", "TEXT" },
                    { "CreatedAt", "TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP" }
                };

                // Add missing columns
                bool schemaUpdated = false;
                foreach (var column in columnsToAdd)
                {
                    if (!existingColumns.Contains(column.Key))
                    {
                        var alterCommand = connection.CreateCommand();
                        alterCommand.CommandText = $"ALTER TABLE Users ADD COLUMN {column.Key} {column.Value}";
                        await alterCommand.ExecuteNonQueryAsync();
                        Log.Information("Added missing column {Column} to Users table", column.Key);
                        schemaUpdated = true;
                    }
                }

                // If schema was updated, update permissions based on roles for existing users
                if (schemaUpdated)
                {
                    // Close the connection to allow EF Core to use it
                    await connection.CloseAsync();

                    try
                    {
                        // Use raw SQL to update permissions based on roles
                        // This avoids issues with NULL values in the database
                        await context.Database.ExecuteSqlRawAsync(@"
                            UPDATE Users SET
                                CanAddDocuments = CASE
                                    WHEN Role = 'Administrator' THEN 1
                                    WHEN Role = 'Clerk' THEN 1
                                    ELSE 0
                                END,
                                CanEditDocuments = CASE
                                    WHEN Role = 'Administrator' THEN 1
                                    WHEN Role = 'Clerk' THEN 1
                                    ELSE 0
                                END,
                                CanDeleteDocuments = CASE
                                    WHEN Role = 'Administrator' THEN 1
                                    ELSE 0
                                END,
                                CanViewRequests = CASE
                                    WHEN Role = 'Administrator' THEN 1
                                    WHEN Role = 'Clerk' THEN 1
                                    ELSE 0
                                END,
                                CanProcessRequests = CASE
                                    WHEN Role = 'Administrator' THEN 1
                                    WHEN Role = 'Clerk' THEN 1
                                    ELSE 0
                                END,
                                CanManageUsers = CASE
                                    WHEN Role = 'Administrator' THEN 1
                                    ELSE 0
                                END
                        ");

                        Log.Information("Updated permissions for all users based on their roles");
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Error updating user permissions. Will continue with application startup.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error adding missing columns to database");
                throw;
            }
        }

        private static async Task CreateScheduledReportsTableAsync(SqliteConnection connection)
        {
            try
            {
                var createTableCommand = connection.CreateCommand();
                createTableCommand.CommandText = @"
                    CREATE TABLE ScheduledReports (
                        ScheduledReportId INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        ReportType TEXT NOT NULL,
                        DocumentType TEXT,
                        RegistryOffice TEXT,
                        Province TEXT,
                        CityMunicipality TEXT,
                        Barangay TEXT,
                        DateFrom TEXT,
                        DateTo TEXT,
                        Schedule TEXT NOT NULL,
                        ExportFormat TEXT NOT NULL,
                        OutputPath TEXT,
                        CreatedBy INTEGER NOT NULL,
                        CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        LastRunAt TEXT,
                        IsActive INTEGER NOT NULL DEFAULT 1,
                        EmailRecipients TEXT,
                        FOREIGN KEY (CreatedBy) REFERENCES Users(UserId) ON DELETE RESTRICT
                    )";
                await createTableCommand.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating ScheduledReports table");
                throw;
            }
        }

        private static async Task CreateFieldConfigurationsTableAsync(SqliteConnection connection)
        {
            try
            {
                var createTableCommand = connection.CreateCommand();
                createTableCommand.CommandText = @"
                    CREATE TABLE FieldConfigurations (
                        FieldConfigurationId INTEGER PRIMARY KEY AUTOINCREMENT,
                        DocumentType TEXT NOT NULL,
                        FieldName TEXT NOT NULL,
                        IsRequired INTEGER NOT NULL DEFAULT 0,
                        DisplayName TEXT NOT NULL,
                        Description TEXT,
                        DisplayOrder INTEGER NOT NULL DEFAULT 0,
                        CreatedAt TEXT NOT NULL,
                        CreatedBy INTEGER NOT NULL,
                        UpdatedAt TEXT,
                        UpdatedBy INTEGER,
                        UNIQUE(DocumentType, FieldName),
                        FOREIGN KEY (CreatedBy) REFERENCES Users(UserId) ON DELETE RESTRICT
                    )";
                await createTableCommand.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating FieldConfigurations table");
                throw;
            }
        }
    }
}
