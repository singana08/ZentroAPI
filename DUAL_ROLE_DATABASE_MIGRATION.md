# Dual-Role Database Migration Guide

Complete guide for setting up the database for dual-role functionality.

---

## 📋 Migration Steps

### Step 1: Create Migration Files
```powershell
# Move to project directory
Set-Location "d:\KalyaniMatrimony\Git\HaluluAPI"

# Create migrations in order
dotnet ef migrations add AddProviderServiceTable
dotnet ef migrations add AddProviderAvailabilityTable
dotnet ef migrations add AddServiceBidTable
dotnet ef migrations add AddReviewTable
dotnet ef migrations add AddProviderFieldsToServiceRequest

# Apply all migrations
dotnet ef database update
```

---

## 🗄️ Generated Migration Code

### Migration 1: AddProviderServiceTable
**File:** `Migrations/20251101_AddProviderServiceTable.cs`

```csharp
using Microsoft.EntityFrameworkCore.Migrations;

namespace HaluluAPI.Migrations
{
    public partial class AddProviderServiceTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProviderServices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    MainCategory = table.Column<string>(type: "character varying(100)", 
                        maxLength: 100, nullable: false),
                    SubCategory = table.Column<string>(type: "character varying(100)", 
                        maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", 
                        maxLength: 1000, nullable: true),
                    PricePerHour = table.Column<decimal>(type: "numeric", nullable: false),
                    PricingType = table.Column<string>(type: "character varying(50)", 
                        maxLength: 50, nullable: true),
                    ServiceAreas = table.Column<string>(type: "character varying(500)", 
                        maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AverageRating = table.Column<double>(type: "double precision", nullable: true),
                    TotalReviews = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", 
                        nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProviderServices_Users_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderServices_ProviderId",
                table: "ProviderServices",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderServices_MainCategory",
                table: "ProviderServices",
                column: "MainCategory");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ProviderServices");
        }
    }
}
```

---

### Migration 2: AddProviderAvailabilityTable
**File:** `Migrations/20251101_AddProviderAvailabilityTable.cs`

```csharp
using Microsoft.EntityFrameworkCore.Migrations;

namespace HaluluAPI.Migrations
{
    public partial class AddProviderAvailabilityTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProviderAvailabilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", 
                        nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderAvailabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProviderAvailabilities_Users_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderAvailabilities_ProviderId",
                table: "ProviderAvailabilities",
                column: "ProviderId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ProviderAvailabilities");
        }
    }
}
```

---

### Migration 3: AddServiceBidTable
**File:** `Migrations/20251101_AddServiceBidTable.cs`

```csharp
using Microsoft.EntityFrameworkCore.Migrations;

namespace HaluluAPI.Migrations
{
    public partial class AddServiceBidTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceBids",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuoteAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    QuoteDescription = table.Column<string>(type: "character varying(1000)", 
                        maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", 
                        maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", 
                        nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceBids", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceBids_ServiceRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "ServiceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceBids_Users_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceBids_RequestId",
                table: "ServiceBids",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceBids_ProviderId",
                table: "ServiceBids",
                column: "ProviderId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ServiceBids");
        }
    }
}
```

---

### Migration 4: AddReviewTable
**File:** `Migrations/20251101_AddReviewTable.cs`

```csharp
using Microsoft.EntityFrameworkCore.Migrations;

namespace HaluluAPI.Migrations
{
    public partial class AddReviewTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(1000)", 
                        maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", 
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reviews_ServiceRequests_ServiceRequestId",
                        column: x => x.ServiceRequestId,
                        principalTable: "ServiceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reviews_Users_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reviews_Users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ServiceRequestId",
                table: "Reviews",
                column: "ServiceRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ProviderId",
                table: "Reviews",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ReviewedByUserId",
                table: "Reviews",
                column: "ReviewedByUserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Reviews");
        }
    }
}
```

---

### Migration 5: AddProviderFieldsToServiceRequest
**File:** `Migrations/20251101_AddProviderFieldsToServiceRequest.cs`

