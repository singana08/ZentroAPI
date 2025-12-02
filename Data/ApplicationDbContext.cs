using ZentroAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ZentroAPI.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Requester> Requesters => Set<Requester>();
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<OtpRecord> OtpRecords => Set<OtpRecord>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Subcategory> Subcategories => Set<Subcategory>();
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<HiddenRequest> HiddenRequests => Set<HiddenRequest>();
    public DbSet<ProviderRequestStatus> ProviderRequestStatuses => Set<ProviderRequestStatus>();
    public DbSet<Agreement> Agreements => Set<Agreement>();
    public DbSet<WorkflowStatus> WorkflowStatuses => Set<WorkflowStatus>();
    public DbSet<Review> Reviews => Set<Review>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Set default schema
        modelBuilder.HasDefaultSchema("zentro_api");

        // User configuration - simplified
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(20);
            entity.Property(e => e.ProfileImage).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).IsRequired();
            
            // Unique constraints
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.PhoneNumber)
                .IsUnique()
                .HasFilter("\"PhoneNumber\" IS NOT NULL AND \"PhoneNumber\" != ''");
        });

        // Requester configuration
        modelBuilder.Entity<Requester>(entity =>
        {
            entity.ToTable("requesters");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.PushToken).HasMaxLength(200);
            entity.Property(e => e.NotificationsEnabled).HasDefaultValue(true);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired();
            
            // Indexes
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasIndex(e => e.IsActive);
        });

        // Requester to User relationship
        modelBuilder.Entity<Requester>()
            .HasOne(r => r.User)
            .WithOne(u => u.RequesterProfile)
            .HasForeignKey<Requester>(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Provider configuration
        modelBuilder.Entity<Provider>(entity =>
        {
            entity.ToTable("providers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.ExperienceYears).HasDefaultValue(0);
            entity.Property(e => e.Bio).HasMaxLength(1000);
            entity.Property(e => e.PricingModel).HasMaxLength(100);
            entity.Property(e => e.Rating).HasDefaultValue(0);
            entity.Property(e => e.Earnings).HasDefaultValue(0);
            entity.Property(e => e.PushToken).HasMaxLength(200);
            entity.Property(e => e.NotificationsEnabled).HasDefaultValue(true);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired();
            
            // Indexes
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.Rating);
        });

        // Provider to User relationship
        modelBuilder.Entity<Provider>()
            .HasOne(p => p.User)
            .WithOne(u => u.ProviderProfile)
            .HasForeignKey<Provider>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // OtpRecord configuration
        modelBuilder.Entity<OtpRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.OtpCode).IsRequired().HasMaxLength(10);
            entity.HasIndex(e => new { e.Email, e.CreatedAt });
        });

        // OtpRecord to User relationship
        modelBuilder.Entity<OtpRecord>()
            .HasOne(o => o.User)
            .WithMany(u => u.OtpRecords)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Category configuration
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("master_category");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Icon).HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.IsActive);
        });

        // Subcategory configuration
        modelBuilder.Entity<Subcategory>(entity =>
        {
            entity.ToTable("master_subcategory");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Icon).HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.HasIndex(e => new { e.CategoryId, e.Name }).IsUnique();
            entity.HasIndex(e => e.IsActive);
        });

        // Category to Subcategory relationship
        modelBuilder.Entity<Subcategory>()
            .HasOne(s => s.Category)
            .WithMany(c => c.Subcategories)
            .HasForeignKey(s => s.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // ServiceRequest configuration
        modelBuilder.Entity<ServiceRequest>(entity =>
        {
            entity.ToTable("service_requests");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.RequesterId).IsRequired();
            entity.Property(e => e.BookingType).IsRequired().HasConversion<string>();
            entity.Property(e => e.MainCategory).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SubCategory).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Location).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Time).HasMaxLength(20);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.AdditionalNotes).HasMaxLength(500);
            entity.Property(e => e.Status).IsRequired().HasConversion<string>().HasDefaultValue(ServiceRequestStatus.Open);
            entity.Property(e => e.CreatedAt).IsRequired();
            
            // Indexes for common queries
            entity.HasIndex(e => e.RequesterId);
            entity.HasIndex(e => e.AssignedProviderId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.BookingType);
            entity.HasIndex(e => new { e.RequesterId, e.Status });
            entity.HasIndex(e => new { e.AssignedProviderId, e.Status });
            entity.HasIndex(e => e.CreatedAt);
        });

        // ServiceRequest relationships
        modelBuilder.Entity<ServiceRequest>()
            .HasOne(sr => sr.Requester)
            .WithMany(r => r.ServiceRequests)
            .HasForeignKey(sr => sr.RequesterId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ServiceRequest>()
            .HasOne(sr => sr.AssignedProvider)
            .WithMany()
            .HasForeignKey(sr => sr.AssignedProviderId)
            .OnDelete(DeleteBehavior.SetNull);

        // Address configuration
        modelBuilder.Entity<Address>(entity =>
        {
            entity.ToTable("addresses");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ProfileId).IsRequired();
            entity.Property(e => e.Label).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Latitude).IsRequired();
            entity.Property(e => e.Longitude).IsRequired();
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.State).HasMaxLength(100);
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            entity.Property(e => e.IsPrimary).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasIndex(e => e.ProfileId);
            entity.HasIndex(e => new { e.ProfileId, e.IsPrimary });
        });

        // Quote configuration
        modelBuilder.Entity<Quote>(entity =>
        {
            entity.ToTable("quotes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ProviderId).IsRequired();
            entity.Property(e => e.RequestId).IsRequired();
            entity.Property(e => e.Price).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.Message).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Pending");
            entity.Property(e => e.UpdatedAt).IsRequired();
            
            entity.HasIndex(e => e.RequestId);
            entity.HasIndex(e => e.ProviderId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.RequestId, e.ProviderId }).IsUnique();
        });

        // Quote relationships
        modelBuilder.Entity<Quote>()
            .HasOne(q => q.Provider)
            .WithMany()
            .HasForeignKey(q => q.ProviderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Quote>()
            .HasOne(q => q.ServiceRequest)
            .WithMany(sr => sr.Quotes)
            .HasForeignKey(q => q.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        // Message configuration
        modelBuilder.Entity<Message>(entity =>
        {
            entity.ToTable("messages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.SenderId).IsRequired();
            entity.Property(e => e.ReceiverId).IsRequired();
            entity.Property(e => e.RequestId).IsRequired();
            entity.Property(e => e.MessageText).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.IsRead).HasDefaultValue(false);
            
            entity.HasIndex(e => e.RequestId);
            entity.HasIndex(e => new { e.SenderId, e.ReceiverId });
            entity.HasIndex(e => e.Timestamp);
        });

        // Message relationships
        modelBuilder.Entity<Message>()
            .HasOne(m => m.ServiceRequest)
            .WithMany(sr => sr.Messages)
            .HasForeignKey(m => m.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        // Notification configuration
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("notifications");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProfileId).IsRequired();
            entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Body).IsRequired().HasMaxLength(500);
            entity.Property(e => e.NotificationType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasIndex(e => new { e.ProfileId, e.CreatedAt });
            entity.HasIndex(e => new { e.ProfileId, e.IsRead });
        });

        // HiddenRequest configuration
        modelBuilder.Entity<HiddenRequest>(entity =>
        {
            entity.ToTable("hidden_requests");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProviderId).IsRequired();
            entity.Property(e => e.ServiceRequestId).IsRequired();
            entity.Property(e => e.HiddenAt).IsRequired();
            
            entity.HasIndex(e => new { e.ProviderId, e.ServiceRequestId }).IsUnique();
            entity.HasIndex(e => e.ProviderId);
        });

        // HiddenRequest relationships
        modelBuilder.Entity<HiddenRequest>()
            .HasOne(hr => hr.Provider)
            .WithMany()
            .HasForeignKey(hr => hr.ProviderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<HiddenRequest>()
            .HasOne(hr => hr.ServiceRequest)
            .WithMany()
            .HasForeignKey(hr => hr.ServiceRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        // ProviderRequestStatus configuration
        modelBuilder.Entity<ProviderRequestStatus>(entity =>
        {
            entity.ToTable("provider_request_status");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RequestId).IsRequired();
            entity.Property(e => e.ProviderId).IsRequired();
            entity.Property(e => e.Status).IsRequired().HasConversion<string>();
            entity.Property(e => e.LastUpdated).IsRequired();
            
            entity.HasIndex(e => new { e.ProviderId, e.RequestId }).IsUnique();
            entity.HasIndex(e => e.ProviderId);
            entity.HasIndex(e => e.RequestId);
        });

        // ProviderRequestStatus relationships
        modelBuilder.Entity<ProviderRequestStatus>()
            .HasOne(prs => prs.Provider)
            .WithMany()
            .HasForeignKey(prs => prs.ProviderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProviderRequestStatus>()
            .HasOne(prs => prs.ServiceRequest)
            .WithMany()
            .HasForeignKey(prs => prs.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProviderRequestStatus>()
            .HasOne(prs => prs.Quote)
            .WithMany()
            .HasForeignKey(prs => prs.QuoteId)
            .OnDelete(DeleteBehavior.SetNull);

        // Agreement configuration
        modelBuilder.Entity<Agreement>(entity =>
        {
            entity.ToTable("agreements");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.QuoteId).IsRequired();
            entity.Property(e => e.RequesterId).IsRequired();
            entity.Property(e => e.ProviderId).IsRequired();
            entity.Property(e => e.RequesterAccepted).HasDefaultValue(false);
            entity.Property(e => e.ProviderAccepted).HasDefaultValue(false);
            entity.Property(e => e.Status).IsRequired().HasConversion<string>().HasDefaultValue(AgreementStatus.Pending);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            
            entity.HasIndex(e => new { e.QuoteId, e.ProviderId }).IsUnique();
            entity.HasIndex(e => e.RequesterId);
            entity.HasIndex(e => e.ProviderId);
            entity.HasIndex(e => e.Status);
        });

        // WorkflowStatus configuration
        modelBuilder.Entity<WorkflowStatus>(entity =>
        {
            entity.ToTable("workflow_statuses");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.RequestId).IsRequired();
            entity.Property(e => e.ProviderId).IsRequired();
            entity.Property(e => e.IsInProgress).HasDefaultValue(false);
            entity.Property(e => e.IsCheckedIn).HasDefaultValue(false);
            entity.Property(e => e.IsCompleted).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasIndex(e => new { e.RequestId, e.ProviderId }).IsUnique();
            entity.HasIndex(e => e.RequestId);
            entity.HasIndex(e => e.ProviderId);
        });

        // WorkflowStatus relationships
        modelBuilder.Entity<WorkflowStatus>()
            .HasOne(ws => ws.ServiceRequest)
            .WithMany()
            .HasForeignKey(ws => ws.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WorkflowStatus>()
            .HasOne(ws => ws.Provider)
            .WithMany()
            .HasForeignKey(ws => ws.ProviderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Review configuration
        modelBuilder.Entity<Review>(entity =>
        {
            entity.ToTable("reviews");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ServiceRequestId).IsRequired();
            entity.Property(e => e.ProviderId).IsRequired();
            entity.Property(e => e.CustomerId).IsRequired();
            entity.Property(e => e.Rating).IsRequired();
            entity.Property(e => e.Comment).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasIndex(e => e.ProviderId);
            entity.HasIndex(e => e.ServiceRequestId).IsUnique();
            entity.HasIndex(e => e.CreatedAt);
        });

        // Review relationships
        modelBuilder.Entity<Review>()
            .HasOne(r => r.ServiceRequest)
            .WithMany()
            .HasForeignKey(r => r.ServiceRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Review>()
            .HasOne(r => r.Provider)
            .WithMany()
            .HasForeignKey(r => r.ProviderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Review>()
            .HasOne(r => r.Customer)
            .WithMany()
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);


    }
}
