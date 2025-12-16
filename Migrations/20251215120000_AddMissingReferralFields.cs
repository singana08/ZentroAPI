using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZentroAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingReferralFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop and recreate referrals table to ensure all fields exist
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS zentro_api.referrals CASCADE;
                
                CREATE TABLE zentro_api.referrals (
                    ""Id"" uuid NOT NULL,
                    ""ReferrerId"" uuid NOT NULL,
                    ""ReferredUserId"" uuid NOT NULL,
                    ""ReferralCode"" character varying(10) NOT NULL,
                    ""Status"" text NOT NULL DEFAULT 'Pending',
                    ""BonusAmount"" numeric(18,2),
                    ""FirstBookingId"" uuid,
                    ""ReferredUserBookingsUsed"" integer NOT NULL DEFAULT 0,
                    ""CompletedAt"" timestamp with time zone,
                    ""WalletTransactionId"" uuid,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT NOW(),
                    CONSTRAINT ""PK_referrals"" PRIMARY KEY (""Id"")
                );
            ");

            // Add indexes
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_referrals_ReferrerId_ReferredUserId"" 
                ON zentro_api.referrals (""ReferrerId"", ""ReferredUserId"");
                
                CREATE INDEX IF NOT EXISTS ""IX_referrals_ReferrerId"" 
                ON zentro_api.referrals (""ReferrerId"");
                
                CREATE INDEX IF NOT EXISTS ""IX_referrals_ReferredUserId"" 
                ON zentro_api.referrals (""ReferredUserId"");
                
                CREATE INDEX IF NOT EXISTS ""IX_referrals_Status"" 
                ON zentro_api.referrals (""Status"");
            ");

            // Add foreign key constraints
            migrationBuilder.Sql(@"
                ALTER TABLE zentro_api.referrals 
                ADD CONSTRAINT IF NOT EXISTS ""FK_referrals_users_ReferrerId"" 
                FOREIGN KEY (""ReferrerId"") REFERENCES zentro_api.users(""Id"") ON DELETE CASCADE;
                
                ALTER TABLE zentro_api.referrals 
                ADD CONSTRAINT IF NOT EXISTS ""FK_referrals_users_ReferredUserId"" 
                FOREIGN KEY (""ReferredUserId"") REFERENCES zentro_api.users(""Id"") ON DELETE RESTRICT;
                
                ALTER TABLE zentro_api.referrals 
                ADD CONSTRAINT IF NOT EXISTS ""FK_referrals_wallet_transactions_WalletTransactionId"" 
                FOREIGN KEY (""WalletTransactionId"") REFERENCES zentro_api.wallet_transactions(""Id"") ON DELETE SET NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "referrals",
                schema: "zentro_api");
        }
    }
}