```csharp
using Microsoft.EntityFrameworkCore.Migrations;

namespace HaluluAPI.Migrations
{
    public partial class AddProviderFieldsToServiceRequest : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssignedProviderId",
                table: "ServiceRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AcceptedAt",
                table: "ServiceRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "ServiceRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "ServiceRequests",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderQuote",
                table: "ServiceRequests",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProviderRating",
                table: "ServiceRequests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderReview",
                table: "ServiceRequests",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_AssignedProviderId",
                table: "ServiceRequests",
                column: "AssignedProviderId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceRequests_Users_AssignedProviderId",
                table: "ServiceRequests",
                column: "AssignedProviderId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceRequests_Users_AssignedProviderId",
                table: "ServiceRequests");

            migrationBuilder.DropIndex(
                name: "IX_ServiceRequests_AssignedProviderId",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(name: "AssignedProviderId", table: "ServiceRequests");
            migrationBuilder.DropColumn(name: "AcceptedAt", table: "ServiceRequests");
            migrationBuilder.DropColumn(name: "CompletedAt", table: "ServiceRequests");
            migrationBuilder.DropColumn(name: "CancellationReason", table: "ServiceRequests");
            migrationBuilder.DropColumn(name: "ProviderQuote", table: "ServiceRequests");
            migrationBuilder.DropColumn(name: "ProviderRating", table: "ServiceRequests");
            migrationBuilder.DropColumn(name: "ProviderReview", table: "ServiceRequests");
        }
    }
}
```

---

## 🔄 How Migrations Work

### Step-by-Step Process

```
1. Models Updated
   ├─ ProviderService.cs created
   ├─ ProviderAvailability.cs created
   ├─ ServiceBid.cs created
   ├─ Review.cs created
   └─ ServiceRequest.cs updated

2. DbContext Updated
   ├─ Add DbSet<ProviderService>
   ├─ Add DbSet<ProviderAvailability>
   ├─ Add DbSet<ServiceBid>
   ├─ Add DbSet<Review>
   └─ Configure relationships

3. Create Migrations
   └─ dotnet ef migrations add [MigrationName]
      ├─ Generates: Migrations/[timestamp]_[Name].cs
      └─ Generates: Migrations/[timestamp]_[Name].Designer.cs

4. Review Generated Code
   └─ Check Up() method (what gets added)
   └─ Check Down() method (rollback logic)

5. Apply Migrations
   └─ dotnet ef database update
      ├─ Executes all pending migrations
      ├─ Updates database schema
      └─ Updates __EFMigrationsHistory table
```

---

## 🔙 Rollback Steps

If something goes wrong:

### Rollback Last Migration
```powershell
# Remove last migration
dotnet ef migrations remove

# Revert database to previous state
dotnet ef database update [PreviousMigrationName]
```

### Rollback All Migrations
```powershell
# Remove all migrations (careful!)
dotnet ef database update 0

# This reverts database to initial state
```

### Check Current State
```powershell
# List all migrations
dotnet ef migrations list

# List all applied migrations
dotnet ef database update --script --idempotent
```

---

## 📊 Verify Database After Migration

### Check Tables Were Created
```sql
-- Connect to your PostgreSQL database
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public';

-- Expected output:
-- ProviderServices
-- ProviderAvailabilities
-- ServiceBids
-- Reviews
-- ServiceRequests (updated)
-- Users
-- Categories
-- etc.
```

### Check Columns on ServiceRequest
```sql
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'ServiceRequests';

-- Expected new columns:
-- AssignedProviderId (uuid)
-- AcceptedAt (timestamp with time zone)
-- CompletedAt (timestamp with time zone)
-- CancellationReason (varchar)
-- ProviderQuote (varchar)
-- ProviderRating (integer)
-- ProviderReview (varchar)
```

