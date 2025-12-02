using System;
using System.Collections.Generic;
using ZentroAPI.TempModels;
using Microsoft.EntityFrameworkCore;

namespace ZentroAPI.Data;

public partial class ZentroDbContext : DbContext
{
    public ZentroDbContext()
    {
    }

    public ZentroDbContext(DbContextOptions<ZentroDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<MasterCategory> MasterCategories { get; set; }

    public virtual DbSet<MasterSubcategory> MasterSubcategories { get; set; }

    public virtual DbSet<OtpRecord> OtpRecords { get; set; }

    public virtual DbSet<Provider> Providers { get; set; }

    public virtual DbSet<Requester> Requesters { get; set; }

    public virtual DbSet<ServiceRequest> ServiceRequests { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=zentro_db;Username=postgres;Password=postgres");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MasterCategory>(entity =>
        {
            entity.ToTable("master_category", "zentro_api");

            entity.HasIndex(e => e.IsActive, "IX_master_category_IsActive");

            entity.HasIndex(e => e.Name, "IX_master_category_Name").IsUnique();

            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Icon).HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<MasterSubcategory>(entity =>
        {
            entity.ToTable("master_subcategory", "zentro_api");

            entity.HasIndex(e => new { e.CategoryId, e.Name }, "IX_master_subcategory_CategoryId_Name").IsUnique();

            entity.HasIndex(e => e.IsActive, "IX_master_subcategory_IsActive");

            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Icon).HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(100);

            entity.HasOne(d => d.Category).WithMany(p => p.MasterSubcategories).HasForeignKey(d => d.CategoryId);
        });

        modelBuilder.Entity<OtpRecord>(entity =>
        {
            entity.ToTable("OtpRecords", "zentro_api");

            entity.HasIndex(e => new { e.Email, e.CreatedAt }, "IX_OtpRecords_Email_CreatedAt");

            entity.HasIndex(e => e.UserId, "IX_OtpRecords_UserId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.OtpCode).HasMaxLength(10);

            entity.HasOne(d => d.User).WithMany(p => p.OtpRecords)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Provider>(entity =>
        {
            entity.ToTable("providers", "zentro_api");

            entity.HasIndex(e => e.IsActive, "IX_providers_IsActive");

            entity.HasIndex(e => e.Rating, "IX_providers_Rating");

            entity.HasIndex(e => e.UserId, "IX_providers_UserId").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Bio).HasMaxLength(1000);
            entity.Property(e => e.Earnings).HasPrecision(18, 2);
            entity.Property(e => e.ExperienceYears).HasDefaultValue(0);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PricingModel).HasMaxLength(100);
            entity.Property(e => e.Rating).HasPrecision(3, 2);

            entity.HasOne(d => d.User).WithOne(p => p.Provider).HasForeignKey<Provider>(d => d.UserId);
        });

        modelBuilder.Entity<Requester>(entity =>
        {
            entity.ToTable("requesters", "zentro_api");

            entity.HasIndex(e => e.IsActive, "IX_requesters_IsActive");

            entity.HasIndex(e => e.UserId, "IX_requesters_UserId").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.User).WithOne(p => p.Requester).HasForeignKey<Requester>(d => d.UserId);
        });

        modelBuilder.Entity<ServiceRequest>(entity =>
        {
            entity.ToTable("service_requests", "zentro_api");

            entity.HasIndex(e => e.BookingType, "IX_service_requests_BookingType");

            entity.HasIndex(e => e.CreatedAt, "IX_service_requests_CreatedAt");

            entity.HasIndex(e => e.Status, "IX_service_requests_Status");

            entity.HasIndex(e => e.UserId, "IX_service_requests_UserId");

            entity.HasIndex(e => new { e.UserId, e.Status }, "IX_service_requests_UserId_Status");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.AdditionalNotes).HasMaxLength(500);
            entity.Property(e => e.Location).HasMaxLength(500);
            entity.Property(e => e.MainCategory).HasMaxLength(100);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.Status).HasDefaultValueSql("'Pending'::text");
            entity.Property(e => e.SubCategory).HasMaxLength(100);
            entity.Property(e => e.Time).HasMaxLength(20);

            entity.HasOne(d => d.User).WithMany(p => p.ServiceRequests).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users", "zentro_api");

            entity.HasIndex(e => e.Email, "IX_users_Email").IsUnique();

            entity.HasIndex(e => e.PhoneNumber, "IX_users_PhoneNumber")
                .IsUnique()
                .HasFilter("((\"PhoneNumber\" IS NOT NULL) AND ((\"PhoneNumber\")::text <> ''::text))");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.DefaultRole)
                .HasMaxLength(20)
                .HasDefaultValueSql("'REQUESTER'::character varying");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.FullName).HasMaxLength(255);
            entity.Property(e => e.IsEmailVerified).HasDefaultValue(false);
            entity.Property(e => e.IsPhoneVerified).HasDefaultValue(false);
            entity.Property(e => e.IsProfileCompleted).HasDefaultValue(false);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.ProfileImage).HasMaxLength(500);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
