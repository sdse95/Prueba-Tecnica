using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Patients.Api.Features.Patients.Domain;

namespace Patients.Api.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<Patient> Patients => Set<Patient>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasKey(p => p.PatientId);

            entity.Property(p => p.DocumentType)
                .HasMaxLength(10)
                .IsRequired();

            entity.Property(p => p.DocumentNumber)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(p => p.FirstName)
                .HasMaxLength(80)
                .IsRequired();

            entity.Property(p => p.LastName)
                .HasMaxLength(80)
                .IsRequired();

            entity.Property(p => p.BirthDate)
                .HasColumnType("date")
                .IsRequired();

            entity.Property(p => p.PhoneNumber)
                .HasMaxLength(20);

            entity.Property(p => p.Email)
                .HasMaxLength(120);

            entity.Property(p => p.CreatedAt)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(p => new { p.DocumentType, p.DocumentNumber })
                .IsUnique();
        });
    }
}