### Check Indexes Were Created
```sql
SELECT indexname 
FROM pg_indexes 
WHERE tablename IN ('ProviderServices', 'ServiceBids', 'ServiceRequests');

-- Expected indexes:
-- IX_ProviderServices_ProviderId
-- IX_ProviderServices_MainCategory
-- IX_ServiceBids_RequestId
-- IX_ServiceBids_ProviderId
-- IX_ServiceRequests_AssignedProviderId
```

---

## 🚀 Running Migrations

### Option 1: Command Line
```powershell
Set-Location "d:\KalyaniMatrimony\Git\HaluluAPI"

# Create all migrations at once
dotnet ef migrations add AddDualRoleSupport

# Or create individually (recommended for understanding)
dotnet ef migrations add AddProviderServiceTable
dotnet ef migrations add AddProviderAvailabilityTable
dotnet ef migrations add AddServiceBidTable
dotnet ef migrations add AddReviewTable
dotnet ef migrations add AddProviderFieldsToServiceRequest

# Apply to database
dotnet ef database update
```

### Option 2: Visual Studio Package Manager Console
```powershell
# In Package Manager Console (while in HaluluAPI project)
Add-Migration AddProviderServiceTable
Update-Database
```

### Option 3: Automated Script
```powershell
# Create script file: migrate.ps1
$projectPath = "d:\KalyaniMatrimony\Git\HaluluAPI"
Set-Location $projectPath

# Run migrations
dotnet ef database update

# Verify
Write-Host "Database migration complete!"
```

---

## ⚠️ Important Notes

### Before Running Migrations

✅ **Backup your database**
```powershell
# Export current database
pg_dump -U postgres -W halulu_api > backup.sql
```

✅ **Commit your code**
```powershell
git add .
git commit -m "Add dual-role models and migrations"
```

✅ **Review migrations**
```powershell
# Check what will be applied
dotnet ef migrations script --idempotent
```

### During Migration

⚠️ **Don't interrupt** the migration process
⚠️ **Avoid** making database changes while migrating
⚠️ **Monitor** for errors

### After Migration

✅ **Verify** all tables created
✅ **Test** API with new data models
✅ **Monitor** database size/performance
✅ **Document** applied migrations

---

## 🔍 Common Migration Issues

### Issue 1: Migration Fails - "Column already exists"
**Cause**: Model and migration got out of sync
**Fix**:
```powershell
# Check current state
dotnet ef migrations list

# Remove conflicting migration
dotnet ef migrations remove

# Create new migration
dotnet ef migrations add [MigrationName]
```

### Issue 2: "No DbSet for model X"
**Cause**: Model created but not added to DbContext
**Fix**: Add to ApplicationDbContext
```csharp
public DbSet<YourModel> YourModels { get; set; }
```

### Issue 3: Foreign Key Constraint Violations
**Cause**: Migration trying to add constraint on non-existent data
**Fix**: Check data integrity or adjust OnDelete behavior

### Issue 4: Rollback Required
**Cause**: Migration had errors
**Fix**:
```powershell
dotnet ef migrations remove
dotnet ef database update
# Fix code
dotnet ef migrations add [MigrationName]
```

---

## 📝 Verification Checklist

After running migrations:

- [ ] All 4 new tables exist
- [ ] ServiceRequest has 7 new columns
- [ ] Foreign keys are properly set
- [ ] Indexes created for performance
- [ ] Migration appears in `__EFMigrationsHistory`
- [ ] No errors in migration output
- [ ] Database backup taken
- [ ] Application starts without errors
- [ ] API endpoints work with new models
- [ ] Existing data not affected

---

## 💾 Restore from Backup

If something goes wrong:

```powershell
# List backups
dir *.sql

# Restore database
psql -U postgres -d halulu_api < backup.sql

# Or in C# code - reset migrations
dotnet ef database update 0
dotnet ef database update

# Recreate from migrations
```

---

## 🎯 Next Steps

After migrations are applied:

1. ✅ Build solution
2. ✅ Run unit tests
3. ✅ Test with Postman
4. ✅ Update API documentation
5. ✅ Deploy to staging
6. ✅ Smoke tests
7. ✅ Deploy to production

---

**Migration setup complete!** Ready for implementation. 🚀