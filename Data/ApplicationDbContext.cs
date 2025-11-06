using HaluluAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HaluluAPI.Data;

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
    public DbSet<Address> Addresses => Set<Address>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Set default schema
        modelBuilder.HasDefaultSchema("halulu_api");

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
            entity.Property(e => e.AdditionalNotes).HasMaxLength(500);
            entity.Property(e => e.Status).IsRequired().HasConversion<string>().HasDefaultValue(ServiceRequestStatus.Pending);
            entity.Property(e => e.CreatedAt).IsRequired();
            
            // Indexes for common queries
            entity.HasIndex(e => e.RequesterId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.BookingType);
            entity.HasIndex(e => new { e.RequesterId, e.Status });
            entity.HasIndex(e => e.CreatedAt);
        });

        // ServiceRequest to Requester relationship
        modelBuilder.Entity<ServiceRequest>()
            .HasOne(sr => sr.Requester)
            .WithMany(r => r.ServiceRequests)
            .HasForeignKey(sr => sr.RequesterId)
            .OnDelete(DeleteBehavior.Cascade);

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
    }
}