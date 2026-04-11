using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Schichtplaner.Models;

namespace Schichtplaner.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Standort> Standorte => Set<Standort>();
    public DbSet<Mitarbeiter> Mitarbeiter => Set<Mitarbeiter>();
    public DbSet<Schicht> Schichten => Set<Schicht>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Schicht>()
            .Property(s => s.Datum)
            .HasColumnType("date");

        builder.Entity<Standort>()
            .HasMany(s => s.Mitarbeiter)
            .WithOne(m => m.Standort)
            .HasForeignKey(m => m.StandortId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Standort>()
            .HasMany(s => s.Schichten)
            .WithOne(s => s.Standort)
            .HasForeignKey(s => s.StandortId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Mitarbeiter>()
            .HasMany(m => m.Schichten)
            .WithOne(s => s.Mitarbeiter)
            .HasForeignKey(s => s.MitarbeiterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}