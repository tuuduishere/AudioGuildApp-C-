using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace TravelSmart.API.Models;

public partial class VinhKhanhTravelDbContext : DbContext
{
    public VinhKhanhTravelDbContext()
    {
    }

    public VinhKhanhTravelDbContext(DbContextOptions<VinhKhanhTravelDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Language> Languages { get; set; }

    public virtual DbSet<Poi> Pois { get; set; }

    public virtual DbSet<PoiTranslation> PoiTranslations { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<VisitLog> VisitLogs { get; set; }

    public virtual DbSet<MenuItem> MenuItems { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public DbSet<Tour> Tours { get; set; }

    public DbSet<TourDetail> TourDetails { get; set; }


    public virtual DbSet<Notification> Notifications { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=TUUDUISHERE;Database=VinhKhanhTravelDB;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Language>(entity =>
        {
            entity.HasKey(e => e.LanguageCode).HasName("PK__Language__8B8C8A35F1CFF2D5");

            entity.Property(e => e.LanguageCode)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LanguageName).HasMaxLength(50);
        });

        modelBuilder.Entity<Poi>(entity =>
        {
            entity.HasKey(e => e.PoiId).HasName("PK__POIs__19261529A978ED8A");

            entity.ToTable("POIs");

            entity.HasIndex(e => e.QrCodeKey, "UQ__POIs__99CA2776D215F758").IsUnique();

            entity.Property(e => e.PoiId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.QrCodeKey)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.RadiusMeter).HasDefaultValue(50);

            entity.HasOne(d => d.Owner).WithMany(p => p.Pois)
                .HasForeignKey(d => d.OwnerId)
                .HasConstraintName("FK__POIs__OwnerId__440B1D61");
        });

        modelBuilder.Entity<PoiTranslation>(entity =>
        {
            entity.HasKey(e => e.TranslationId).HasName("PK__PoiTrans__663DA04CB751165A");

            entity.HasIndex(e => new { e.PoiId, e.LanguageCode }, "UQ_Poi_Lang").IsUnique();

            entity.Property(e => e.TranslationId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.AudioUrl).IsUnicode(false);
            entity.Property(e => e.LanguageCode)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Name).HasMaxLength(255);

            entity.HasOne(d => d.LanguageCodeNavigation).WithMany(p => p.PoiTranslations)
                .HasForeignKey(d => d.LanguageCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PoiTransl__Langu__4E88ABD4");

            entity.HasOne(d => d.Poi).WithMany(p => p.PoiTranslations)
                .HasForeignKey(d => d.PoiId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PoiTransl__PoiId__4D94879B");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE1AC09A9DC5");

            entity.HasIndex(e => e.RoleName, "UQ__Roles__8A2B616085F90033").IsUnique();

            entity.Property(e => e.RoleName)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4C2D556D68");

            entity.HasIndex(e => e.Username, "UQ__Users__536C85E400479039").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Users__A9D10534100A9B29").IsUnique();

            entity.Property(e => e.UserId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Users__RoleId__3D5E1FD2");
        });

        modelBuilder.Entity<VisitLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__VisitLog__5E54864832938F2F");

            entity.Property(e => e.LogId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.VisitTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.VisitType)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Poi).WithMany(p => p.VisitLogs)
                .HasForeignKey(d => d.PoiId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__VisitLogs__PoiId__52593CB8");

            entity.HasOne(d => d.User).WithMany(p => p.VisitLogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__VisitLogs__UserI__534D60F1");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
