using Microsoft.EntityFrameworkCore;
using HaluluAPI.Data;

namespace HaluluAPI;

public class SchemaUpdater
{
    public static async Task UpdateSchema()
    {
        var connectionString = "Host=localhost;Port=5432;Database=halulu_db;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        using var context = new ApplicationDbContext(optionsBuilder.Options);

        try
        {
            Console.WriteLine("Updating database schema...");
            
            // Execute SQL commands to update schema
            await context.Database.ExecuteSqlRawAsync(@"
                -- Drop existing foreign key constraint
                ALTER TABLE halulu_api.service_requests DROP CONSTRAINT IF EXISTS ""FK_service_requests_users_UserId"";
                
                -- Drop existing indexes
                DROP INDEX IF EXISTS halulu_api.""IX_service_requests_UserId"";
                DROP INDEX IF EXISTS halulu_api.""IX_service_requests_UserId_Status"";
                
                -- Rename column from UserId to RequesterId
                ALTER TABLE halulu_api.service_requests RENAME COLUMN ""UserId"" TO ""RequesterId"";
                
                -- Create new indexes with RequesterId
                CREATE INDEX ""IX_service_requests_RequesterId"" ON halulu_api.service_requests (""RequesterId"");
                CREATE INDEX ""IX_service_requests_RequesterId_Status"" ON halulu_api.service_requests (""RequesterId"", ""Status"");
                
                -- Add foreign key constraint to requesters table
                ALTER TABLE halulu_api.service_requests 
                ADD CONSTRAINT ""FK_service_requests_requesters_RequesterId"" 
                FOREIGN KEY (""RequesterId"") REFERENCES halulu_api.requesters (""Id"") ON DELETE CASCADE;
            ");
            
            Console.WriteLine("Schema updated successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}