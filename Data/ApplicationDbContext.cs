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
    public DbSet<TagesSlotZeit> TagesSlotZeiten => Set<TagesSlotZeit>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Schicht>()
            .Property(s => s.Datum)
            .HasColumnType("date");

        builder.Entity<TagesSlotZeit>()
            .Property(t => t.Datum)
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

        builder.Entity<Standort>()
            .HasMany(s => s.TagesSlotZeiten)
            .WithOne(t => t.Standort)
            .HasForeignKey(t => t.StandortId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Mitarbeiter>()
            .HasMany(m => m.Schichten)
            .WithOne(s => s.Mitarbeiter)
            .HasForeignKey(s => s.MitarbeiterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<TagesSlotZeit>()
            .HasIndex(t => new { t.StandortId, t.Datum, t.Slot })
            .IsUnique();

        builder.Entity<Schicht>()
            .HasIndex(s => new { s.StandortId, s.Datum, s.Slot });

        builder.Entity<Standort>()
            .Property(s => s.FruehBeginn)
            .HasConversion(v => v, v => v);

        builder.Entity<Standort>()
            .Property(s => s.FruehEnde)
            .HasConversion(v => v, v => v);

        builder.Entity<Standort>()
            .Property(s => s.TagBeginn)
            .HasConversion(v => v, v => v);

        builder.Entity<Standort>()
            .Property(s => s.TagEnde)
            .HasConversion(v => v, v => v);

        builder.Entity<Standort>()
            .Property(s => s.SpaetBeginn)
            .HasConversion(v => v, v => v);

        builder.Entity<Standort>()
            .Property(s => s.SpaetEnde)
            .HasConversion(v => v, v => v);
    }
}